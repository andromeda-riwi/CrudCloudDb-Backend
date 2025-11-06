// --- Imports necesarios ---
using System.Security.Claims;
using CCD.Api.Dtos;
using CCD.Core; // Para usar la entidad DatabaseInstance
using CCD.Core.Interfaces; // Para usar IDatabaseProvisioner
using CCD.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// [Authorize] asegura que solo los usuarios con un token JWT válido pueden acceder a estos endpoints.
[Authorize]
[ApiController]
[Route("api/[controller]")] // Ruta base: /api/databases
public class DatabasesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    // Servicio que crea bases de datos reales en PostgreSQL
    private readonly IDatabaseProvisioner _provisioner;

    // Inyectamos el DbContext para interactuar con nuestra base de datos de gestión.
    public DatabasesController(ApplicationDbContext context, IDatabaseProvisioner provisioner)
    {
        _context = context;
        _provisioner = provisioner;
    }

    // --- ENDPOINT PARA LISTAR BASES DE DATOS ---
    // Responde a peticiones GET en /api/databases
    [HttpGet]
    public async Task<IActionResult> GetDatabasesForUser()
    {
        // 1. Obtenemos el ID del usuario directamente desde los claims del token JWT.
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdString == null)
        {
            return Unauthorized(); // Seguridad extra, aunque [Authorize] ya lo previene.
        }
        var userId = Guid.Parse(userIdString);

        // 2. Buscamos en la tabla DatabaseInstances todas las entradas que pertenezcan a este usuario.
        var databases = await _context.DatabaseInstances
            .Where(db => db.UserId == userId)
            // 3. Convertimos cada resultado a un DTO para enviar solo la información necesaria al frontend.
            .Select(db => new DatabaseResponseDto
            {
                Id = db.Id,
                Name = db.Name,
                Engine = db.Engine,
                Status = db.Status,
                CreatedAt = db.CreatedAt
            })
            .ToListAsync();

        // 4. Devolvemos la lista de bases de datos.
        return Ok(databases);
    }

    // --- ENDPOINT PARA OBTENER ESTADÍSTICAS DEL DASHBOARD ---
    // Responde a peticiones GET en /api/databases/stats
    [HttpGet("stats")]
    public async Task<IActionResult> GetDashboardStats()
    {
        // 1. Obtenemos el ID del usuario del token.
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdString == null)
        {
            return Unauthorized();
        }
        var userId = Guid.Parse(userIdString);

        // 2. Obtener todas las bases de datos del usuario
        var databases = await _context.DatabaseInstances
            .Where(db => db.UserId == userId)
            .ToListAsync();

        // 3. Calcular estadísticas por motor
        var databasesByEngine = databases
            .GroupBy(db => db.Engine)
            .ToDictionary(g => g.Key, g => g.Count());

        // 4. Obtener plan del usuario (por ahora hardcoded a "Básico")
        var currentPlan = "Básico";
        var maxDatabases = 10; // Límite del plan básico
        var monthlyPrice = 0; // Plan gratuito

        // 5. Retornar estadísticas
        return Ok(new
        {
            totalDatabases = databases.Count,
            databasesByEngine = databasesByEngine,
            currentPlan = currentPlan,
            maxDatabases = maxDatabases,
            monthlyPrice = monthlyPrice,
            nextBillingDate = (string?)null
        });
    }

    // --- ENDPOINT PARA CREAR UNA NUEVA BASE DE DATOS ---
    // Responde a peticiones POST en /api/databases
    [HttpPost]
    public async Task<IActionResult> CreateDatabase(DatabaseCreateDto createDto)
    {
        // 1. Obtenemos el ID del usuario del token.
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdString == null)
        {
            return Unauthorized();
        }
        var userId = Guid.Parse(userIdString);

        // --- LÓGICA DE VALIDACIÓN DE CUOTAS (Tu Responsabilidad) ---
        // Asumimos un plan gratuito por ahora.
        const int freePlanLimit = 2;

        // Contamos cuántas bases de datos del motor solicitado ya tiene el usuario.
        var currentDbCount = await _context.DatabaseInstances
            .CountAsync(db => db.UserId == userId && db.Engine == createDto.Engine);

        // Si el conteo es igual o mayor al límite, rechazamos la petición.
        if (currentDbCount >= freePlanLimit)
        {
            return BadRequest(new { message = $"Has alcanzado el límite de {freePlanLimit} bases de datos para el motor {createDto.Engine} en el plan gratuito." });
        }
        // --- FIN DE LA LÓGICA DE CUOTAS ---


        // --- LÓGICA DE APROVISIONAMIENTO ---
        // Aquí es donde realmente creamos la base de datos en PostgreSQL
        try
        {
            // PASO 1: Llamar al servicio que crea la base de datos real
            // Este servicio se conecta a PostgreSQL como superusuario y ejecuta:
            // - CREATE USER con una contraseña segura generada automáticamente
            // - CREATE DATABASE asignando el usuario como dueño
            var connectionDetails = await _provisioner.CreateDatabaseAsync(createDto.Engine, userId);

            if (connectionDetails == null)
            {
                return StatusCode(500, new { message = "Error inesperado al crear la base de datos." });
            }

            // PASO 2: Guardar el registro en nuestra base de datos de gestión
            // Esto es para que el usuario pueda ver sus bases de datos en el dashboard
            var newDbInstance = new DatabaseInstance
            {
                Name = connectionDetails.DatabaseName,  // Nombre generado automáticamente
                Engine = createDto.Engine,              // Motor solicitado (PostgreSQL, MySQL, etc.)
                Status = "Active",                      // Estado inicial: activa
                DbUsername = connectionDetails.Username, // Usuario de la base de datos
                UserId = userId,                         // Asociar al usuario actual
                CreatedAt = DateTime.UtcNow
            };

            // Agregamos la nueva instancia a la base de datos y guardamos los cambios
            await _context.DatabaseInstances.AddAsync(newDbInstance);
            await _context.SaveChangesAsync();

            // PASO 3: TODO - Enviar correo electrónico con las credenciales
            // connectionDetails contiene: Host, Port, DatabaseName, Username, Password
            // Aquí deberías integrar un servicio de email (SendGrid, SMTP, etc.)

            // PASO 4: Devolver la respuesta exitosa con código 201 Created
            var responseDto = new DatabaseResponseDto
            {
                Id = newDbInstance.Id,
                Name = newDbInstance.Name,
                Engine = newDbInstance.Engine,
                Status = newDbInstance.Status
            };

            // CreatedAtAction devuelve 201 y la URL donde se puede consultar el recurso creado
            return CreatedAtAction(nameof(GetDatabasesForUser), new { id = responseDto.Id }, responseDto);
        }
        catch (Exception ex)
        {
            // Si ocurre cualquier error (conexión, permisos, etc.), lo capturamos aquí
            // TODO: En producción, enviar este error a un sistema de logging o webhook
            return StatusCode(500, new { 
                message = "No se pudo crear la base de datos.", 
                detail = ex.Message 
            });
        }
    }

    // --- ENDPOINT PARA ELIMINAR UNA BASE DE DATOS ---
    // Responde a peticiones DELETE en /api/databases/some-guid-id
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDatabase(Guid id)
    {
        // 1. Obtener el ID del usuario del token para seguridad.
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdString == null) return Unauthorized();
        var userId = Guid.Parse(userIdString);

        // 2. Buscar la instancia de la base de datos en nuestra DB de gestión.
        var dbInstance = await _context.DatabaseInstances
            .FirstOrDefaultAsync(db => db.Id == id);

        // 3. Validaciones de seguridad
        if (dbInstance == null)
        {
            return NotFound(); // La base de datos no existe
        }
        if (dbInstance.UserId != userId)
        {
            return Forbid(); // La base de datos no pertenece a este usuario
        }

        // Llamar al servicio para eliminar la base de datos y el usuario del servidor real
        try
        {
            var deleted = await _provisioner.DeleteDatabaseAsync(
                dbInstance.Engine, 
                dbInstance.Name, 
                dbInstance.DbUsername
            );

            if (!deleted)
            {
                return StatusCode(500, new { message = "No se pudo eliminar la base de datos del servidor." });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                message = "Error al eliminar la base de datos del servidor.", 
                detail = ex.Message 
            });
        }

        // Eliminar el registro de nuestra base de datos de gestión
        _context.DatabaseInstances.Remove(dbInstance);
        await _context.SaveChangesAsync();

        return NoContent(); // Respuesta estándar para un borrado exitoso.
    }
}
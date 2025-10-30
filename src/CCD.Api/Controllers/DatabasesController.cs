// --- Imports necesarios ---
using System.Security.Claims;
using CCD.Api.Dtos;
using CCD.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CCD.Api.Controllers;

// [Authorize] asegura que solo los usuarios con un token JWT válido pueden acceder a estos endpoints.
[Authorize]
[ApiController]
[Route("api/[controller]")] // Ruta base: /api/databases
public class DatabasesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    // Más adelante, aquí inyectarás el servicio de Richi:
    // private readonly IDatabaseProvisioner _provisioner;

    // Inyectamos el DbContext para interactuar con nuestra base de datos de gestión.
    public DatabasesController(ApplicationDbContext context /* , IDatabaseProvisioner provisioner */)
    {
        _context = context;
        // _provisioner = provisioner;
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
                Status = db.Status
            })
            .ToListAsync();

        // 4. Devolvemos la lista de bases de datos.
        return Ok(databases);
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


        // --- LÓGICA DE APROVISIONAMIENTO (Integración con el trabajo de Richi) ---
        // TODO: Este bloque se activará cuando integres el servicio de Richi.
        /*
            // 1. Llamar al servicio de aprovisionamiento
            var connectionDetails = await _provisioner.CreateDatabaseAsync(createDto.Engine, userId);

            if (connectionDetails == null)
            {
                return StatusCode(500, "Hubo un error al crear la base de datos.");
            }

            // 2. Crear la nueva entidad para guardarla en nuestra DB de gestión
            var newDbInstance = new Core.DatabaseInstance
            {
                Name = connectionDetails.DatabaseName,
                Engine = createDto.Engine,
                Status = "Active",
                UserId = userId
            };

            // 3. Guardar el registro en nuestra base de datos
            await _context.DatabaseInstances.AddAsync(newDbInstance);
            await _context.SaveChangesAsync();

            // 4. TODO: Enviar correo con las credenciales (connectionDetails)

            // 5. Devolver la información de la nueva base de datos creada
            var responseDto = new DatabaseResponseDto
            {
                Id = newDbInstance.Id,
                Name = newDbInstance.Name,
                Engine = newDbInstance.Engine,
                Status = newDbInstance.Status
            };

            return CreatedAtAction(nameof(GetDatabasesForUser), new { id = responseDto.Id }, responseDto);
        */
        
        // Respuesta temporal mientras el servicio de Richi no está integrado
        return Ok(new { message = $"Validación de cuota exitosa. La creación de la base de datos {createDto.Engine} está en proceso..." });
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

        // TODO: Llamar a un servicio para eliminar la base de datos y el usuario del servidor real.

        _context.DatabaseInstances.Remove(dbInstance);
        await _context.SaveChangesAsync();

        return NoContent(); // Respuesta estándar para un borrado exitoso.
    }
}
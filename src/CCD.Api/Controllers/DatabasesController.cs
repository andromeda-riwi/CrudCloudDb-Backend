// --- Imports necesarios ---
using System.Security.Claims;
using CCD.Api.Dtos;
using CCD.Core; // Para usar la entidad DatabaseInstance
using CCD.Core.Interfaces; // Para usar IDatabaseProvisioner e IEmailService
using CCD.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeZoneConverter;

[Authorize]
[ApiController]
[Route("api/[controller]")] // Ruta base: /api/databases
public class DatabasesController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly ApplicationDbContext _context;
    // Servicio que crea bases de datos reales en múltiples motores (PostgreSQL, MongoDB, MySQL, SQL Server, etc.)
    private readonly IDatabaseProvisioner _databaseProvisioner;
    private readonly IEmailService _emailService;
    private readonly ILogger<DatabasesController> _logger;

    // Inyectamos el DbContext y los servicios de aprovisionamiento y email
    public DatabasesController(
        IConfiguration config,
        ApplicationDbContext context,
        IDatabaseProvisioner databaseProvisioner,
        IEmailService emailService,
        ILogger<DatabasesController> logger)
    {
        _config = config;
        _context = context;
        _databaseProvisioner = databaseProvisioner;
        _emailService = emailService;
        _logger = logger;
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

        // 2. Buscamos las bases de datos del usuario.
        var databases = await _context.DatabaseInstances
            .Where(db => db.UserId == userId)
            .AsNoTracking()
            .ToListAsync();

        // 3. Convertimos cada resultado a un DTO aplicando la zona horaria almacenada.
        var response = databases.Select(db =>
        {
            TimeZoneInfo timeZone;
            try
            {
                timeZone = TZConvert.GetTimeZoneInfo(db.TimeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
                timeZone = TimeZoneInfo.Utc;
            }
            catch (InvalidTimeZoneException)
            {
                timeZone = TimeZoneInfo.Utc;
            }

            var createdAtLocal = TimeZoneInfo.ConvertTimeFromUtc(db.CreatedAt, timeZone);

            return new DatabaseResponseDto
            {
                Id = db.Id,
                Name = db.Name,
                Engine = db.Engine,
                Status = db.Status,
                CreatedAt = createdAtLocal,
                CreatedAtUtc = db.CreatedAt,
                TimeZoneId = db.TimeZoneId
            };
        }).ToList();

        // 4. Devolvemos la lista de bases de datos.
        return Ok(response);
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
            .AsNoTracking()
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

        TimeZoneInfo userTimeZoneInfo;
        string normalizedTimeZoneId;
        try
        {
            userTimeZoneInfo = TZConvert.GetTimeZoneInfo(createDto.TimeZoneId);
            normalizedTimeZoneId = TZConvert.TryWindowsToIana(userTimeZoneInfo.Id, out var ianaId)
                ? ianaId
                : userTimeZoneInfo.Id;
        }
        catch (TimeZoneNotFoundException)
        {
            return BadRequest(new { message = $"Zona horaria '{createDto.TimeZoneId}' no es válida." });
        }
        catch (InvalidTimeZoneException)
        {
            return BadRequest(new { message = $"Zona horaria '{createDto.TimeZoneId}' no es válida." });
        }

        // --- LÓGICA DE VALIDACIÓN DE CUOTAS (Tu Responsabilidad) ---
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
        // Aquí es donde realmente creamos la base de datos en el motor solicitado (PostgreSQL, MongoDB, MySQL, SQL Server, etc.)
        try
        {
            // PASO 1: Llamar al servicio que crea la base de datos real
            // Este servicio se conecta al motor de base de datos como administrador y:
            // - Para PostgreSQL: Crea usuario y base de datos con permisos aislados
            // - Para MongoDB: Crea base de datos, usuario con roles readWrite y dbAdmin
            // - Para MySQL: Crea base de datos y usuario con permisos específicos
            // - Para SQL Server: Crea base de datos, login y usuario con roles específicos
            // - Genera credenciales seguras (usuario, contraseña) automáticamente
            var connectionDetails = await _databaseProvisioner.CreateDatabaseAsync(createDto.Engine, userId);

            // Si algo salió mal y no obtuvimos las credenciales, devolvemos error 500
            if (connectionDetails == null)
            {
                return StatusCode(500, new { message = "Error inesperado al crear la base de datos." });
            }

            // Validar que los campos obligatorios no sean nulos
            if (string.IsNullOrEmpty(connectionDetails.DatabaseName) ||
                string.IsNullOrEmpty(createDto.Engine) ||
                string.IsNullOrEmpty(connectionDetails.Username))
            {
                return StatusCode(500, new { message = "Error: No se pudieron generar todas las credenciales necesarias." });
            }

            // PASO 2: Guardar el registro en nuestra base de datos de gestión
            var newDbInstance = new DatabaseInstance
            {
                Name = connectionDetails.DatabaseName,  // Nombre generado automáticamente
                Engine = createDto.Engine,              // Motor solicitado (PostgreSQL, MySQL, etc.)
                Status = "Active",                      // Estado inicial: activa
                DbUsername = connectionDetails.Username, // Usuario de la base de datos
                UserId = userId,                        // Asociar al usuario actual
                CreatedAt = DateTime.UtcNow,
                TimeZoneId = normalizedTimeZoneId
            };

            await _context.DatabaseInstances.AddAsync(newDbInstance);
            await _context.SaveChangesAsync();

            // PASO 3: Enviar correo electrónico con las credenciales
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                if (!string.IsNullOrEmpty(userEmail))
                {
                    await _emailService.SendDatabaseCredentialsAsync(
                        userEmail,
                        User.Identity?.Name ?? "Usuario",
                        connectionDetails);

                    _logger.LogInformation($"Correo de credenciales enviado a {userEmail}");
                }
                else
                {
                    _logger.LogWarning("No se pudo obtener el correo del usuario para enviar las credenciales");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar correo con credenciales");
            }

            // Validar que las credenciales no sean nulas
            if (connectionDetails.Host == null || connectionDetails.Username == null || connectionDetails.Password == null)
            {
                _logger.LogError("No se pudieron obtener todas las credenciales de conexión");
                return StatusCode(500, new { message = "Error al generar las credenciales de conexión." });
            }

            var createdAtLocal = TimeZoneInfo.ConvertTimeFromUtc(newDbInstance.CreatedAt, userTimeZoneInfo);

            var responseDto = new DatabaseResponseDto
            {
                Id = newDbInstance.Id,
                Name = newDbInstance.Name ?? string.Empty,
                Engine = newDbInstance.Engine ?? string.Empty,
                Status = newDbInstance.Status ?? "Active",
                CreatedAt = createdAtLocal,
                CreatedAtUtc = newDbInstance.CreatedAt,
                TimeZoneId = newDbInstance.TimeZoneId,
                // Incluir credenciales solo para la respuesta de creación
                Host = connectionDetails.Host,
                Port = connectionDetails.Port,
                Username = connectionDetails.Username,
                Password = connectionDetails.Password
            };

            return CreatedAtAction(nameof(GetDatabasesForUser), new { id = responseDto.Id }, responseDto);
        }
        catch (NotImplementedException)
        {
            return BadRequest(new { message = $"El motor de base de datos '{createDto.Engine}' no es soportado actualmente." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
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
            var deleted = await _databaseProvisioner.DeleteDatabaseAsync(
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
            return StatusCode(500, new
            {
                message = "Error al eliminar la base de datos del servidor.",
                detail = ex.Message
            });
        }

        _context.DatabaseInstances.Remove(dbInstance);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
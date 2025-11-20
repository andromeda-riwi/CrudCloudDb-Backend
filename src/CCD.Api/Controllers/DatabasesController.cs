// --- Imports necesarios ---
using System.Security.Claims;
using CCD.Api.Dtos;
using CCD.Core; 
using CCD.Core.Interfaces; 
using CCD.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeZoneConverter;

[Authorize]
[ApiController]
[Route("api/[controller]")] 
public class DatabasesController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly ApplicationDbContext _context;
    private readonly IDatabaseProvisioner _databaseProvisioner;
    private readonly IEmailService _emailService;
    private readonly ILogger<DatabasesController> _logger;
    private readonly IAuditService _auditService;
    
    public DatabasesController(
        IConfiguration config,
        ApplicationDbContext context,
        IDatabaseProvisioner databaseProvisioner,
        IEmailService emailService,
        ILogger<DatabasesController> logger,
        IAuditService auditService)
    {
        _config = config;
        _context = context;
        _databaseProvisioner = databaseProvisioner;
        _emailService = emailService;
        _logger = logger;
        _auditService = auditService;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetDatabasesForUser()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdString == null)
        {
            return Unauthorized(); 
        }
        var userId = Guid.Parse(userIdString);
        
        var databases = await _context.DatabaseInstances
            .Where(db => db.UserId == userId)
            .AsNoTracking()
            .ToListAsync();
        
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

       
        return Ok(response);
    }

    /// <summary>
    /// Obtener detalles de una base de datos específica
    /// GET /api/databases/{id}
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetDatabase(Guid id)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        var database = await _context.DatabaseInstances
            .FirstOrDefaultAsync(db => db.Id == id && db.UserId == userId);

        if (database == null)
        {
            return NotFound(new { message = "Base de datos no encontrada." });
        }

        TimeZoneInfo timeZone;
        try
        {
            timeZone = TZConvert.GetTimeZoneInfo(database.TimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            timeZone = TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            timeZone = TimeZoneInfo.Utc;
        }

        var createdAtLocal = TimeZoneInfo.ConvertTimeFromUtc(database.CreatedAt, timeZone);

        var responseDto = new DatabaseResponseDto
        {
            Id = database.Id,
            Name = database.Name,
            Engine = database.Engine,
            Status = database.Status,
            CreatedAt = createdAtLocal,
            CreatedAtUtc = database.CreatedAt,
            TimeZoneId = database.TimeZoneId
        };

        return Ok(responseDto);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetDashboardStats()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdString == null)
        {
            return Unauthorized();
        }
        var userId = Guid.Parse(userIdString);

        // 2. Obtener el usuario con su plan
        var user = await _context.Users
            .Include(u => u.Plan)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return NotFound(new { message = "Usuario no encontrado." });
        }

        // 3. Obtener todas las bases de datos del usuario
        var databases = await _context.DatabaseInstances
            .Where(db => db.UserId == userId)
            .AsNoTracking()
            .ToListAsync();

        // 4. Calcular estadísticas por motor
        var databasesByEngine = databases
            .GroupBy(db => db.Engine)
            .ToDictionary(g => g.Key, g => g.Count());

        // 5. Obtener información del plan del usuario
        var currentPlan = user.Plan?.Name ?? "Básico";
        var maxDatabasesPerEngine = user.Plan?.DatabaseLimitPerEngine ?? 2;
        var monthlyPrice = user.Plan?.Price ?? 0;

        // Calcular el número de motores disponibles (PostgreSQL, MySQL, MongoDB, etc.)
        // Por ahora, asumimos 4 motores principales
        const int availableEngines = 4; // PostgreSQL, MySQL, MongoDB, SQLServer
        var maxTotalDatabases = maxDatabasesPerEngine * availableEngines;

        // 6. Retornar estadísticas
        return Ok(new
        {
            totalDatabases = databases.Count,
            databasesByEngine = databasesByEngine,
            currentPlan = currentPlan,
            maxDatabasesPerEngine = maxDatabasesPerEngine,
            maxTotalDatabases = maxTotalDatabases,
            monthlyPrice = monthlyPrice,
            nextBillingDate = (string?)null
        });
    }


    [HttpPost]
    public async Task<IActionResult> CreateDatabase(DatabaseCreateDto createDto)
    {

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

        // --- LÓGICA DE VALIDACIÓN DE CUOTAS ---
        // Obtener el usuario con su plan para validar límites
        var user = await _context.Users
            .Include(u => u.Plan)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return NotFound(new { message = "Usuario no encontrado." });
        }

        // Obtener el límite de bases de datos por motor según el plan del usuario
        var databaseLimitPerEngine = user.Plan?.DatabaseLimitPerEngine ?? 2;
        var planName = user.Plan?.Name ?? "Gratuito";

        var currentDbCount = await _context.DatabaseInstances
            .CountAsync(db => db.UserId == userId && db.Engine == createDto.Engine);

        // Si el conteo es igual o mayor al límite, rechazamos la petición.
        if (currentDbCount >= databaseLimitPerEngine)
        {
            return BadRequest(new { message = $"Has alcanzado el límite de {databaseLimitPerEngine} bases de datos para el motor {createDto.Engine} en tu plan {planName}. Mejora tu plan para crear más bases de datos." });
        }

        try
        {

            var connectionDetails = await _databaseProvisioner.CreateDatabaseAsync(createDto.Engine, userId);

            if (connectionDetails == null)
            {
                return StatusCode(500, new { message = "Error inesperado al crear la base de datos." });
            }


            if (string.IsNullOrEmpty(connectionDetails.DatabaseName) ||
                string.IsNullOrEmpty(createDto.Engine) ||
                string.IsNullOrEmpty(connectionDetails.Username))
            {
                return StatusCode(500, new { message = "Error: No se pudieron generar todas las credenciales necesarias." });
            }


            var newDbInstance = new DatabaseInstance
            {
                Name = connectionDetails.DatabaseName,
                Engine = createDto.Engine,
                Status = "Active",
                DbUsername = connectionDetails.Username,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                TimeZoneId = normalizedTimeZoneId
            };

            await _context.DatabaseInstances.AddAsync(newDbInstance);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                "database.created",
                nameof(DatabaseInstance),
                newDbInstance.Id.ToString(),
                userId,
                $"Base de datos creada: {newDbInstance.Name} ({newDbInstance.Engine})",
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
            );

#pragma warning disable CS8601
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
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
#pragma warning restore CS8601
                _logger.LogError(ex, "Error al enviar correo con credenciales");
            }


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
                Host = connectionDetails.Host,
                Port = connectionDetails.Port,
                Username = connectionDetails.Username,
                Password = connectionDetails.Password
            };

            return CreatedAtAction(nameof(GetDatabasesForUser), new { id = responseDto.Id }, responseDto);
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

    /// <summary>
    /// Obtener credenciales de una base de datos (solo la primera vez)
    /// GET /api/databases/{id}/credentials
    /// </summary>
    [HttpGet("{id}/credentials")]
    public async Task<IActionResult> GetDatabaseCredentials(Guid id)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdString == null) return Unauthorized();
        var userId = Guid.Parse(userIdString);

        var dbInstance = await _context.DatabaseInstances
            .FirstOrDefaultAsync(db => db.Id == id);

        if (dbInstance == null)
        {
            return NotFound(new { message = "Base de datos no encontrada." });
        }

        if (dbInstance.UserId != userId)
        {
            return Forbid();
        }

        // Validación: Si ya fueron vistas, retornar error
        if (dbInstance.CredentialsViewed)
        {
            return BadRequest(new {
                message = "Las credenciales ya fueron visualizadas anteriormente. Por razones de seguridad, solo se muestran una vez. Si necesitas acceder nuevamente, usa la rotación de credenciales.",
                credentialsViewed = true
            });
        }

        try
        {
            var credentials = await _databaseProvisioner.GetDatabaseCredentialsAsync(
                dbInstance.Engine,
                dbInstance.Name,
                dbInstance.DbUsername
            );

            if (credentials == null)
            {
                return StatusCode(500, new { message = "No se pudieron obtener las credenciales." });
            }

            // Marcar credenciales como vistas
            dbInstance.CredentialsViewed = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Credenciales visualizadas para BD {DatabaseId} por usuario {UserId}", id, userId);

            return Ok(new
            {
                host = credentials.Host,
                port = credentials.Port,
                databaseName = credentials.DatabaseName,
                username = credentials.Username,
                password = credentials.Password,
                message = "⚠️ Estas son tus únicas credenciales. Guárdalas en un lugar seguro. No podrás verlas de nuevo."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener credenciales para la base de datos {DatabaseId}", id);
            return StatusCode(500, new
            {
                message = "Error al obtener las credenciales.",
                detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Rotar credenciales de una base de datos
    /// POST /api/databases/{id}/rotate-credentials
    /// </summary>
    [HttpPost("{id}/rotate-credentials")]
    public async Task<IActionResult> RotateDatabaseCredentials(Guid id)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdString == null) return Unauthorized();
        var userId = Guid.Parse(userIdString);

        var dbInstance = await _context.DatabaseInstances
            .FirstOrDefaultAsync(db => db.Id == id && db.UserId == userId);

        if (dbInstance == null)
        {
            return NotFound(new { message = "Base de datos no encontrada." });
        }

        try
        {
            var newCredentials = await _databaseProvisioner.RotateDatabaseCredentialsAsync(
                dbInstance.Engine,
                dbInstance.Name,
                dbInstance.DbUsername
            );

            if (newCredentials == null)
            {
                return StatusCode(500, new { message = "No se pudieron generar nuevas credenciales." });
            }

            dbInstance.CredentialsViewed = false;
            dbInstance.DbUsername = newCredentials.Username;
            await _context.SaveChangesAsync();

#pragma warning disable CS8601
            try
            {
                var emailClaim = User.FindFirst(ClaimTypes.Email);
                var userEmail = emailClaim?.Value ?? string.Empty;
                if (!string.IsNullOrEmpty(userEmail))
                {
                    await _emailService.SendDatabaseCredentialsAsync(
                        userEmail,
                        User.Identity?.Name ?? "Usuario",
                        newCredentials
                    );
                    _logger.LogInformation("Credenciales rotadas para BD {DatabaseId}", id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar email de credenciales");
            }
#pragma warning restore CS8601

            return Ok(new
            {
                message = "Credenciales rotadas. Email enviado.",
                host = newCredentials.Host,
                port = newCredentials.Port,
                databaseName = newCredentials.DatabaseName,
                username = newCredentials.Username,
                password = newCredentials.Password
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al rotar credenciales");
            return StatusCode(500, new { message = "Error al rotar credenciales." });
        }
    }

    // --- ENDPOINT PARA ELIMINAR UNA BASE DE DATOS ---
    // Responde a peticiones DELETE en /api/databases/some-guid-id
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDatabase(Guid id)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdString == null) return Unauthorized();
        var userId = Guid.Parse(userIdString);

        var dbInstance = await _context.DatabaseInstances
            .FirstOrDefaultAsync(db => db.Id == id);

        if (dbInstance == null)
        {
            return NotFound();
        }
        if (dbInstance.UserId != userId)
        {
            return Forbid();
        }

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

        // Enviar correo de notificación de eliminación
        try
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Usuario";

            if (!string.IsNullOrEmpty(userEmail))
            {
                await _emailService.SendDatabaseDeletionEmailAsync(
                    userEmail,
                    userName,
                    dbInstance.Name,
                    dbInstance.Engine);

                _logger.LogInformation($"Correo de eliminación enviado a {userEmail}");
            }
            else
            {
                _logger.LogWarning("No se pudo obtener el correo del usuario para enviar notificación de eliminación");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar correo de notificación de eliminación");
            // No fallar la eliminación si el correo no se puede enviar
        }

        return NoContent();
    }
}
<<<<<<< HEAD
=======
﻿// --- Imports necesarios ---
>>>>>>> a9e68670a5dc89e2128cddadaa203fed88386d58
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


    [HttpGet("stats")]
    public async Task<IActionResult> GetDashboardStats()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdString == null)
        {
            return Unauthorized();
        }
        var userId = Guid.Parse(userIdString);
<<<<<<< HEAD
        
=======

        // 2. Obtener el usuario con su plan
        var user = await _context.Users
            .Include(u => u.Plan)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return NotFound(new { message = "Usuario no encontrado." });
        }

        // 3. Obtener todas las bases de datos del usuario
>>>>>>> a9e68670a5dc89e2128cddadaa203fed88386d58
        var databases = await _context.DatabaseInstances
            .Where(db => db.UserId == userId)
            .AsNoTracking()
            .ToListAsync();
<<<<<<< HEAD
        
        var databasesByEngine = databases
            .GroupBy(db => db.Engine)
            .ToDictionary(g => g.Key, g => g.Count());
        
        var currentPlan = "Básico";
        var maxDatabases = 10; 
        var monthlyPrice = 0; 

        
=======

        // 4. Calcular estadísticas por motor
        var databasesByEngine = databases
            .GroupBy(db => db.Engine)
            .ToDictionary(g => g.Key, g => g.Count());

        // 5. Obtener información del plan del usuario
        var currentPlan = user.Plan?.Name ?? "Básico";
        var maxDatabasesPerEngine = user.Plan?.DatabaseLimitPerEngine ?? 2;
        var monthlyPrice = user.Plan?.Price ?? 0;

        // Calcular el número de motores disponibles (PostgreSQL, MySQL, MongoDB, etc.)
        // Por ahora, asumimos 3 motores principales
        const int availableEngines = 6; // PostgreSQL, MySQL, MongoDB, MariaDB, Redis, SQLite
        var maxTotalDatabases = maxDatabasesPerEngine * availableEngines;

        // 6. Retornar estadísticas
>>>>>>> a9e68670a5dc89e2128cddadaa203fed88386d58
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

<<<<<<< HEAD
       
        var user = await _context.Users
            .Include(u => u.Plan) 
            .FirstOrDefaultAsync(u => u.Id == userId);
=======
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
>>>>>>> a9e68670a5dc89e2128cddadaa203fed88386d58

        if (user == null)
        {
            return Unauthorized(new { message = "Usuario no encontrado." });
        }
        
        var planLimit = user.Plan.DatabaseLimitPerEngine;
        var planName = user.Plan.Name;

        
        var currentDbCount = await _context.DatabaseInstances
            .CountAsync(db => db.UserId == userId && db.Engine == createDto.Engine);
<<<<<<< HEAD
        
        if (currentDbCount >= planLimit)
        {
            return BadRequest(new { message = $"Has alcanzado el límite de {planLimit} bases de datos para el motor {createDto.Engine} en tu plan '{planName}'." });
=======

        // Si el conteo es igual o mayor al límite, rechazamos la petición.
        if (currentDbCount >= databaseLimitPerEngine)
        {
            var planName = user.Plan?.Name ?? "Gratuito";
            return BadRequest(new { message = $"Has alcanzado el límite de {databaseLimitPerEngine} bases de datos para el motor {createDto.Engine} en tu plan {planName}. Mejora tu plan para crear más bases de datos." });
>>>>>>> a9e68670a5dc89e2128cddadaa203fed88386d58
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

        return NoContent();
    }
}
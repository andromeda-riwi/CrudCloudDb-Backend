﻿using System.Security.Claims;
using CCD.Infrastructure.Data;
using CCD.Core;
using CCD.Core.Dtos;
using CCD.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CCD.Api.Dtos;

namespace CCD.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuthRepository _authRepository;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        ApplicationDbContext context,
        IAuthRepository authRepository,
        ILogger<UsersController> logger)
    {
        _context = context;
        _authRepository = authRepository;
        _logger = logger;
    }

    // Ruta: GET /api/users/me
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _context.Users
            .Select(u => new UserResponseDto
            {
                Id = u.Id,
                Name = u.Name,
                LastName = u.LastName,
                UserName = u.UserName,
                Email = u.Email,
                PlanId = u.PlanId
            })
            .FirstOrDefaultAsync(u => u.Id == Guid.Parse(userId));

        if (user == null)
        {
            return NotFound("Usuario no encontrado.");
        }

        return Ok(user);
    }

    /// <summary>
    /// Obtener información del plan actual del usuario
    /// GET /api/users/plan
    /// </summary>
    [HttpGet("plan")]
    public async Task<IActionResult> GetUserPlan()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        var user = await _context.Users
            .Include(u => u.Plan)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return NotFound("Usuario no encontrado.");
        }

        // Obtener cantidad de BD por motor
        var databases = await _context.DatabaseInstances
            .Where(db => db.UserId == userId)
            .GroupBy(db => db.Engine)
            .ToDictionaryAsync(g => g.Key, g => g.Count());

        var totalDatabases = databases.Values.Sum();

        var planDto = new UserPlanDto
        {
            PlanId = user.PlanId,
            PlanName = user.Plan?.Name ?? "Gratuito",
            DatabaseLimitPerEngine = user.Plan?.DatabaseLimitPerEngine ?? 2,
            MonthlyPrice = user.Plan?.Price ?? 0,
            DatabaseCountByEngine = databases,
            TotalDatabases = totalDatabases,
            NextBillingDate = user.PlanRenewalDate,
            CanUpgrade = (user.Plan?.Id ?? 1) < 3 // Puedes upgradear si no estás en el plan más alto
        };

        return Ok(planDto);
    }

    /// <summary>
    /// Cambiar contraseña del usuario
    /// POST /api/users/change-password
    /// </summary>
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        // Validaciones
        if (string.IsNullOrWhiteSpace(dto.CurrentPassword) ||
            string.IsNullOrWhiteSpace(dto.NewPassword) ||
            string.IsNullOrWhiteSpace(dto.ConfirmPassword))
        {
            return BadRequest(new { message = "Todos los campos son requeridos." });
        }

        if (dto.NewPassword != dto.ConfirmPassword)
        {
            return BadRequest(new { message = "Las contraseñas nuevas no coinciden." });
        }

        if (dto.NewPassword.Length < 6)
        {
            return BadRequest(new { message = "La contraseña debe tener al menos 6 caracteres." });
        }

        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var success = await _authRepository.ChangePasswordAsync(userId, dto.CurrentPassword, dto.NewPassword);

            if (!success)
            {
                return BadRequest(new { message = "La contraseña actual es incorrecta." });
            }

            _logger.LogInformation("Usuario {UserId} cambió su contraseña exitosamente", userId);
            return Ok(new { message = "Contraseña actualizada exitosamente." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cambiar contraseña para usuario {UserId}", userId);
            return StatusCode(500, new { message = "Error al cambiar la contraseña." });
        }
    }

    /// <summary>
    /// Actualizar perfil del usuario
    /// PUT /api/users/profile
    /// </summary>
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return NotFound("Usuario no encontrado.");
        }

        // Actualizar campos que se proporcionaron
        if (!string.IsNullOrWhiteSpace(dto.Name))
            user.Name = dto.Name;

        if (!string.IsNullOrWhiteSpace(dto.LastName))
            user.LastName = dto.LastName;

        if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != user.Email)
        {
            // Verificar que el email no esté en uso
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email && u.Id != userId);
            
            if (existingUser != null)
            {
                return BadRequest(new { message = "El email ya está en uso." });
            }

            user.Email = dto.Email;
            user.EmailVerified = false; // Requerir re-verificación
        }

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Perfil del usuario {UserId} actualizado", userId);

            return Ok(new
            {
                message = "Perfil actualizado exitosamente.",
                user = new
                {
                    user.Id,
                    user.Name,
                    user.LastName,
                    user.Email,
                    user.UserName
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar perfil del usuario {UserId}", userId);
            return StatusCode(500, new { message = "Error al actualizar el perfil." });
        }
    }
}
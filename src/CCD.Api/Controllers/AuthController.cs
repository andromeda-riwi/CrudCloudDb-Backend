﻿// --- Imports necesarios para el controlador ---
using CCD.Api.Dtos;
using CCD.Core;
using CCD.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CCD.Api.Controllers;

[ApiController]
[Route("api/[controller]")] // Ruta base: /api/auth
public class AuthController : ControllerBase
{
    private readonly IAuthRepository _authRepo;
    private readonly IEmailService _emailService;

    public AuthController(IAuthRepository authRepo, IEmailService emailService)
    {
        _authRepo = authRepo;
        _emailService = emailService;
    }

    // --- ENDPOINT DE REGISTRO ---
    [HttpPost("register")] // Ruta: POST /api/auth/register
    public async Task<IActionResult> Register(UserRegisterDto request)
    {
        var userToCreate = new User
        {
            Name = request.Name,
            LastName = request.LastName,
            UserName = request.UserName,
            Email = request.Email
        };

        var createdUser = await _authRepo.Register(userToCreate, request.Password);

        if (createdUser == null)
        {
            return BadRequest("El correo electrónico o nombre de usuario ya está en uso.");
        }

        return Ok(new { message = "Usuario registrado exitosamente." });
    }


    // ▼▼▼ ESTE ES EL NUEVO MÉTODO QUE ESTÁS AÑADIENDO ▼▼▼
    
    // --- ENDPOINT DE LOGIN ---
    [HttpPost("login")] // Ruta: POST /api/auth/login
    public async Task<IActionResult> Login(UserLoginDto request)
    {
        // Determinar si se está usando email o username
        string identifier = !string.IsNullOrEmpty(request.Email) ? request.Email : request.UserName ?? "";
        string loginType = !string.IsNullOrEmpty(request.Email) ? "email" : "username";
        
        Console.WriteLine($"[LOGIN] Intento de login con {loginType}: {identifier}");
        
        // Validar que al menos uno esté presente
        if (string.IsNullOrEmpty(identifier))
        {
            return BadRequest(new { message = "Debes proporcionar un email o nombre de usuario." });
        }
        
        // Llama al método Login del repositorio
        var token = await _authRepo.Login(identifier, request.Password, loginType == "email");

        // Si el repositorio devuelve null, significa que las credenciales son inválidas.
        if (token == null)
        {
            Console.WriteLine($"[LOGIN] Login fallido para: {identifier}");
            return Unauthorized(new { message = "Credenciales inválidas." });
        }

        // Verificar si el email está verificado
        // Obtener el usuario para verificar el estado del email
        User? user = null;
        if (loginType == "email")
        {
            user = await _authRepo.GetUserByEmailAsync(identifier);
        }
        else
        {
            user = await _authRepo.GetUserByUserNameAsync(identifier);
        }

        var emailVerified = user != null ? await _authRepo.IsEmailVerifiedAsync(user.Id) : false;

        Console.WriteLine($"[LOGIN] Login exitoso para: {identifier}");
        return Ok(new { 
            token,
            emailVerified = emailVerified
        });
    }
    
    // --- ENDPOINT DE VERIFICACIÓN DE EMAIL ---
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(VerifyEmailDto request)
    {
        if (string.IsNullOrEmpty(request.Token))
        {
            return BadRequest(new { message = "El token de verificación es requerido." });
        }

        var verified = await _authRepo.VerifyEmailAsync(request.Token);

        if (!verified)
        {
            return BadRequest(new { message = "Token de verificación inválido o expirado." });
        }

        return Ok(new { message = "Email verificado exitosamente." });
    }

    // --- ENDPOINT PARA REENVIAR VERIFICACIÓN ---
    [HttpPost("resend-verification")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> ResendVerification()
    {
        var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userIdString == null || !Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized(new { message = "Usuario no autenticado." });
        }

        var isVerified = await _authRepo.IsEmailVerifiedAsync(userId);
        if (isVerified)
        {
            return BadRequest(new { message = "El email ya está verificado." });
        }

        try
        {
            var token = await _authRepo.GenerateEmailVerificationTokenAsync(userId);
            
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var userName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "Usuario";
            
            if (!string.IsNullOrEmpty(userEmail))
            {
                try
                {
                    await _emailService.SendEmailVerificationAsync(userEmail, userName, token);
                }
                catch (Exception ex)
                {
                    // No fallar si el correo no se puede enviar, pero loguear el error
                    return StatusCode(500, new { message = "Error al enviar correo de verificación.", detail = ex.Message });
                }
            }

            return Ok(new { message = "Token de verificación generado. Revisa tu correo." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al generar token de verificación.", detail = ex.Message });
        }
    }

    // --- ENDPOINT DE RECUPERACIÓN DE CONTRASEÑA ---
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto request)
    {
        if (string.IsNullOrEmpty(request.Email))
        {
            return BadRequest(new { message = "El email es requerido." });
        }

        var token = await _authRepo.GeneratePasswordResetTokenAsync(request.Email);

        // Por seguridad, siempre retornamos el mismo mensaje, incluso si el email no existe
        // Pero si el token se generó, enviamos el correo
        if (token != null)
        {
            try
            {
                var user = await _authRepo.GetUserByEmailAsync(request.Email);
                var userName = user?.UserName ?? user?.Name ?? request.Email;
                await _emailService.SendPasswordResetEmailAsync(request.Email, userName, token);
            }
            catch (Exception)
            {
                // No revelar si el email existe o no por seguridad
                // Solo loguear el error internamente
            }
        }

        return Ok(new { message = "Si el email existe, se ha enviado un enlace de recuperación." });
    }

    // --- ENDPOINT DE RESTABLECIMIENTO DE CONTRASEÑA ---
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto request)
    {
        if (string.IsNullOrEmpty(request.Token) || string.IsNullOrEmpty(request.NewPassword))
        {
            return BadRequest(new { message = "El token y la nueva contraseña son requeridos." });
        }

        if (request.NewPassword.Length < 6)
        {
            return BadRequest(new { message = "La contraseña debe tener al menos 6 caracteres." });
        }

        var reset = await _authRepo.ResetPasswordAsync(request.Token, request.NewPassword);

        if (!reset)
        {
            return BadRequest(new { message = "Token de recuperación inválido o expirado." });
        }

        return Ok(new { message = "Contraseña restablecida exitosamente." });
    }

    // --- ENDPOINT TEMPORAL DE DEBUG (ELIMINAR EN PRODUCCIÓN) ---
    [HttpGet("debug/user/{email}")]
    public async Task<IActionResult> DebugUser(string email)
    {
        var exists = await _authRepo.UserExists(email);
        return Ok(new { exists, email });
    }
    // ▲▲▲ ▲▲▲ ▲▲▲ ▲▲▲ ▲▲▲ ▲▲▲ ▲▲▲ ▲▲▲ ▲▲▲ ▲▲▲ ▲▲▲ ▲▲▲ ▲▲▲
}
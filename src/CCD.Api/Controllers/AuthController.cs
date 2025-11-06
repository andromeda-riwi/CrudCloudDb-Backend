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

    public AuthController(IAuthRepository authRepo)
    {
        _authRepo = authRepo;
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

        Console.WriteLine($"[LOGIN] Login exitoso para: {identifier}");
        return Ok(new { token });
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
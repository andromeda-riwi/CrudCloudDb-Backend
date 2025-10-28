// --- Imports necesarios para el controlador ---
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
            Email = request.Email
        };

        var createdUser = await _authRepo.Register(userToCreate, request.Password);

        if (createdUser == null)
        {
            return BadRequest("El correo electrónico ya está en uso.");
        }

        return Ok(new { message = "Usuario registrado exitosamente." });
    }


    // ▼▼▼ ESTE ES EL NUEVO MÉTODO QUE ESTÁS AÑADIENDO ▼▼▼
    
    // --- ENDPOINT DE LOGIN ---
    [HttpPost("login")] // Ruta: POST /api/auth/login
    public async Task<IActionResult> Login(UserLoginDto request)
    {
        // Llama al método Login del repositorio, que hace todo el trabajo pesado.
        var token = await _authRepo.Login(request.Email, request.Password);

        // Si el repositorio devuelve null, significa que las credenciales son inválidas.
        if (token == null)
        {
            // Devolvemos un 401 Unauthorized, que es el código de estado correcto
            // para un intento de login fallido.
            return Unauthorized("Credenciales inválidas.");
        }

        // Si el login es exitoso, devolvemos un 200 OK con el token JWT.
        // El frontend guardará este token para usarlo en futuras peticiones.
        return Ok(new { token });
    }
    // ▲▲▲ ▲▲▲ ▲▲▲ ▲▲▲ ▲▲▲ ▲▲▲ ▲▲▲ ▲▲▲ ▲▲▲ ▲▲▲ ▲▲▲ ▲▲▲ ▲▲▲
}
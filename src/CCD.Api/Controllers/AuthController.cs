using CCD.Api.Dtos;
using CCD.Core;
using CCD.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CCD.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Ruta base: /api/auth
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _authRepo;

        // El repositorio se inyecta a través del constructor (Inyección de Dependencias)
        public AuthController(IAuthRepository authRepo)
        {
            _authRepo = authRepo;
        }

        // --- ENDPOINT DE REGISTRO ---
        [HttpPost("register")] // Ruta: POST /api/auth/register
        public async Task<IActionResult> Register(UserRegisterDto request)
        {
            // 1. Verificamos si el email o el username ya existen para dar un error claro.
            if (await _authRepo.UserExists(request.Email) || await _authRepo.UserExists(request.UserName))
            {
                return BadRequest("El correo electrónico o el nombre de usuario ya están en uso.");
            }

            // 2. Mapeamos los datos del DTO a la entidad User que se guardará en la BD.
            var userToCreate = new User
            {
                Name = request.Name,
                LastName = request.LastName,
                UserName = request.UserName,
                Email = request.Email
            };

            // 3. Llamamos al repositorio para que cree el usuario y hashee la contraseña.
            var createdUser = await _authRepo.Register(userToCreate, request.Password);

            // 4. Si por alguna razón la creación falla, devolvemos un error.
            if (createdUser == null)
            {
                return StatusCode(500, "No se pudo crear el usuario en este momento.");
            }

            // 5. Si todo sale bien, devolvemos un 200 OK.
            return Ok(new { message = "Usuario registrado exitosamente." });
        }
        
        // --- ENDPOINT DE LOGIN ---
        [HttpPost("login")] // Ruta: POST /api/auth/login
        public async Task<IActionResult> Login(UserLoginDto request)
        {
            // 1. Llamamos al repositorio pasándole el identificador y la contraseña como strings.
            // El repositorio se encargará de la lógica de buscar por email o username.
            var token = await _authRepo.Login(request.Identifier, request.Password);

            // 2. Si el token es nulo, significa que las credenciales son inválidas.
            if (string.IsNullOrEmpty(token))
            {
                // Devolvemos 401 Unauthorized, el código estándar para un login fallido.
                return Unauthorized("Credenciales inválidas.");
            }
            
            // 3. Si el login es exitoso, devolvemos el token en un objeto JSON.
            return Ok(new { token });
        }
    }
}
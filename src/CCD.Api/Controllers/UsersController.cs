using System.Security.Claims;
using CCD.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization; // <-- Importante
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CCD.Api.Dtos;

namespace CCD.Api.Controllers;

[Authorize] // <-- ¡ESTA ES LA MAGIA!
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public UsersController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Ruta: GET /api/users/me
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        // El atributo [Authorize] asegura que esta línea solo se ejecutará si el token es válido.
        // Podemos acceder a los "claims" del token para obtener la información del usuario.
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            return Unauthorized(); // Si por alguna razón no se encuentra el ID en el token
        }

        var user = await _context.Users
            .Select(u => new UserResponseDto // Usamos el DTO para no exponer datos sensibles
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
}
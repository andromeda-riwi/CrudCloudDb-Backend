using CCD.Api.Dtos;
using CCD.Core.Interfaces;
using CCD.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CCD.Api.Controllers
{
    [Authorize] // ¡Todo en este controlador requiere que el usuario esté autenticado!
    [ApiController]
    [Route("api/databases")] // Ruta base unificada a /api/databases
    public class DatabaseController : ControllerBase
    {
        private readonly IDatabaseProvisioner _provisioner;
        private readonly ApplicationDbContext _context;

        public DatabaseController(IDatabaseProvisioner provisioner, ApplicationDbContext context)
        {
            _provisioner = provisioner;
            _context = context;
        }

        // Este método ya estaba completo.
        [HttpGet]
        public async Task<IActionResult> GetUserDatabases()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized();
            }

            var databases = await _provisioner.GetUserDatabasesAsync(Guid.Parse(userIdString));
            return Ok(databases);
        }

        // Este método ya estaba completo.
        [HttpPost]
        public async Task<IActionResult> CreateDatabase([FromBody] CreateDatabaseDto request)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized();
            }

            var user = await _context.Users.FindAsync(Guid.Parse(userIdString));
            if (user == null)
            {
                return Unauthorized();
            }

            var createdInstance = await _provisioner.CreateDatabaseAsync(user, request.Engine);

            if (createdInstance == null)
            {
                return BadRequest("No se pudo crear la base de datos. Es posible que hayas alcanzado el límite de tu plan.");
            }
            
            return Ok(createdInstance);
        }

        // ========================================================================
        // --- ESTE ES EL MÉTODO QUE ESTAMOS IMPLEMENTANDO ---
        // ========================================================================
        [HttpDelete("{id}")] // Ruta: DELETE /api/databases/xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
        public async Task<IActionResult> DeleteDatabase(Guid id)
        {
            // 1. Obtener el ID del usuario desde el token JWT.
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized();
            }

            // 2. Buscar el objeto User completo en nuestra base de datos.
            var user = await _context.Users.FindAsync(Guid.Parse(userIdString));
            if (user == null)
            {
                return Unauthorized();
            }

            // 3. Llamar al servicio de aprovisionamiento para que haga el borrado.
            var success = await _provisioner.DeleteDatabaseAsync(id, user);

            // 4. Devolver un resultado basado en el éxito de la operación.
            if (success)
            {
                // 204 No Content es una respuesta estándar y correcta para un DELETE exitoso.
                return NoContent();
            }
            
            // Si no tuvo éxito, es porque la BD no existe o no pertenece al usuario.
            // 404 Not Found es un código apropiado en este caso.
            return NotFound("No se encontró la base de datos o no tienes permiso para borrarla.");
        }
    }
}
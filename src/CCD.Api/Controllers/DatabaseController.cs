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

        // Inyectamos tanto el provisioner (para la lógica de BD) como el DbContext (para encontrar al usuario)
        public DatabaseController(IDatabaseProvisioner provisioner, ApplicationDbContext context)
        {
            _provisioner = provisioner;
            _context = context;
        }

        // GET: api/databases
        [HttpGet]
        public async Task<IActionResult> GetUserDatabases()
        {
            // 1. Obtener el ID del usuario desde el token JWT.
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized(); // Token inválido o no contiene el ID.
            }

            // 2. Llamar al método del provisioner para obtener las bases de datos.
            var databases = await _provisioner.GetUserDatabasesAsync(Guid.Parse(userIdString));
            
            // 3. Devolver la lista.
            return Ok(databases);
        }

        // POST: api/databases
        [HttpPost]
        public async Task<IActionResult> CreateDatabase([FromBody] CreateDatabaseDto request)
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
                // Esto no debería pasar si el token es válido, pero es una buena medida de seguridad.
                return Unauthorized();
            }

            // 3. Llamar al servicio de aprovisionamiento, pasándole el usuario y el motor solicitado.
            var createdInstance = await _provisioner.CreateDatabaseAsync(user, request.Engine);

            // 4. Manejar el resultado.
            if (createdInstance == null)
            {
                // Si el provisioner devuelve nulo, es porque el usuario no tiene cuota.
                return BadRequest("No se pudo crear la base de datos. Es posible que hayas alcanzado el límite de tu plan.");
            }

            // Devolvemos un 200 OK con los detalles de la instancia creada.
            return Ok(createdInstance);
        }

        // DELETE: api/databases/{id}
        [HttpDelete("{id}")]
        public  IActionResult DeleteDatabase(Guid id)
        {
            // TODO: Implementar la lógica para borrar una BD.
            // Se necesitará obtener el usuario del token y pasarlo al método DeleteDatabaseAsync
            // del provisioner para verificar que es el dueño de la BD que intenta borrar.
            return Ok($"Borrando la base de datos con ID {id} - LÓGICA PENDIENTE");
        }
    }
}
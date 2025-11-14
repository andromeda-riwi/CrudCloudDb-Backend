Tiene razón. Ya existe un `WebhookController` en `src/CCD.Api/Controllers/WebhookController.cs`.

Sin embargo, el controlador existente es una versión básica y necesita ser actualizado para implementar todos los métodos definidos en `IWebhookService` y para utilizar los DTOs que hemos creado. La versión actual no incluye la funcionalidad para obtener un webhook por ID, actualizarlo o eliminarlo.

A continuación, se presenta la versión actualizada y completa del `WebhookController` que se alinea con la interfaz `IWebhookService` y los DTOs definidos.

```csharp
using CCD.Api.Dtos;
using CCD.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CCD.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/webhooks")] // Se ajusta la ruta para seguir la convención RESTful
    public class WebhookController : ControllerBase
    {
        private readonly IWebhookService _webhookService;

        public WebhookController(IWebhookService webhookService)
        {
            _webhookService = webhookService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateWebhook([FromBody] CreateWebhookDto createDto)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var webhook = await _webhookService.CreateWebhookAsync(userId, createDto.Url, createDto.Event, createDto.Secret, createDto.IsActive);
            return CreatedAtAction(nameof(GetWebhookById), new { id = webhook.Id }, webhook);
        }

        [HttpGet]
        public async Task<IActionResult> GetUserWebhooks()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var webhooks = await _webhookService.GetUserWebhooksAsync(userId);
            return Ok(webhooks);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetWebhookById(Guid id)
        {
            var webhook = await _webhookService.GetWebhookByIdAsync(id);
            if (webhook == null)
            {
                return NotFound();
            }

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (webhook.UserId != userId)
            {
                return Forbid();
            }

            return Ok(webhook);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateWebhook(Guid id, [FromBody] UpdateWebhookDto updateDto)
        {
            var webhook = await _webhookService.GetWebhookByIdAsync(id);
            if (webhook == null)
            {
                return NotFound();
            }

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (webhook.UserId != userId)
            {
                return Forbid();
            }

            await _webhookService.UpdateWebhookAsync(id, updateDto.Url, updateDto.Event, updateDto.Secret, updateDto.IsActive);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWebhook(Guid id)
        {
            var webhook = await _webhookService.GetWebhookByIdAsync(id);
            if (webhook == null)
            {
                return NotFound();
            }

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (webhook.UserId != userId)
            {
                return Forbid();
            }

            await _webhookService.DeleteWebhookAsync(id);
            return NoContent();
        }
    }
}

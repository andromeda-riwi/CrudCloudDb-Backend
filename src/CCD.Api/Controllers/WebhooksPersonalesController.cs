using System.Security.Claims;
using CCD.Api.Dtos;
using CCD.Core;
using CCD.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CCD.Api.Controllers;

/// <summary>
/// Controlador para gestionar webhooks personalizados del usuario
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class WebhooksController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(ApplicationDbContext context, ILogger<WebhooksController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Obtener todos los webhooks del usuario autenticado
    /// GET /api/webhooks
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetWebhooks()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        var webhooks = await _context.Webhooks
            .Where(w => w.UserId == userId)
            .Select(w => new WebhookResponseDto
            {
                Id = w.Id,
                Url = w.Url,
                Event = w.Event,
                IsActive = w.IsActive,
                CreatedAt = w.CreatedAt,
                UpdatedAt = w.UpdatedAt
            })
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();

        return Ok(new
        {
            total = webhooks.Count,
            webhooks = webhooks
        });
    }

    /// <summary>
    /// Obtener un webhook específico del usuario
    /// GET /api/webhooks/{id}
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetWebhook(Guid id)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        var webhook = await _context.Webhooks
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

        if (webhook == null)
        {
            return NotFound(new { message = "Webhook no encontrado." });
        }

        return Ok(new WebhookResponseDto
        {
            Id = webhook.Id,
            Url = webhook.Url,
            Event = webhook.Event,
            IsActive = webhook.IsActive,
            CreatedAt = webhook.CreatedAt,
            UpdatedAt = webhook.UpdatedAt
        });
    }

    /// <summary>
    /// Crear un nuevo webhook personalizado
    /// POST /api/webhooks
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateWebhook([FromBody] CreateWebhookDto dto)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        // Validaciones
        if (string.IsNullOrWhiteSpace(dto.Url))
        {
            return BadRequest(new { message = "La URL del webhook es requerida." });
        }

        if (string.IsNullOrWhiteSpace(dto.Event))
        {
            return BadRequest(new { message = "El tipo de evento es requerido." });
        }

        // Validar que sea una URL válida
        if (!Uri.TryCreate(dto.Url, UriKind.Absolute, out var uriResult) || 
            (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
        {
            return BadRequest(new { message = "La URL del webhook debe ser válida (HTTP o HTTPS)." });
        }

        // Validar eventos soportados
        var supportedEvents = new[] { "database.created", "database.deleted", "payment.completed", "user.created" };
        if (!supportedEvents.Contains(dto.Event.ToLower()))
        {
            return BadRequest(new { message = $"Evento no soportado. Eventos válidos: {string.Join(", ", supportedEvents)}" });
        }

        try
        {
            var webhook = new Webhook
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Url = dto.Url,
                Event = dto.Event.ToLower(),
                Secret = !string.IsNullOrWhiteSpace(dto.Secret) ? dto.Secret : GenerateWebhookSecret(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Webhooks.AddAsync(webhook);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Webhook creado para usuario {UserId}: {WebhookId}", userId, webhook.Id);

            return CreatedAtAction(nameof(GetWebhook), new { id = webhook.Id }, new WebhookResponseDto
            {
                Id = webhook.Id,
                Url = webhook.Url,
                Event = webhook.Event,
                IsActive = webhook.IsActive,
                CreatedAt = webhook.CreatedAt,
                UpdatedAt = webhook.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear webhook para usuario {UserId}", userId);
            return StatusCode(500, new { message = "Error al crear el webhook." });
        }
    }

    /// <summary>
    /// Actualizar un webhook existente
    /// PUT /api/webhooks/{id}
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateWebhook(Guid id, [FromBody] CreateWebhookDto dto)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        var webhook = await _context.Webhooks
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

        if (webhook == null)
        {
            return NotFound(new { message = "Webhook no encontrado." });
        }

        // Validaciones
        if (!string.IsNullOrWhiteSpace(dto.Url))
        {
            if (!Uri.TryCreate(dto.Url, UriKind.Absolute, out var uriResult) || 
                (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
            {
                return BadRequest(new { message = "La URL del webhook debe ser válida (HTTP o HTTPS)." });
            }
            webhook.Url = dto.Url;
        }

        if (!string.IsNullOrWhiteSpace(dto.Event))
        {
            var supportedEvents = new[] { "database.created", "database.deleted", "payment.completed", "user.created" };
            if (!supportedEvents.Contains(dto.Event.ToLower()))
            {
                return BadRequest(new { message = $"Evento no soportado. Eventos válidos: {string.Join(", ", supportedEvents)}" });
            }
            webhook.Event = dto.Event.ToLower();
        }

        if (!string.IsNullOrWhiteSpace(dto.Secret))
        {
            webhook.Secret = dto.Secret;
        }

        webhook.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Webhook actualizado para usuario {UserId}: {WebhookId}", userId, webhook.Id);

            return Ok(new WebhookResponseDto
            {
                Id = webhook.Id,
                Url = webhook.Url,
                Event = webhook.Event,
                IsActive = webhook.IsActive,
                CreatedAt = webhook.CreatedAt,
                UpdatedAt = webhook.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar webhook {WebhookId}", id);
            return StatusCode(500, new { message = "Error al actualizar el webhook." });
        }
    }

    /// <summary>
    /// Cambiar estado de un webhook (activar/desactivar)
    /// PATCH /api/webhooks/{id}/toggle
    /// </summary>
    [HttpPatch("{id}/toggle")]
    public async Task<IActionResult> ToggleWebhook(Guid id)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        var webhook = await _context.Webhooks
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

        if (webhook == null)
        {
            return NotFound(new { message = "Webhook no encontrado." });
        }

        webhook.IsActive = !webhook.IsActive;
        webhook.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Estado del webhook {WebhookId} cambiado a {IsActive}", id, webhook.IsActive);

            return Ok(new
            {
                message = $"Webhook {(webhook.IsActive ? "activado" : "desactivado")} exitosamente.",
                isActive = webhook.IsActive
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cambiar estado del webhook {WebhookId}", id);
            return StatusCode(500, new { message = "Error al cambiar el estado del webhook." });
        }
    }

    /// <summary>
    /// Eliminar un webhook personalizado
    /// DELETE /api/webhooks/{id}
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWebhook(Guid id)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        var webhook = await _context.Webhooks
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

        if (webhook == null)
        {
            return NotFound(new { message = "Webhook no encontrado." });
        }

        try
        {
            // Eliminar también los eventos asociados
            var events = await _context.WebhookEvents
                .Where(e => e.WebhookId == id)
                .ToListAsync();

            _context.WebhookEvents.RemoveRange(events);
            _context.Webhooks.Remove(webhook);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Webhook eliminado para usuario {UserId}: {WebhookId}", userId, webhook.Id);

            return Ok(new { message = "Webhook eliminado exitosamente." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar webhook {WebhookId}", id);
            return StatusCode(500, new { message = "Error al eliminar el webhook." });
        }
    }

    /// <summary>
    /// Obtener el historial de eventos enviados a un webhook
    /// GET /api/webhooks/{id}/history
    /// </summary>
    [HttpGet("{id}/history")]
    public async Task<IActionResult> GetWebhookHistory(Guid id)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        var webhook = await _context.Webhooks
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

        if (webhook == null)
        {
            return NotFound(new { message = "Webhook no encontrado." });
        }

        var events = await _context.WebhookEvents
            .Where(e => e.WebhookId == id)
            .OrderByDescending(e => e.TriggeredAt)
            .Take(50) // Limitar a los últimos 50 eventos
            .Select(e => new
            {
                e.Id,
                e.EventType,
                e.Status,
                e.Response,
                e.TriggeredAt
            })
            .ToListAsync();

        return Ok(new
        {
            webhookId = id,
            total = events.Count,
            events = events
        });
    }

    /// <summary>
    /// Probar un webhook enviando un evento de prueba
    /// POST /api/webhooks/{id}/test
    /// </summary>
    [HttpPost("{id}/test")]
    public async Task<IActionResult> TestWebhook(Guid id)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        var webhook = await _context.Webhooks
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

        if (webhook == null)
        {
            return NotFound(new { message = "Webhook no encontrado." });
        }

        if (!webhook.IsActive)
        {
            return BadRequest(new { message = "El webhook debe estar activo para enviarse una prueba." });
        }

        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var testPayload = new
            {
                @event = "test.webhook",
                timestamp = DateTime.UtcNow,
                data = new
                {
                    message = "Este es un evento de prueba",
                    webhookId = id
                }
            };

            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(testPayload),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync(webhook.Url, content);

            _logger.LogInformation("Webhook de prueba enviado a {Url}: Status {StatusCode}", webhook.Url, response.StatusCode);

            return Ok(new
            {
                message = "Webhook de prueba enviado exitosamente.",
                statusCode = (int)response.StatusCode,
                success = response.IsSuccessStatusCode
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error al enviar webhook de prueba a {Url}", webhook.Url);
            return StatusCode(500, new { message = "Error al enviar webhook de prueba.", detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al probar webhook {WebhookId}", id);
            return StatusCode(500, new { message = "Error inesperado al probar webhook." });
        }
    }

    /// <summary>
    /// Generar un secret seguro para el webhook
    /// </summary>
    private string GenerateWebhookSecret()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Range(0, 32).Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }
}


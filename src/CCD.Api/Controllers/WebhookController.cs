using CCD.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CCD.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WebhookController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<WebhookController> _logger; // <-- CORREGIDO AQUÍ

    // Y CORREGIDO AQUÍ ▼▼▼
    public WebhookController(IPaymentService paymentService, ILogger<WebhookController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    [HttpPost("mercadopago")]
    [AllowAnonymous]
    public async Task<IActionResult> ReceiveMercadoPagoNotification([FromBody] JsonElement body)
    {
        _logger.LogInformation("Notificación de Mercado Pago recibida: {Body}", body.ToString());

        var topic = body.TryGetProperty("action", out var action) ? action.GetString() : null;
        var paymentIdString = body.TryGetProperty("data", out var data) && data.TryGetProperty("id", out var id) ? id.GetString() : null;

        if (topic?.StartsWith("payment.") == true && long.TryParse(paymentIdString, out var paymentId))
        {
            _logger.LogInformation("Procesando notificación para el pago ID: {PaymentId}", paymentId);
            await _paymentService.ProcessPaymentNotificationAsync(paymentId);
        }
        else
        {
            _logger.LogWarning("Notificación no reconocida o sin ID de pago válido.");
        }

        return Ok();
    }
}
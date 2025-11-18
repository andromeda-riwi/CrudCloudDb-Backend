using CCD.Api.Dtos;
using CCD.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography; //to improve the security 
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Primitives;

[Route("api/[controller]")]
[ApiController]
public class WebhookController : ControllerBase
{
    private readonly IPaymentService _paymentService; //Mercado pago IPaymentService
    private readonly ILogger<WebhookController> _logger;
    private readonly IConfiguration _configuration; // IConfiguration

    public WebhookController(IPaymentService paymentService, ILogger<WebhookController> logger, IConfiguration configuration)
    {
        _paymentService = paymentService;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpPost("mercadopago")]
    [AllowAnonymous]
    public async Task<IActionResult> ReceiveMercadoPagoNotification([FromBody] MercadoPagoNotificationsDto notification)
    {
        // --- Init of firm validation ---
        if (!Request.Headers.TryGetValue("X-Signature", out StringValues signatureHeader))
        {
            _logger.LogWarning("Webhook de Mercado Pago recibido sin la cabecera X-Signature.");
            return BadRequest("Firma no encontrada.");
        }

        var webhookSecret = _configuration["MercadoPago:WebhookSecret"];
        if (string.IsNullOrEmpty(webhookSecret))
        {
            _logger.LogError("El Webhook Secret de Mercado Pago no está configurado.");
            return StatusCode(500, "Configuración del servidor incompleta.");
        }

        // FIRM in format ts=<timestamp>,v1=<hash>
        var parts = signatureHeader.ToString().Split(',');
        var timestamp = parts.FirstOrDefault(p => p.StartsWith("ts="))?.Substring(3);
        var hash = parts.FirstOrDefault(p => p.StartsWith("v1="))?.Substring(3);

        if (string.IsNullOrEmpty(timestamp) || string.IsNullOrEmpty(hash) || notification.Data == null)
        {
            return BadRequest("Formato de firma inválido.");
        }
        
        // manifest of the mercado pago payment
        var manifest = $"id:{notification.Data.Id};request-id:{Request.Headers["X-Request-Id"]};ts:{timestamp};"; //this part should be like do get mercado pago by security

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(webhookSecret));
        var computedHashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest));
        var computedHash = BitConverter.ToString(computedHashBytes).Replace("-", "").ToLower();

        if (computedHash != hash)
        {
            _logger.LogWarning("¡Firma de Webhook inválida! Se recibió una notificación potencialmente fraudulenta.");
            return Unauthorized("Firma inválida.");
        }
        // -end validation of the FIRM ---

        _logger.LogInformation("Notificación de Mercado Pago recibida y validada: {Body}", JsonSerializer.Serialize(notification));

        var topic = notification.Action;
        var paymentIdString = notification.Data?.Id;

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
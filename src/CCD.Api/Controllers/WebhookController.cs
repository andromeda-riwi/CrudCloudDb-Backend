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
        _logger.LogInformation("🔔 ====== WEBHOOK DE MERCADO PAGO RECIBIDO ======");
        _logger.LogInformation("📋 Notificación: {Notification}", JsonSerializer.Serialize(notification));
        
        // --- Init of firm validation ---
        if (!Request.Headers.TryGetValue("X-Signature", out StringValues signatureHeader))
        {
            _logger.LogWarning("⚠️ Webhook de Mercado Pago recibido sin la cabecera X-Signature.");
            return BadRequest("Firma no encontrada.");
        }

        var webhookSecret = _configuration["MercadoPago:WebhookSecret"];
        if (string.IsNullOrEmpty(webhookSecret))
        {
            _logger.LogError("❌ El Webhook Secret de Mercado Pago no está configurado.");
            return StatusCode(500, "Configuración del servidor incompleta.");
        }

        // FIRM in format ts=<timestamp>,v1=<hash>
        var parts = signatureHeader.ToString().Split(',');
        var timestamp = parts.FirstOrDefault(p => p.StartsWith("ts="))?.Substring(3);
        var hash = parts.FirstOrDefault(p => p.StartsWith("v1="))?.Substring(3);

        if (string.IsNullOrEmpty(timestamp) || string.IsNullOrEmpty(hash) || notification.Data == null)
        {
            _logger.LogWarning("⚠️ Formato de firma inválido o datos faltantes");
            return BadRequest("Formato de firma inválido.");
        }
        
        // manifest of the mercado pago payment
        var manifest = $"id:{notification.Data.Id};request-id:{Request.Headers["X-Request-Id"]};ts:{timestamp};";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(webhookSecret));
        var computedHashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest));
        var computedHash = BitConverter.ToString(computedHashBytes).Replace("-", "").ToLower();

        if (computedHash != hash)
        {
            _logger.LogWarning("🚨 ¡Firma de Webhook inválida! Se recibió una notificación potencialmente fraudulenta.");
            _logger.LogWarning("Hash recibido: {ReceivedHash}", hash);
            _logger.LogWarning("Hash calculado: {ComputedHash}", computedHash);
            return Unauthorized("Firma inválida.");
        }
        
        _logger.LogInformation("✅ Firma de webhook validada correctamente");
        // -end validation of the FIRM ---

        _logger.LogInformation("📄 Notificación validada: Action={Action}, Type={Type}, DataId={DataId}", 
            notification.Action, notification.Type, notification.Data?.Id);

        var topic = notification.Action;
        var paymentIdString = notification.Data?.Id;

        if (topic?.StartsWith("payment.") == true && long.TryParse(paymentIdString, out var paymentId))
        {
            _logger.LogInformation("💳 Procesando notificación de pago. Topic: {Topic}, PaymentId: {PaymentId}", topic, paymentId);
            
            try
            {
                await _paymentService.ProcessPaymentNotificationAsync(paymentId);
                _logger.LogInformation("✅ Notificación procesada exitosamente para pago {PaymentId}", paymentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error al procesar notificación para pago {PaymentId}", paymentId);
                return StatusCode(500, "Error procesando webhook");
            }
        }
        else
        {
            _logger.LogWarning("⚠️ Notificación no reconocida o sin ID de pago válido. Topic: {Topic}, DataId: {DataId}", 
                topic, paymentIdString);
        }
        
        _logger.LogInformation("🏁 Webhook procesado. Retornando OK a Mercado Pago.");
        return Ok();
    }
}
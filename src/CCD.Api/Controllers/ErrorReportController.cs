using System.Security.Claims;
using CCD.Api.Dtos;
using CCD.Core;
using CCD.Core.Interfaces;
using CCD.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CCD.Api.Controllers;

/// <summary>
/// Controlador para gestionar errores y enviarlos vía webhooks
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ErrorReportController : ControllerBase
{
    private readonly IWebhookService _webhookService;
    private readonly ILogger<ErrorReportController> _logger;

    public ErrorReportController(IWebhookService webhookService, ILogger<ErrorReportController> logger)
    {
        _webhookService = webhookService;
        _logger = logger;
    }

    /// <summary>
    /// Reportar un error en producción
    /// POST /api/error-report
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> ReportError([FromBody] ErrorReportDto errorReport)
    {
        if (errorReport == null)
        {
            return BadRequest(new { message = "Error report cannot be empty." });
        }

        try
        {
            var errorData = new
            {
                eventType = "error.occurred",
                timestamp = DateTime.UtcNow,
                environment = errorReport.Environment ?? "production",
                exception = errorReport.Exception,
                message = errorReport.Message,
                stackTrace = errorReport.StackTrace,
                source = errorReport.Source,
                userId = errorReport.UserId,
                endpoint = errorReport.Endpoint,
                method = errorReport.Method,
                statusCode = errorReport.StatusCode,
                additionalData = errorReport.AdditionalData
            };

            // Disparar webhook de error
            await _webhookService.TriggerWebhooksAsync("error.occurred", errorData);

            _logger.LogError(
                "Error reportado: {Exception} - {Message} - {StackTrace}",
                errorReport.Exception,
                errorReport.Message,
                errorReport.StackTrace
            );

            return Ok(new { message = "Error reported successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reportar error");
            return StatusCode(500, new { message = "Error reporting failed.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Obtener historial de errores reportados (requiere autenticación)
    /// GET /api/error-report/history
    /// </summary>
    [HttpGet("history")]
    [Authorize]
    public IActionResult GetErrorHistory()
    {
        // Nota: Este endpoint devuelve un mensaje indicando que el historial
        // se mantiene en logs del sistema. En una implementación futura,
        // se podría crear una tabla de ErrorLog en la BD.

        return Ok(new
        {
            message = "Error history is maintained in system logs.",
            note = "Errors are automatically reported to configured webhooks.",
            recommendation = "Check application logs for detailed error information."
        });
    }
}


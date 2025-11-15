using System.Net;
using System.Text.Json;
using CCD.Core.Interfaces;

namespace CCD.Api.Middleware;

/// <summary>
/// Middleware para manejo centralizado de excepciones
/// Asegura que todas las excepciones retornen un formato consistente de error
/// y dispara webhooks de error automáticamente
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebhookService? _webhookService;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IWebhookService? webhookService = null)
    {
        _next = next;
        _logger = logger;
        _webhookService = webhookService;
    }

    public async Task InvokeAsync(HttpContext context, IWebhookService? webhookService = null)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción no controlada en {Path}", context.Request.Path);
            
            // Disparar webhook de error
            try
            {
                var errorData = new
                {
                    eventType = "error.occurred",
                    timestamp = DateTime.UtcNow,
                    exception = ex.GetType().Name,
                    message = ex.Message,
                    stackTrace = ex.StackTrace,
                    endpoint = context.Request.Path,
                    method = context.Request.Method,
                    traceId = context.TraceIdentifier
                };

                var service = webhookService ?? _webhookService;
                if (service != null)
                {
                    await service.TriggerWebhooksAsync("error.occurred", errorData);
                }
            }
            catch (Exception webhookEx)
            {
                _logger.LogError(webhookEx, "Error al disparar webhook de error");
            }

            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse
        {
            message = "Ocurrió un error interno del servidor",
            timestamp = DateTime.UtcNow,
            traceId = context.TraceIdentifier
        };

        switch (exception)
        {
            case ArgumentException argEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.message = argEx.Message;
                break;

            case UnauthorizedAccessException:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response.message = "No autorizado";
                break;

            case KeyNotFoundException:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response.message = "Recurso no encontrado";
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.message = "Error interno del servidor";
                break;
        }

        return context.Response.WriteAsJsonAsync(response);
    }

    public class ErrorResponse
    {
        public string message { get; set; } = string.Empty;
        public DateTime timestamp { get; set; }
        public string traceId { get; set; } = string.Empty;
    }
}



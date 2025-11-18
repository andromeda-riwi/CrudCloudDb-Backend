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

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción no controlada en {Path}", context.Request.Path);

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



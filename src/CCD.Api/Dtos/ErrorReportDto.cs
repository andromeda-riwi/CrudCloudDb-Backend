namespace CCD.Api.Dtos;

/// <summary>
/// DTO para reportar errores a webhooks
/// </summary>
public class ErrorReportDto
{
    /// <summary>
    /// Tipo de excepción (ej: NullReferenceException, ArgumentException)
    /// </summary>
    public string? Exception { get; set; }

    /// <summary>
    /// Mensaje del error
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Stack trace del error
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// Origen del error (ej: DatabaseService, PaymentController)
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// ID del usuario afectado (si aplica)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Endpoint donde ocurrió el error
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Método HTTP (GET, POST, etc)
    /// </summary>
    public string? Method { get; set; }

    /// <summary>
    /// Status code HTTP retornado
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// Entorno donde ocurrió (Development, Production, Staging)
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Datos adicionales del contexto
    /// </summary>
    public Dictionary<string, object>? AdditionalData { get; set; }
}


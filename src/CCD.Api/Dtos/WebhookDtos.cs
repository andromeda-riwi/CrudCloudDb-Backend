namespace CCD.Api.Dtos;

/// <summary>
/// DTO para crear un webhook personalizado
/// </summary>
public class CreateWebhookDto
{
    public string Url { get; set; } = string.Empty;
    public string Event { get; set; } = string.Empty;
    public string? Secret { get; set; }
}

/// <summary>
/// DTO para respuesta de webhook
/// </summary>
public class WebhookResponseDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Event { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}


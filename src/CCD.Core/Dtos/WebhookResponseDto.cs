namespace CCD.Api.Dtos;

public class WebhookResponseDto
{
    public Guid Id { get; set; }
    public string Event { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
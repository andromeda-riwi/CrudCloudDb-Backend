namespace CCD.Api.Dtos;

public class CreateWebhookDto
{
    public string Event { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Secret { get; set; }
    public bool IsActive { get; set; } = true;
}
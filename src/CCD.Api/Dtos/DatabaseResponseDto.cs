namespace CCD.Api.Dtos;

// Esta es una clase simple para devolver datos al frontend de forma segura.
public class DatabaseResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Engine { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
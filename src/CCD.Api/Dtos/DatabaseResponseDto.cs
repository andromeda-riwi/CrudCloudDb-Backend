using System.Text.Json.Serialization;

namespace CCD.Api.Dtos;

// Esta es una clase simple para devolver datos al frontend de forma segura.
public class DatabaseResponseDto
{
    private DateTime _createdAtUtc;

    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Engine { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt
    {
        get => DateTime.SpecifyKind(_createdAtUtc, DateTimeKind.Utc).ToLocalTime();
        set => _createdAtUtc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? CreatedAtUtc => _createdAtUtc != default ? DateTime.SpecifyKind(_createdAtUtc, DateTimeKind.Utc) : null;
    
    // Credenciales de la base de datos
    public string? Host { get; set; }
    public int? Port { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
}
namespace CCD.Api.Dtos;

/// <summary>
/// DTO para actualizar perfil del usuario
/// </summary>
public class UpdateProfileDto
{
    public string? Name { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
}


namespace CCD.Api.Dtos;

public class UserResponseDto
{
    public Guid Id { get; set; }
    // Asígnale un valor por defecto para satisfacer al compilador
    public string Email { get; set; } = string.Empty; 
}
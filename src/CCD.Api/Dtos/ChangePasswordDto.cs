namespace CCD.Api.Dtos;

/// <summary>
/// DTO para cambiar contraseña de usuario
/// </summary>
public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}


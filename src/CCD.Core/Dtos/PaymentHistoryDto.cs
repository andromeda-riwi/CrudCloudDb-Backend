namespace CCD.Core.Dtos;

/// <summary>
/// DTO para historial de pagos
/// </summary>
public class PaymentHistoryDto
{
    public int Id { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Currency { get; set; } = "COP";
    public DateTime CreatedAt { get; set; }
    public string? MercadoPagoId { get; set; }
}
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


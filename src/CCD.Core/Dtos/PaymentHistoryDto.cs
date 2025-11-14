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


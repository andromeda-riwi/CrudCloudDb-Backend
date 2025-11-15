using System.ComponentModel.DataAnnotations;

namespace CCD.Api.Dtos;

public class ConfirmPaymentRequestDto
{
    [Required]
    [Range(1, long.MaxValue, ErrorMessage = "El identificador del pago debe ser un número positivo.")]
    public long PaymentId { get; set; }
}
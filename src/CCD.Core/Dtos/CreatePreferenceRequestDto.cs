using System.ComponentModel.DataAnnotations;


namespace CCD.Core.Dtos;

public class CreatePreferenceRequestDto
{
    [Required]
    [Range(2, 3, ErrorMessage = "El PlanId debe ser 2 (Intermedio) o 3 (Avanzado).")]
    public int PlanId { get; set; }
}
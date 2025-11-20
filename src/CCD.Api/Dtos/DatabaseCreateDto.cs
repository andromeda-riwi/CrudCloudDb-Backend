using System.ComponentModel.DataAnnotations;

namespace CCD.Api.Dtos;

public class DatabaseCreateDto
{
    [Required(ErrorMessage = "El motor de base de datos es requerido")]
    [RegularExpression(@"^(PostgreSQL|MySQL|MongoDB|SQLServer)$", 
        ErrorMessage = "Motor no válido. Los motores permitidos son: PostgreSQL, MySQL, MongoDB, SQLServer")]
    public string Engine { get; set; } = string.Empty;

    [Required(ErrorMessage = "La zona horaria es requerida")]
    [StringLength(100, ErrorMessage = "La zona horaria no puede exceder 100 caracteres")]
    public string TimeZoneId { get; set; } = "UTC";
}
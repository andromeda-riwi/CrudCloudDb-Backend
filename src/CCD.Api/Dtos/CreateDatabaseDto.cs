using System.ComponentModel.DataAnnotations;

namespace CCD.Api.Dtos
{
    public class CreateDatabaseDto
    {
        [Required]
        [RegularExpression("^(PostgreSQL|MySQL)$", ErrorMessage = "El motor debe ser PostgreSQL o MySQL.")]
        public string Engine { get; set; } = string.Empty;
    }
}
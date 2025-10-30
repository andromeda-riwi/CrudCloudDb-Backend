using System.ComponentModel.DataAnnotations;

namespace CCD.Api.Dtos
{
    public class UserLoginDto
    {
        [Required(ErrorMessage = "El nombre de usuario o correo es obligatorio.")]
        public string Identifier { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        public string Password { get; set; } = string.Empty;
    }
}
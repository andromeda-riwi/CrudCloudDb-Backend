// Añadimos esta directiva 'using' para poder usar [Required]
using System.ComponentModel.DataAnnotations;

namespace CCD.Api.Dtos;

// Esta clase define los datos que el frontend debe enviar para crear una base de datos.
public class DatabaseCreateDto
{
    // Solo necesitamos que nos digan qué motor quieren crear.
    [Required]
    public string Engine { get; set; } = string.Empty;
}
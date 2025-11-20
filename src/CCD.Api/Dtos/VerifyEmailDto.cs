using System.ComponentModel.DataAnnotations;

namespace CCD.Api.Dtos;

public class VerifyEmailDto
{
    [Required]
    public string Token { get; set; } = string.Empty;
}


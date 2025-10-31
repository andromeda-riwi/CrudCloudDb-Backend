﻿using System.ComponentModel.DataAnnotations;

namespace CCD.Api.Dtos;

public class UserLoginDto
{
    // El usuario puede enviar email o username (pero no ambos)
    public string? Email { get; set; }
    
    public string? UserName { get; set; }

    [Required]
    public string Password { get; set; } = string.Empty;
}
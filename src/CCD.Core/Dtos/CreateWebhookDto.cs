﻿using System.ComponentModel.DataAnnotations;

namespace CCD.Api.Dtos;

public class CreateWebhookDto
{
    [Required]
    [Url]
    public string Url { get; set; } = string.Empty;

    [Required]
    public string EventTypes { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
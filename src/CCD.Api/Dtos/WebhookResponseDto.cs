using System;

namespace CCD.Api.Dtos
{
    public class WebhookResponseDto
    {
        public Guid Id { get; set; }
        public string Url { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}


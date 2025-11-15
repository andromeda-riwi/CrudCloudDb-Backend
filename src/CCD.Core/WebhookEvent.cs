using System.ComponentModel.DataAnnotations;

namespace CCD.Core
{
    public class WebhookEvent
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid WebhookId { get; set; }

        [Required]
        public string EventType { get; set; } = string.Empty;

        [Required]
        public string Payload { get; set; } = string.Empty;

        public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;

        public bool IsSuccess { get; set; }

        public int StatusCode { get; set; }

        public string Response { get; set; } = string.Empty;

        public Webhook Webhook { get; set; } = null!;
    }
}

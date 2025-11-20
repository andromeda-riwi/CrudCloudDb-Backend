using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace CCD.Core
{
    public class Webhook
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [Url]
        public string Url { get; set; } = string.Empty;

        [Required]
        public string Event { get; set; } = string.Empty; // Propiedad añadida para especificar el tipo de evento

        public string? Secret { get; set; } // Se hizo opcional para alinearse con CreateWebhookDto

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; } // Propiedad añadida para registrar la última actualización

        public User User { get; set; } = null!;

        public ICollection<WebhookEvent> Events { get; set; } = new List<WebhookEvent>();
    }
}
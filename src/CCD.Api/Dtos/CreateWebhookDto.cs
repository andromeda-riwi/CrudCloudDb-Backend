using System.ComponentModel.DataAnnotations;

namespace CCD.Api.Dtos
{
    public class CreateWebhookDto
    {
        [Required]
        [Url]
        public string Url { get; set; }

        [Required]
        public string Secret { get; set; }

        public bool IsActive { get; set; }
    }
}


using CCD.Api.Dtos;
using CCD.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CCD.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/webhooks")]
    public class WebhooksController : ControllerBase
    {
        private readonly IWebhookService _webhookService;

        public WebhooksController(IWebhookService webhookService)
        {
            _webhookService = webhookService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateWebhook([FromBody] CreateWebhookDto createDto)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var webhook = await _webhookService.CreateWebhookAsync(userId, createDto.Url, createDto.EventTypes, createDto.Description, createDto.IsActive);
            return CreatedAtAction(nameof(GetWebhookById), new { id = webhook.Id }, webhook);
        }

        [HttpGet]
        public async Task<IActionResult> GetUserWebhooks()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var webhooks = await _webhookService.GetUserWebhooksAsync(userId);
            return Ok(webhooks);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetWebhookById(Guid id)
        {
            var webhook = await _webhookService.GetWebhookByIdAsync(id);
            if (webhook == null)
            {
                return NotFound();
            }

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (webhook.UserId != userId)
            {
                return Forbid();
            }

            return Ok(webhook);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateWebhook(Guid id, [FromBody] CreateWebhookDto updateDto)
        {
            var webhook = await _webhookService.GetWebhookByIdAsync(id);
            if (webhook == null)
            {
                return NotFound();
            }

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (webhook.UserId != userId)
            {
                return Forbid();
            }

            await _webhookService.UpdateWebhookAsync(id, updateDto.Url, updateDto.EventTypes, updateDto.Description, updateDto.IsActive);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWebhook(Guid id)
        {
            var webhook = await _webhookService.GetWebhookByIdAsync(id);
            if (webhook == null)
            {
                return NotFound();
            }

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (webhook.UserId != userId)
            {
                return Forbid();
            }

            await _webhookService.DeleteWebhookAsync(id);
            return NoContent();
        }
    }
}

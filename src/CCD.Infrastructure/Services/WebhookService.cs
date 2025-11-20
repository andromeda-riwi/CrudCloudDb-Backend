using CCD.Core;
using CCD.Core.Interfaces;
using CCD.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Http;
using System.Text.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace CCD.Infrastructure.Services
{
    public class WebhookService : IWebhookService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WebhookService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public WebhookService(ApplicationDbContext context, ILogger<WebhookService> logger, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<Webhook> CreateWebhookAsync(Guid userId, string url, string eventTypes, string? description, bool isActive)
        {
            var webhook = new Webhook
            {
                UserId = userId,
                Url = url,
                Event = eventTypes, // Mapear a la propiedad Event
                Secret = Guid.NewGuid().ToString("N"), // Generar un secret automáticamente
                IsActive = isActive
            };

            _context.Webhooks.Add(webhook);
            await _context.SaveChangesAsync();
            return webhook;
        }

        public async Task<IEnumerable<Webhook>> GetUserWebhooksAsync(Guid userId)
        {
            return await _context.Webhooks
                .Where(w => w.UserId == userId)
                .ToListAsync();
        }

        public async Task<Webhook?> GetWebhookByIdAsync(Guid webhookId)
        {
            return await _context.Webhooks.FindAsync(webhookId);
        }

        public async Task UpdateWebhookAsync(Guid webhookId, string url, string eventTypes, string? description, bool isActive)
        {
            var webhook = await _context.Webhooks.FindAsync(webhookId);
            if (webhook == null)
            {
                throw new KeyNotFoundException($"Webhook con ID {webhookId} no encontrado.");
            }

            webhook.Url = url;
            webhook.Event = eventTypes; // Mapear a la propiedad Event
            webhook.IsActive = isActive;
            webhook.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteWebhookAsync(Guid webhookId)
        {
            var webhook = await _context.Webhooks.FindAsync(webhookId);
            if (webhook == null)
            {
                throw new KeyNotFoundException($"Webhook con ID {webhookId} no encontrado.");
            }

            _context.Webhooks.Remove(webhook);
            await _context.SaveChangesAsync();
        }

        public async Task TriggerWebhooksAsync<T>(string eventType, T payload)
        {
            var activeWebhooks = await _context.Webhooks.Where(w => w.IsActive).ToListAsync();
            var payloadJson = JsonSerializer.Serialize(payload);

            foreach (var webhook in activeWebhooks)
            {
                await SendWebhookNotification(webhook, eventType, payloadJson);
            }
        }

        private async Task SendWebhookNotification(Webhook webhook, string eventType, string payloadJson)
        {
            var httpClient = _httpClientFactory.CreateClient();
            bool isSuccess = false;
            int statusCode = 0;
            string responseContent = string.Empty;

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, webhook.Url)
                {
                    Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
                };
                // Aquí podrías añadir la lógica para firmar la solicitud con webhook.Secret si es necesario
                // request.Headers.Add("X-Webhook-Signature", GenerateSignature(payloadJson, webhook.Secret));

                var response = await httpClient.SendAsync(request);
                statusCode = (int)response.StatusCode;
                responseContent = await response.Content.ReadAsStringAsync();
                isSuccess = response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar webhook a {Url}", webhook.Url);
                responseContent = ex.Message;
                isSuccess = false;
            }
            finally
            {
                await LogWebhookEventAsync(webhook.Id, eventType, payloadJson, isSuccess, statusCode, responseContent);
            }
        }

        public async Task LogWebhookEventAsync(Guid webhookId, string eventType, string payload, bool isSuccess, int statusCode, string response)
        {
            var webhookEvent = new WebhookEvent
            {
                WebhookId = webhookId,
                EventType = eventType,
                Payload = payload,
                IsSuccess = isSuccess,
                StatusCode = statusCode,
                Response = response
            };

            _context.WebhookEvents.Add(webhookEvent);
            await _context.SaveChangesAsync();
        }
    }
}


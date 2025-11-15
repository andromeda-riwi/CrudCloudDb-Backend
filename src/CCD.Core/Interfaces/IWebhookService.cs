using CCD.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CCD.Core.Interfaces
{
    public interface IWebhookService
    {
        Task<Webhook> CreateWebhookAsync(Guid userId, string url, string eventType, string? secret, bool isActive);
        Task<IEnumerable<Webhook>> GetUserWebhooksAsync(Guid userId);
        Task<Webhook?> GetWebhookByIdAsync(Guid webhookId);
        Task UpdateWebhookAsync(Guid webhookId, string url, string eventType, string? secret, bool isActive);
        Task DeleteWebhookAsync(Guid webhookId);
        Task TriggerWebhooksAsync<T>(string eventType, T payload);
        Task LogWebhookEventAsync(Guid webhookId, string eventType, string payload, bool isSuccess, int statusCode, string response);
    }
}

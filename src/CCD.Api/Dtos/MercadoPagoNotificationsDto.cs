using Newtonsoft.Json;

namespace CCD.Api.Dtos;

public class MercadoPagoNotificationsDto
{
    [JsonProperty("action")]
    public string Action { get; set; }

    [JsonProperty("api_version")]
    public string ApiVersion { get; set; }

    [JsonProperty("data")]
    public NotificationData Data { get; set; }

    [JsonProperty("date_created")]
    public DateTime DateCreated { get; set; }

    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("live_mode")]
    public bool LiveMode { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("user_id")]
    public int UserId { get; set; }
}

public class NotificationData
{
    [JsonProperty("id")]
    public string Id { get; set; }
}
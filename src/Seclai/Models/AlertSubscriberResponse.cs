using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class AlertSubscriberResponse
{
    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("user_name")]
    public string? UserName { get; set; }
}

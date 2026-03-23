using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class UpdateAlertConfigRequest
{
    [JsonPropertyName("threshold")]
    public Dictionary<string, JsonElement>? Threshold { get; set; }

    [JsonPropertyName("cooldown_minutes")]
    public int? CooldownMinutes { get; set; }

    [JsonPropertyName("distribution_type")]
    public string? DistributionType { get; set; }

    [JsonPropertyName("enabled")]
    public bool? Enabled { get; set; }

    [JsonPropertyName("recipient_user_ids")]
    public List<string>? RecipientUserIds { get; set; }
}

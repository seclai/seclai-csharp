using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class AlertConfigResponse
{
    [JsonPropertyName("account_id")]
    public string AccountId { get; set; } = string.Empty;

    [JsonPropertyName("agent_id")]
    public string? AgentId { get; set; }

    [JsonPropertyName("alert_type")]
    public string AlertType { get; set; } = string.Empty;

    [JsonPropertyName("cooldown_minutes")]
    public int CooldownMinutes { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("distribution_type")]
    public string DistributionType { get; set; } = string.Empty;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("last_alerted_at")]
    public string? LastAlertedAt { get; set; }

    [JsonPropertyName("recipient_user_ids")]
    public List<string> RecipientUserIds { get; set; } = new();

    [JsonPropertyName("source_connection_id")]
    public string? SourceConnectionId { get; set; }

    [JsonPropertyName("threshold")]
    public Dictionary<string, JsonElement>? Threshold { get; set; }

    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }
}

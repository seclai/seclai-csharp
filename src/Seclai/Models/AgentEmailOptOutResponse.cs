using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>A recipient's opt-out from an account's agent emails (one agent or all).</summary>
public sealed class AgentEmailOptOutResponse
{
    [JsonPropertyName("agent_id")]
    public string? AgentId { get; set; }

    [JsonPropertyName("agent_name")]
    public string? AgentName { get; set; }

    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("recipient_email")]
    public string RecipientEmail { get; set; } = string.Empty;
}

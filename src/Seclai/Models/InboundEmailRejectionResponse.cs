using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>An inbound email that was discarded without running an agent.</summary>
public sealed class InboundEmailRejectionResponse
{
    [JsonPropertyName("agent_id")]
    public string? AgentId { get; set; }

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("message_id")]
    public string? MessageId { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("recipient")]
    public string Recipient { get; set; } = string.Empty;

    [JsonPropertyName("sender")]
    public string Sender { get; set; } = string.Empty;

    [JsonPropertyName("sender_ip")]
    public string? SenderIp { get; set; }

    [JsonPropertyName("subject")]
    public string? Subject { get; set; }
}

using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>A single blocked inbound email sender.</summary>
public sealed class BlockedEmailSenderResponse
{
    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("match_type")]
    public string MatchType { get; set; } = string.Empty;

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("sender_email")]
    public string SenderEmail { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;
}

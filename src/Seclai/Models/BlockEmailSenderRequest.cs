using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>Add one sender/domain to the account blocklist (shared REST request).</summary>
public sealed class BlockEmailSenderRequest
{
    [JsonPropertyName("match_type")]
    public string? MatchType { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("sender_email")]
    public string SenderEmail { get; set; } = string.Empty;
}

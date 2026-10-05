using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class UpdateMemoryBankRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("compaction_prompt")]
    public string? CompactionPrompt { get; set; }

    /// <summary>Deprecated and no longer applied. Rejected with a 400 from <c>Seclai-Version: 2026-08-03</c>, except 0, which clears a stored value.</summary>
    [JsonPropertyName("max_age_days")]
    public int? MaxAgeDays { get; set; }

    [JsonPropertyName("max_size_tokens")]
    public int? MaxSizeTokens { get; set; }

    [JsonPropertyName("max_turns")]
    public int? MaxTurns { get; set; }

    [JsonPropertyName("retention_days")]
    public int? RetentionDays { get; set; }

    /// <summary>Conversation banks only. When true, a conversation turn written to this bank has the quoted reply chain an email client prepends to a reply dropped from it. Omitted when null, which leaves it unchanged.</summary>
    [JsonPropertyName("strip_quoted_reply_chains")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? StripQuotedReplyChains { get; set; }
}

using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class CreateMemoryBankRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("solution_id")]
    public string? SolutionId { get; set; }

    /// <summary>Conversation banks only. When true, a conversation turn written to this bank has the quoted reply chain an email client prepends to a reply dropped from it. Omitted when null, which leaves it off.</summary>
    [JsonPropertyName("strip_quoted_reply_chains")]
    public bool? StripQuotedReplyChains { get; set; }

    /// <summary>
    /// Days a memory entry is kept before it is deleted outright, text and embeddings. Must be greater than 0.
    /// Omitted when null, which gives the API's default: from <c>Seclai-Version: 2026-08-03</c>, 90 days for a
    /// conversation bank and indefinite for a general bank; on older versions, 30 days for a conversation bank.
    /// To keep entries indefinitely, set <see cref="RetainIndefinitely"/> instead.
    /// </summary>
    [JsonPropertyName("retention_days")]
    public int? RetentionDays { get; set; }

    /// <summary>
    /// When true, the client sends <c>"retention_days": null</c>, which keeps entries indefinitely on every
    /// API version. Cannot be combined with <see cref="RetentionDays"/>:
    /// <see cref="SeclaiClient.CreateMemoryBankAsync"/> throws an <see cref="System.ArgumentException"/> when both are set.
    /// </summary>
    [JsonIgnore]
    public bool RetainIndefinitely { get; set; }
}

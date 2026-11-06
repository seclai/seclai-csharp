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

    /// <summary>Embedding quality / cost trade-off: <c>fast_and_cheap</c> (the server default), <c>balanced</c>, <c>slow_and_thorough</c>, or <c>custom</c>.</summary>
    [JsonPropertyName("mode")]
    public string? Mode { get; set; }

    /// <summary>Embedding model (<c>custom</c> mode only).</summary>
    [JsonPropertyName("embedding_model")]
    public string? EmbeddingModel { get; set; }

    /// <summary>Embedding dimensions (<c>custom</c> mode only).</summary>
    [JsonPropertyName("dimensions")]
    public int? Dimensions { get; set; }

    /// <summary>Chunk size (<c>custom</c> mode only).</summary>
    [JsonPropertyName("chunk_size")]
    public int? ChunkSize { get; set; }

    /// <summary>Chunk overlap (<c>custom</c> mode only).</summary>
    [JsonPropertyName("chunk_overlap")]
    public int? ChunkOverlap { get; set; }

    /// <summary>Custom prompt used when compacting older entries. When set, entries that exceed a threshold are summarized into a new entry before being soft-deleted.</summary>
    [JsonPropertyName("compaction_prompt")]
    public string? CompactionPrompt { get; set; }

    /// <summary>Max total tokens per partition before compaction. Checked inline after each write and by the hourly background sweep.</summary>
    [JsonPropertyName("max_size_tokens")]
    public int? MaxSizeTokens { get; set; }

    /// <summary>Max conversation turns per partition before compaction. Checked inline after each write and by the hourly background sweep.</summary>
    [JsonPropertyName("max_turns")]
    public int? MaxTurns { get; set; }

    /// <summary>Deprecated and no longer applied: age belongs to <see cref="RetentionDays"/>, which deletes. Refused with HTTP 400 from <c>Seclai-Version: 2026-08-03</c>; stored but inert on older versions. Leave it null.</summary>
    [JsonPropertyName("max_age_days")]
    public int? MaxAgeDays { get; set; }
}

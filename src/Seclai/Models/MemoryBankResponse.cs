using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class MemoryBankResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("solution_id")]
    public string? SolutionId { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }

    /// <summary>Conversation banks only. When true, a conversation turn written to this bank has the quoted reply chain an email client prepends to a reply dropped from it.</summary>
    [JsonPropertyName("strip_quoted_reply_chains")]
    public bool StripQuotedReplyChains { get; set; }

    /// <summary>Embedding mode: <c>fast_and_cheap</c>, <c>balanced</c>, <c>slow_and_thorough</c>, or <c>custom</c>.</summary>
    [JsonPropertyName("mode")]
    public string? Mode { get; set; }

    /// <summary>Embedding model identifier.</summary>
    [JsonPropertyName("embedding_model")]
    public string? EmbeddingModel { get; set; }

    /// <summary>Vector embedding dimensions.</summary>
    [JsonPropertyName("dimensions")]
    public int? Dimensions { get; set; }

    /// <summary>Characters per chunk.</summary>
    [JsonPropertyName("chunk_size")]
    public int? ChunkSize { get; set; }

    /// <summary>Character overlap between chunks.</summary>
    [JsonPropertyName("chunk_overlap")]
    public int? ChunkOverlap { get; set; }

    /// <summary>Custom prompt used when compacting older entries. When set, entries that exceed a threshold are summarized into a new entry before being soft-deleted.</summary>
    [JsonPropertyName("compaction_prompt")]
    public string? CompactionPrompt { get; set; }

    /// <summary>Max total tokens per partition before compaction.</summary>
    [JsonPropertyName("max_size_tokens")]
    public int? MaxSizeTokens { get; set; }

    /// <summary>Max conversation turns per partition before compaction.</summary>
    [JsonPropertyName("max_turns")]
    public int? MaxTurns { get; set; }

    /// <summary>Deprecated and no longer applied: age belongs to <see cref="RetentionDays"/>. Always null from <c>Seclai-Version: 2026-08-03</c>; older versions read whatever was stored.</summary>
    [JsonPropertyName("max_age_days")]
    public int? MaxAgeDays { get; set; }

    /// <summary>Days entries are kept before deletion; null keeps them indefinitely.</summary>
    [JsonPropertyName("retention_days")]
    public int? RetentionDays { get; set; }

    /// <summary>The linked content source; null until provisioned.</summary>
    [JsonPropertyName("source_connection_id")]
    public string? SourceConnectionId { get; set; }
}

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>
/// How often each item-metadata key appears on a knowledge base's content items, per time window.
/// Returned by <see cref="SeclaiClient.GetKnowledgeBaseMetadataKeyStatsAsync"/>.
/// </summary>
public sealed class KnowledgeBaseMetadataKeyStatsResponse
{
    [JsonPropertyName("knowledge_base_id")]
    public string KnowledgeBaseId { get; set; } = string.Empty;

    /// <summary>One entry per window: all time, the last 30 days and the last 7 days.</summary>
    [JsonPropertyName("windows")]
    public List<MetadataKeyWindowResponse> Windows { get; set; } = new();
}

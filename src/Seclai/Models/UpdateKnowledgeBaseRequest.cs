using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class UpdateKnowledgeBaseRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("default_score_threshold")]
    public float? DefaultScoreThreshold { get; set; }

    [JsonPropertyName("default_top_k")]
    public int? DefaultTopK { get; set; }

    [JsonPropertyName("default_top_n")]
    public int? DefaultTopN { get; set; }

    [JsonPropertyName("reranker_model")]
    public string? RerankerModel { get; set; }

    [JsonPropertyName("source_ids")]
    public List<string>? SourceIds { get; set; }
}

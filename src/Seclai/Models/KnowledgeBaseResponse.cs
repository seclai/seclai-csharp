using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class KnowledgeBaseResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("solution_id")]
    public string? SolutionId { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }

    /// <summary>Linked source connections.</summary>
    [JsonPropertyName("sources")]
    public List<SourceConnectionResponse>? Sources { get; set; }

    /// <summary>Reranker model in use.</summary>
    [JsonPropertyName("reranker_model")]
    public string? RerankerModel { get; set; }

    /// <summary>Editor default for a new retrieval step's Top K.</summary>
    [JsonPropertyName("default_top_k")]
    public int? DefaultTopK { get; set; }

    /// <summary>Editor default for a new retrieval step's Top N.</summary>
    [JsonPropertyName("default_top_n")]
    public int? DefaultTopN { get; set; }

    /// <summary>Editor default for a new retrieval step's Minimum Rerank Score.</summary>
    [JsonPropertyName("default_score_threshold")]
    public double? DefaultScoreThreshold { get; set; }

    /// <summary>Whether the knowledge base is read-only.</summary>
    [JsonPropertyName("readonly")]
    public bool Readonly { get; set; }
}

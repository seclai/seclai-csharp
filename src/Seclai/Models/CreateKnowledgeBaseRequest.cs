using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class CreateKnowledgeBaseRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("solution_id")]
    public string? SolutionId { get; set; }

    /// <summary>IDs of the source connections to link; the API requires at least one.</summary>
    [JsonPropertyName("source_ids")]
    public List<string>? SourceIds { get; set; }

    /// <summary>Reranker model to use: a <c>model_type</c> from <see cref="SeclaiClient.ListRerankerModelsAsync"/>, or <c>none</c> to disable reranking (<c>""</c> is a synonym). Omitted when null, which picks a default from the sources (<c>none</c> when every source embeds media natively).</summary>
    [JsonPropertyName("reranker_model")]
    public string? RerankerModel { get; set; }

    /// <summary>Prefilled into Top K on a new retrieval step in the editor (1 to 200). Not applied at retrieval time.</summary>
    [JsonPropertyName("default_top_k")]
    public int? DefaultTopK { get; set; }

    /// <summary>Prefilled into Top N on a new retrieval step in the editor (1 to 200). Not applied at retrieval time: the step's own value is used.</summary>
    [JsonPropertyName("default_top_n")]
    public int? DefaultTopN { get; set; }

    /// <summary>Prefilled into Minimum Rerank Score on a new retrieval step in the editor (0.0 to 1.0). Not applied at retrieval time: the step's own value is used.</summary>
    [JsonPropertyName("default_score_threshold")]
    public double? DefaultScoreThreshold { get; set; }
}

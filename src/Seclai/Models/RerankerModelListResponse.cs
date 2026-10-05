using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>The reranker model catalog.</summary>
/// <remarks>
/// The list key is version-gated. By default the models arrive under
/// <c>models</c>; once <see cref="SeclaiClientOptions.ApiVersion"/> is
/// <c>2026-07-27</c> or later they arrive under <c>data</c> with
/// <c>pagination</c>. The default and pricing sit beside the list on both shapes. Use
/// <see cref="Items"/> to read whichever arrived.
/// </remarks>
public sealed class RerankerModelListResponse : IListPage<RerankerModelResponse>
{
    /// <summary>Reranker used when a knowledge base does not choose one</summary>
    [JsonPropertyName("default_model_type")]
    public string DefaultModelType { get; set; } = string.Empty;

    /// <summary>Legacy key. Empty once the canonical envelope is in use.</summary>
    [JsonPropertyName("models")]
    public List<RerankerModelResponse> Models { get; set; } = new();

    /// <summary>Canonical key. Empty on the legacy shape.</summary>
    [JsonPropertyName("data")]
    public List<RerankerModelResponse> Data { get; set; } = new();

    /// <summary>Paging metadata, present once <c>Seclai-Version</c> is <c>2026-07-27</c> or later. Null on the default shape.</summary>
    [JsonPropertyName("pagination")]
    public PaginationResponse? Pagination { get; set; }

    /// <summary>The models, from whichever key the response used.</summary>
    [JsonIgnore]
    public List<RerankerModelResponse> Items => Data.Count > 0 ? Data : Models;

    /// <summary>Credits charged for processing a search request</summary>
    [JsonPropertyName("search_processing_credits")]
    public double SearchProcessingCredits { get; set; }

    void IListPage<RerankerModelResponse>.Fill(List<RerankerModelResponse> items, ListShape shape)
    {
        if (shape.FromData)
        {
            Data = items;
        }
        else
        {
            Models = items;
        }
        Pagination = shape.Pagination;
    }
}

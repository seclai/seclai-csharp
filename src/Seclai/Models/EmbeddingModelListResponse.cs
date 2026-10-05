using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>The embedding model catalog.</summary>
/// <remarks>
/// The list key is version-gated. By default the models arrive under
/// <c>models</c>; once <see cref="SeclaiClientOptions.ApiVersion"/> is
/// <c>2026-07-27</c> or later they arrive under <c>data</c> with
/// <c>pagination</c>. The defaults and pricing sit beside the list on both shapes. Use
/// <see cref="Items"/> to read whichever arrived.
/// </remarks>
public sealed class EmbeddingModelListResponse
{
    /// <summary>Dimensions used with the default embedding model</summary>
    [JsonPropertyName("default_dimension")]
    public int? DefaultDimension { get; set; }

    /// <summary>Embedding model used when a source does not override it</summary>
    [JsonPropertyName("default_model_type")]
    public string? DefaultModelType { get; set; }

    /// <summary>Credits per MB for file processing at ingest</summary>
    [JsonPropertyName("file_processing_credits_per_mb")]
    public double FileProcessingCreditsPerMb { get; set; }

    /// <summary>Legacy key. Empty once the canonical envelope is in use.</summary>
    [JsonPropertyName("models")]
    public List<EmbeddingModelResponse> Models { get; set; } = new();

    /// <summary>Canonical key. Empty on the legacy shape.</summary>
    [JsonPropertyName("data")]
    public List<EmbeddingModelResponse> Data { get; set; } = new();

    /// <summary>Canonical pagination metadata. Null on the legacy shape.</summary>
    [JsonPropertyName("pagination")]
    public PaginationResponse? Pagination { get; set; }

    /// <summary>The models, from whichever key the response used.</summary>
    [JsonIgnore]
    public List<EmbeddingModelResponse> Items => Data.Count > 0 ? Data : Models;

    /// <summary>Monthly storage credits per dimension count</summary>
    [JsonPropertyName("storage_credits")]
    public List<EmbeddingStorageCreditsResponse> StorageCredits { get; set; } = new();
}

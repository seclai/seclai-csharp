using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>Information about an embedding model.</summary>
public sealed class EmbeddingModelResponse
{
    /// <summary>Estimated credits per 1,000 English words</summary>
    [JsonPropertyName("credits")]
    public double Credits { get; set; }

    /// <summary>Model description</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Dimensions options</summary>
    [JsonPropertyName("dimensions")]
    public List<int> Dimensions { get; set; } = new();

    /// <summary>Whether the model is newly released</summary>
    [JsonPropertyName("is_new")]
    public bool IsNew { get; set; }

    /// <summary>Max input tokens per request</summary>
    [JsonPropertyName("max_input_tokens")]
    public int? MaxInputTokens { get; set; }

    /// <summary>Model identifier</summary>
    [JsonPropertyName("model_id")]
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Full model type identifier (enum value). This is the value to send as embedding_model
    /// when creating a source.
    /// </summary>
    [JsonPropertyName("model_type")]
    public string ModelType { get; set; } = string.Empty;

    /// <summary>MTEB retrieval score</summary>
    [JsonPropertyName("mteb_retrieval_score")]
    public double? MtebRetrievalScore { get; set; }

    /// <summary>Human-readable model name</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Non-text rates the vendor charges for this embedder (image, video, audio). Empty for
    /// text-only embedders.
    /// </summary>
    [JsonPropertyName("per_modality_rates")]
    public List<EmbeddingModalityRateResponse>? PerModalityRates { get; set; }

    /// <summary>Model provider identifier</summary>
    [JsonPropertyName("provider")]
    public string? Provider { get; set; }

    /// <summary>Model processing speed</summary>
    [JsonPropertyName("speed")]
    public string? Speed { get; set; }

    /// <summary>
    /// Modalities the embedder accepts on input (short kinds like text / image / video, or full
    /// MIMEs). null means text-only. A source only honours a media_types entry its embedder
    /// lists here.
    /// </summary>
    [JsonPropertyName("supported_input_media")]
    public List<string>? SupportedInputMedia { get; set; }

    /// <summary>Supported languages</summary>
    [JsonPropertyName("supported_languages")]
    public List<string>? SupportedLanguages { get; set; }

    /// <summary>Model documentation URL</summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

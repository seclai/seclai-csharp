using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>Information about a reranker model.</summary>
public sealed class RerankerModelResponse
{
    /// <summary>Credits charged per rerank action</summary>
    [JsonPropertyName("credits_per_action")]
    public double CreditsPerAction { get; set; }

    /// <summary>Model description</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Whether this is the platform default reranker</summary>
    [JsonPropertyName("is_default")]
    public bool IsDefault { get; set; }

    /// <summary>Whether the model is newly released</summary>
    [JsonPropertyName("is_new")]
    public bool IsNew { get; set; }

    /// <summary>Max input tokens per request</summary>
    [JsonPropertyName("max_input_tokens")]
    public int? MaxInputTokens { get; set; }

    /// <summary>
    /// Full model type identifier. This is the value to send as reranker_model on a knowledge
    /// base; send "none" or an empty string to disable reranking.
    /// </summary>
    [JsonPropertyName("model_type")]
    public string ModelType { get; set; } = string.Empty;

    /// <summary>Human-readable model name</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Model provider identifier</summary>
    [JsonPropertyName("provider")]
    public string? Provider { get; set; }

    /// <summary>Supported languages</summary>
    [JsonPropertyName("supported_languages")]
    public List<string>? SupportedLanguages { get; set; }

    /// <summary>Model documentation URL</summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

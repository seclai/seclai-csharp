using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class ModelRecommendationResponse
{
    [JsonPropertyName("deprecated_at")]
    public string? DeprecatedAt { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("family")]
    public string? Family { get; set; }

    [JsonPropertyName("family_generation")]
    public double? FamilyGeneration { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("max_context_tokens")]
    public int MaxContextTokens { get; set; }

    [JsonPropertyName("max_output_tokens")]
    public int MaxOutputTokens { get; set; }

    [JsonPropertyName("model_id")]
    public string ModelId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("provider")]
    public string Provider { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("recommendation_type")]
    public string RecommendationType { get; set; } = string.Empty;

    [JsonPropertyName("released_at")]
    public string? ReleasedAt { get; set; }

    [JsonPropertyName("sunset_at")]
    public string? SunsetAt { get; set; }

    [JsonPropertyName("supports_openai_arguments")]
    public bool SupportsOpenaiArguments { get; set; }

    [JsonPropertyName("supports_streaming")]
    public bool SupportsStreaming { get; set; }

    [JsonPropertyName("supports_structured_output")]
    public bool SupportsStructuredOutput { get; set; }

    [JsonPropertyName("supports_thinking")]
    public bool SupportsThinking { get; set; }

    [JsonPropertyName("supports_tool_use")]
    public bool SupportsToolUse { get; set; }
}

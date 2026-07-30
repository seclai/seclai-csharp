using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>Response model for a variant option</summary>
public sealed class VariantOptionResponse
{
    [JsonPropertyName("default")]
    public bool Default { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("input_1h_cache_write_credits_per_1000_tokens")]
    public double? Input1hCacheWriteCreditsPer1000Tokens { get; set; }

    [JsonPropertyName("input_5m_cache_write_credits_per_1000_tokens")]
    public double? Input5mCacheWriteCreditsPer1000Tokens { get; set; }

    [JsonPropertyName("input_cache_hit_credits_per_1000_tokens")]
    public double? InputCacheHitCreditsPer1000Tokens { get; set; }

    [JsonPropertyName("input_credits_per_1000_tokens")]
    public double? InputCreditsPer1000Tokens { get; set; }

    [JsonPropertyName("long_context_input_cache_hit_credits_per_1000_tokens")]
    public double? LongContextInputCacheHitCreditsPer1000Tokens { get; set; }

    [JsonPropertyName("long_context_input_credits_per_1000_tokens")]
    public double? LongContextInputCreditsPer1000Tokens { get; set; }

    [JsonPropertyName("long_context_output_credits_per_1000_tokens")]
    public double? LongContextOutputCreditsPer1000Tokens { get; set; }

    [JsonPropertyName("long_context_threshold")]
    public int? LongContextThreshold { get; set; }

    [JsonPropertyName("output_credits_per_1000_tokens")]
    public double? OutputCreditsPer1000Tokens { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

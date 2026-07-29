using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>Response model for prompt model data</summary>
public sealed class PromptModelResponse
{
    [JsonPropertyName("default")]
    public bool Default { get; set; }

    [JsonPropertyName("deprecated_at")]
    public string? DeprecatedAt { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("family")]
    public string? Family { get; set; }

    [JsonPropertyName("family_generation")]
    public double? FamilyGeneration { get; set; }

    /// <summary>
    /// Per-unit credit cost for a dedicated media-generation model, in the unit named by
    /// <c>generation_params.pricing_unit</c> (per image / per second / per character / per
    /// output token). Multiply by the produced unit count (images, seconds, characters) for the
    /// run cost. None for token-billed (non-generation) models.
    /// </summary>
    [JsonPropertyName("generation_credits_per_unit")]
    public double? GenerationCreditsPerUnit { get; set; }

    /// <summary>
    /// Media-generation descriptor (modality, pricing_unit, and modality-specific constraints).
    /// NULL for text LLMs; present for image/audio/video generation models. See
    /// schemas.generation_params.
    /// </summary>
    [JsonPropertyName("generation_params")]
    public Dictionary<string, JsonElement>? GenerationParams { get; set; }

    /// <summary>
    /// Human suffix for the per-unit generation rate (e.g. <c>/image</c>, <c>/second</c>,
    /// <c>/1k chars</c>, <c>/1k tokens</c>) — single-sourced from the pricing unit so clients
    /// render cost without re-deriving the mapping. None for non-generation models. Char/token
    /// rates are shown per 1,000 (the <c>/1k …</c> suffix), so scale
    /// <c>generation_credits_per_unit</c> accordingly for those units.
    /// </summary>
    [JsonPropertyName("generation_unit_label")]
    public string? GenerationUnitLabel { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Per-image credit cost of using the built-in image_generation tool (it runs gpt-image-1).
    /// Set only for models that actually support the tool (tool-use capable); None otherwise.
    /// </summary>
    [JsonPropertyName("image_generation_tool_credits_per_image")]
    public double? ImageGenerationToolCreditsPerImage { get; set; }

    [JsonPropertyName("input_1h_cache_write_credits_per_1000_tokens")]
    public double? Input1hCacheWriteCreditsPer1000Tokens { get; set; }

    [JsonPropertyName("input_5m_cache_write_credits_per_1000_tokens")]
    public double? Input5mCacheWriteCreditsPer1000Tokens { get; set; }

    [JsonPropertyName("input_cache_hit_credits_per_1000_tokens")]
    public double? InputCacheHitCreditsPer1000Tokens { get; set; }

    [JsonPropertyName("input_credits_per_1000_tokens")]
    public double? InputCreditsPer1000Tokens { get; set; }

    [JsonPropertyName("is_new")]
    public bool? IsNew { get; set; }

    [JsonPropertyName("last_used")]
    public bool? LastUsed { get; set; }

    [JsonPropertyName("max_context_tokens")]
    public int MaxContextTokens { get; set; }

    [JsonPropertyName("max_conversation_length")]
    public int MaxConversationLength { get; set; }

    [JsonPropertyName("max_output_tokens")]
    public int MaxOutputTokens { get; set; }

    [JsonPropertyName("model_id")]
    public string ModelId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("output_credits_per_1000_tokens")]
    public double? OutputCreditsPer1000Tokens { get; set; }

    /// <summary>Model-specific JSON schema for advanced prompt_call json_template payloads.</summary>
    [JsonPropertyName("payload_schema")]
    public Dictionary<string, JsonElement>? PayloadSchema { get; set; }

    /// <summary>Source URL used to derive payload_schema guidance for this model.</summary>
    [JsonPropertyName("payload_schema_source_url")]
    public string? PayloadSchemaSourceUrl { get; set; }

    [JsonPropertyName("per_modality_rates")]
    public List<ModalityRateResponse>? PerModalityRates { get; set; }

    [JsonPropertyName("provider")]
    public string Provider { get; set; } = string.Empty;

    [JsonPropertyName("released_at")]
    public string? ReleasedAt { get; set; }

    /// <summary>Model documentation URL with request/response payload details.</summary>
    [JsonPropertyName("schema_documentation_url")]
    public string? SchemaDocumentationUrl { get; set; }

    /// <summary>Human-readable notes about request payload compatibility.</summary>
    [JsonPropertyName("schema_notes")]
    public string? SchemaNotes { get; set; }

    [JsonPropertyName("successor_model_id")]
    public string? SuccessorModelId { get; set; }

    [JsonPropertyName("sunset_at")]
    public string? SunsetAt { get; set; }

    [JsonPropertyName("supported_input_media")]
    public List<string>? SupportedInputMedia { get; set; }

    [JsonPropertyName("supported_languages")]
    public List<string>? SupportedLanguages { get; set; }

    [JsonPropertyName("supported_output_media")]
    public List<string>? SupportedOutputMedia { get; set; }

    [JsonPropertyName("supports_openai_arguments")]
    public bool? SupportsOpenaiArguments { get; set; }

    [JsonPropertyName("supports_streaming")]
    public bool? SupportsStreaming { get; set; }

    [JsonPropertyName("supports_structured_output")]
    public bool? SupportsStructuredOutput { get; set; }

    [JsonPropertyName("supports_thinking")]
    public bool? SupportsThinking { get; set; }

    [JsonPropertyName("supports_tool_use")]
    public bool? SupportsToolUse { get; set; }

    [JsonPropertyName("tools_disabled")]
    public List<PromptToolResponse>? ToolsDisabled { get; set; }

    [JsonPropertyName("tools_enabled")]
    public List<PromptToolResponse>? ToolsEnabled { get; set; }

    [JsonPropertyName("training_cutoff_at")]
    public string? TrainingCutoffAt { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("variants")]
    public List<VariantCategoryResponse>? Variants { get; set; }
}

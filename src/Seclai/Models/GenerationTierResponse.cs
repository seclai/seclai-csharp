using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class GenerationTierResponse
{
    /// <summary>
    /// The aspect ratios the tier's model can be asked for, and so the values a <c>media_generation</c> image
    /// generator's <c>aspect_ratio</c> pin may take. Empty for audio and video tiers and for an image model that
    /// takes no ratio. Null when the server does not send it.
    /// </summary>
    [JsonPropertyName("aspect_ratios")]
    public List<string>? AspectRatios { get; set; }

    [JsonPropertyName("credits_per_unit")]
    public int? CreditsPerUnit { get; set; }

    [JsonPropertyName("modality")]
    public string Modality { get; set; } = string.Empty;

    [JsonPropertyName("model_id")]
    public string ModelId { get; set; } = string.Empty;

    [JsonPropertyName("model_name")]
    public string ModelName { get; set; } = string.Empty;

    [JsonPropertyName("price_label")]
    public string? PriceLabel { get; set; }

    [JsonPropertyName("tier")]
    public string Tier { get; set; } = string.Empty;

    [JsonPropertyName("unit_label")]
    public string? UnitLabel { get; set; }
}

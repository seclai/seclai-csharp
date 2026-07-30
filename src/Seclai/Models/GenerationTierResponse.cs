using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class GenerationTierResponse
{
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

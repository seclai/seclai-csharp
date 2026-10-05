using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>A non-text rate a multi-modal embedder charges, such as per image record or per second of video.</summary>
public sealed class EmbeddingModalityRateResponse
{
    /// <summary>Rate value in the unit below</summary>
    [JsonPropertyName("credits")]
    public double Credits { get; set; }

    /// <summary>Modality kind, e.g. image / video</summary>
    [JsonPropertyName("modality")]
    public string Modality { get; set; } = string.Empty;

    /// <summary>Billing unit for this rate (credit_per_record / credit_per_second / credit_per_1000_tokens).</summary>
    [JsonPropertyName("unit")]
    public string Unit { get; set; } = string.Empty;
}

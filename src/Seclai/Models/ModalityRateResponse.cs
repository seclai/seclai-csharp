using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>Per-modality rate for an LLM that prices image/audio/video output (or input) at a rate distinct from the default text rate. Example: Gemini 3.1 Flash Image charges $3/1M output tokens for text but $60/1M output tokens for generated images. The image rate surfaces here with ``modality="image"`` and ``output_credits_per_1000_tokens`` set; the default text rate stays on the parent model fields.</summary>
public sealed class ModalityRateResponse
{
    [JsonPropertyName("input_credits_per_1000_tokens")]
    public double? InputCreditsPer1000Tokens { get; set; }

    [JsonPropertyName("modality")]
    public string Modality { get; set; } = string.Empty;

    [JsonPropertyName("output_credits_per_1000_tokens")]
    public double? OutputCreditsPer1000Tokens { get; set; }
}

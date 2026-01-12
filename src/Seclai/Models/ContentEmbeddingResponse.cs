using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class ContentEmbeddingResponse
{
    [JsonPropertyName("batch_duration")]
    public float BatchDuration { get; set; }

    [JsonPropertyName("batch_size")]
    public int BatchSize { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("text_end")]
    public int TextEnd { get; set; }

    [JsonPropertyName("text_start")]
    public int TextStart { get; set; }

    [JsonPropertyName("vector")]
    public List<float> Vector { get; set; } = new();
}

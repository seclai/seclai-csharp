using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class CreateSourceRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("source_type")]
    public string? SourceType { get; set; }

    [JsonPropertyName("embedding_model")]
    public string? EmbeddingModel { get; set; }

    [JsonPropertyName("dimensions")]
    public int? Dimensions { get; set; }

    [JsonPropertyName("chunk_size")]
    public int? ChunkSize { get; set; }

    [JsonPropertyName("chunk_overlap")]
    public int? ChunkOverlap { get; set; }

    [JsonPropertyName("content_filter")]
    public string? ContentFilter { get; set; }

    [JsonPropertyName("polling")]
    public string? Polling { get; set; }

    [JsonPropertyName("polling_action")]
    public string? PollingAction { get; set; }

    [JsonPropertyName("polling_max_items")]
    public int? PollingMaxItems { get; set; }

    [JsonPropertyName("retention")]
    public int? Retention { get; set; }

    [JsonPropertyName("url_id")]
    public string? UrlId { get; set; }
}

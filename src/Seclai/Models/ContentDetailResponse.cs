using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class ContentDetailResponse
{
    [JsonPropertyName("content_duration")]
    public int? ContentDuration { get; set; }

    [JsonPropertyName("content_duration_display")]
    public string? ContentDurationDisplay { get; set; }

    [JsonPropertyName("content_status")]
    public string? ContentStatus { get; set; }

    [JsonPropertyName("content_type")]
    public string? ContentType { get; set; }

    [JsonPropertyName("content_type_display")]
    public string? ContentTypeDisplay { get; set; }

    [JsonPropertyName("content_url")]
    public string? ContentUrl { get; set; }

    [JsonPropertyName("content_word_count")]
    public int? ContentWordCount { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("metadata")]
    public List<Dictionary<string, string>>? Metadata { get; set; }

    [JsonPropertyName("published_at")]
    public string? PublishedAt { get; set; }

    [JsonPropertyName("pulled_at")]
    public string? PulledAt { get; set; }

    [JsonPropertyName("source_connection_content_version_id")]
    public string? SourceConnectionContentVersionId { get; set; }

    [JsonPropertyName("source_connection_id")]
    public string? SourceConnectionId { get; set; }

    [JsonPropertyName("source_name")]
    public string? SourceName { get; set; }

    [JsonPropertyName("source_type")]
    public string? SourceType { get; set; }

    [JsonPropertyName("text_content")]
    public string? TextContent { get; set; }

    [JsonPropertyName("text_content_end")]
    public int TextContentEnd { get; set; }

    [JsonPropertyName("text_content_start")]
    public int TextContentStart { get; set; }

    [JsonPropertyName("text_content_total_length")]
    public int TextContentTotalLength { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }
}

using System;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class SourceResponse
{
    [JsonPropertyName("account_id")]
    public Guid AccountId { get; set; }

    [JsonPropertyName("avg_episodes_per_month")]
    public float? AvgEpisodesPerMonth { get; set; }

    [JsonPropertyName("avg_words_per_episode")]
    public int? AvgWordsPerEpisode { get; set; }

    [JsonPropertyName("chunk_language")]
    public string? ChunkLanguage { get; set; }

    [JsonPropertyName("chunk_overlap")]
    public int? ChunkOverlap { get; set; }

    [JsonPropertyName("chunk_regex_separators")]
    public bool? ChunkRegexSeparators { get; set; }

    [JsonPropertyName("chunk_separators")]
    public string? ChunkSeparators { get; set; }

    [JsonPropertyName("chunk_size")]
    public int? ChunkSize { get; set; }

    [JsonPropertyName("content_count")]
    public int? ContentCount { get; set; }

    [JsonPropertyName("content_filter")]
    public string? ContentFilter { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("dimensions")]
    public int? Dimensions { get; set; }

    [JsonPropertyName("embedding_model")]
    public string? EmbeddingModel { get; set; }

    [JsonPropertyName("embedding_model_type")]
    public string? EmbeddingModelType { get; set; }

    [JsonPropertyName("has_historical_data")]
    public bool? HasHistoricalData { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("next_poll_at")]
    public string? NextPollAt { get; set; }

    [JsonPropertyName("polling")]
    public string? Polling { get; set; }

    [JsonPropertyName("polling_action")]
    public string? PollingAction { get; set; }

    [JsonPropertyName("polling_max_items")]
    public int? PollingMaxItems { get; set; }

    [JsonPropertyName("pulled_at")]
    public string? PulledAt { get; set; }

    [JsonPropertyName("readonly")]
    public bool? Readonly { get; set; }

    [JsonPropertyName("retention")]
    public int? Retention { get; set; }

    [JsonPropertyName("source_type")]
    public string? SourceType { get; set; }

    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

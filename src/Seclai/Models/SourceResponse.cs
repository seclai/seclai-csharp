using System.Collections.Generic;
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

    /// <summary>Media kinds extracted from indexed content and embedded as multi-modal KB chunks (subset of ['images', 'video']). Empty = text-only.</summary>
    [JsonPropertyName("media_types")]
    public List<string>? MediaTypes { get; set; }

    /// <summary>For a cloud_drive source: the id of the connected drive it indexes. Null for every other source type.</summary>
    [JsonPropertyName("cloud_drive_connection_id")]
    public string? CloudDriveConnectionId { get; set; }

    /// <summary>For a cloud_drive source: the comma-separated MIME types a file must have to be indexed. Null when there is no filter.</summary>
    [JsonPropertyName("cloud_drive_content_types")]
    public string? CloudDriveContentTypes { get; set; }

    /// <summary>For a cloud_drive source: the folder it indexes. Null when it indexes the connected drive's own folder.</summary>
    [JsonPropertyName("cloud_drive_folder_path")]
    public string? CloudDriveFolderPath { get; set; }

    /// <summary>For a cloud_drive source: the size in megabytes above which a file is skipped. Null when the source sets no limit of its own.</summary>
    [JsonPropertyName("cloud_drive_max_file_mb")]
    public int? CloudDriveMaxFileMb { get; set; }

    /// <summary>For a cloud_drive source: the glob a file's path must match to be indexed. Null when there is none.</summary>
    [JsonPropertyName("cloud_drive_path_pattern")]
    public string? CloudDrivePathPattern { get; set; }

    /// <summary>Whether the images inside PDF, DOCX, PPTX and XLSX files are attached to the item and indexed. On a website or RSS source it reports whether <see cref="MediaTypes"/> holds <c>images</c>.</summary>
    [JsonPropertyName("document_images")]
    public bool? DocumentImages { get; set; }

    /// <summary>For an email source: the address to send mail to. Null for every other source type.</summary>
    [JsonPropertyName("email_address")]
    public string? EmailAddress { get; set; }

    /// <summary>For an email source: the addresses and domains whose mail is stored. Null for every other source type.</summary>
    [JsonPropertyName("email_allowed_senders")]
    public List<string>? EmailAllowedSenders { get; set; }

    /// <summary>Kinds of file listed beside a feed item's content that an RSS source attaches to the item and indexes (subset of <c>enclosures</c>, <c>item_image</c>, <c>media_rss</c>).</summary>
    [JsonPropertyName("feed_attachments")]
    public List<string>? FeedAttachments { get; set; }
}

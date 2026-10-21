using System.Collections.Generic;
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

    /// <summary>Media kinds to extract from indexed content and embed as multi-modal KB chunks. Subset of ['images', 'video']. Only kinds the source's embedder can index are honored; unsupported values are dropped. Omit / [] for text-only.</summary>
    [JsonPropertyName("media_types")]
    public List<string>? MediaTypes { get; set; }

    /// <summary>Required for a cloud_drive source, and ignored on any other: the id of a connected drive, from <see cref="SeclaiClient.ListCloudDrivesAsync"/>.</summary>
    [JsonPropertyName("cloud_drive_connection_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CloudDriveConnectionId { get; set; }

    /// <summary>For a cloud_drive source: comma-separated MIME types a file must have to be indexed. Omit for no filter.</summary>
    [JsonPropertyName("cloud_drive_content_types")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CloudDriveContentTypes { get; set; }

    /// <summary>For a cloud_drive source: the folder to index, as a path inside the drive. Omit to index the connected drive's own folder.</summary>
    [JsonPropertyName("cloud_drive_folder_path")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CloudDriveFolderPath { get; set; }

    /// <summary>For a cloud_drive source: files larger than this many megabytes are skipped. Omit for no limit of the source's own.</summary>
    [JsonPropertyName("cloud_drive_max_file_mb")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? CloudDriveMaxFileMb { get; set; }

    /// <summary>For a cloud_drive source: a glob a file's path must match to be indexed, such as <c>**/*.pdf</c> or <c>/reports/*</c>. Omit for no filter.</summary>
    [JsonPropertyName("cloud_drive_path_pattern")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CloudDrivePathPattern { get; set; }

    /// <summary>Whether the images inside PDF, DOCX, PPTX and XLSX files are attached to the item and indexed; on a text-only embedder, by the text in each image. Applies to documents added after it changes. Ignored on a website or RSS source, where <see cref="MediaTypes"/> decides. Defaults to true; below API version <c>2026-10-18</c> it defaults to false on a text-only embedder.</summary>
    [JsonPropertyName("document_images")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? DocumentImages { get; set; }

    /// <summary>Required for an email source, and only valid there: the senders whose mail is stored. Each entry is a full address (<c>news@example.com</c>) or a bare domain (<c>example.com</c>), which also matches its sub-domains. Matched against the From address, which must pass DMARC.</summary>
    [JsonPropertyName("email_allowed_senders")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? EmailAllowedSenders { get; set; }

    /// <summary>Kinds of file listed beside a feed item's content to attach to the item and index, on an RSS source. Subset of <c>enclosures</c>, <c>item_image</c>, <c>media_rss</c>; any other value is refused. None are attached unless chosen.</summary>
    [JsonPropertyName("feed_attachments")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? FeedAttachments { get; set; }
}

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class UpdateSourceRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("polling")]
    public string? Polling { get; set; }

    [JsonPropertyName("retention_days")]
    public int? RetentionDays { get; set; }

    /// <summary>Media kinds to extract from indexed content and embed as multi-modal KB chunks. Subset of ['images', 'video']. Only kinds the source's embedder can index are honored; unsupported values are dropped. [] disables media extraction (text-only).</summary>
    [JsonPropertyName("media_types")]
    public List<string>? MediaTypes { get; set; }

    /// <summary>For a cloud_drive source: comma-separated MIME types a file must have to be indexed. Send an empty string to clear it; leave null to keep it.</summary>
    [JsonPropertyName("cloud_drive_content_types")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CloudDriveContentTypes { get; set; }

    /// <summary>For a cloud_drive source: the folder to index, as a path inside the drive. Send an empty string to index the connected drive's own folder; leave null to keep it.</summary>
    [JsonPropertyName("cloud_drive_folder_path")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CloudDriveFolderPath { get; set; }

    /// <summary>For a cloud_drive source: files larger than this many megabytes are skipped. Send 0 to clear the source's own limit; leave null to keep it.</summary>
    [JsonPropertyName("cloud_drive_max_file_mb")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? CloudDriveMaxFileMb { get; set; }

    /// <summary>For a cloud_drive source: a glob a file's path must match to be indexed, such as <c>**/*.pdf</c> or <c>/reports/*</c>. Send an empty string to clear it; leave null to keep it.</summary>
    [JsonPropertyName("cloud_drive_path_pattern")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CloudDrivePathPattern { get; set; }

    /// <summary>Whether the images inside PDF, DOCX, PPTX and XLSX files are attached to the item and indexed; on a text-only embedder, by the text in each image. Applies to documents added after it changes. Ignored on a website or RSS source, where <see cref="MediaTypes"/> decides. Leave null to keep it.</summary>
    [JsonPropertyName("document_images")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? DocumentImages { get; set; }

    /// <summary>For an email source: replaces the allowed senders. Each entry is a full address or a bare domain. The list cannot be emptied. Leave null to keep it.</summary>
    [JsonPropertyName("email_allowed_senders")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? EmailAllowedSenders { get; set; }

    /// <summary>Kinds of file listed beside a feed item's content to attach to the item and index, on an RSS source. Subset of <c>enclosures</c>, <c>item_image</c>, <c>media_rss</c>; any other value is refused. Leave null to keep them.</summary>
    [JsonPropertyName("feed_attachments")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? FeedAttachments { get; set; }
}

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>One attached file, with the text indexed from it.</summary>
public sealed class ContentAttachmentDetailResponse
{
    /// <summary>The attachment's id, which the single-attachment endpoints take.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>The attachment's key, unique within the item.</summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    /// <summary>MIME type of the attached file.</summary>
    [JsonPropertyName("content_type")]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>Size of the attached file in bytes.</summary>
    [JsonPropertyName("size_bytes")]
    public long SizeBytes { get; set; }

    /// <summary>Whether indexing the file for search was asked for.</summary>
    [JsonPropertyName("indexed")]
    public bool Indexed { get; set; }

    /// <summary><c>pending</c>, <c>completed</c> or <c>failed</c>; null when indexing was not asked for. Search finds the file only when <c>completed</c>.</summary>
    [JsonPropertyName("indexing_status")]
    public string? IndexingStatus { get; set; }

    /// <summary>Why the file was not indexed, when <see cref="IndexingStatus"/> is <c>failed</c>.</summary>
    [JsonPropertyName("indexing_error")]
    public string? IndexingError { get; set; }

    /// <summary>When the file was first attached.</summary>
    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    /// <summary>When the attachment last changed.</summary>
    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }

    /// <summary>What is known about the file beyond its bytes. Empty for most files.</summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, JsonElement>? Metadata { get; set; }

    /// <summary>
    /// Why the file's bytes are not served, or null when they are: <c>blocked_by_governance_policy</c>,
    /// <c>redacted_by_governance_policy</c> (any text is the redacted text), or <c>pending_screening</c>.
    /// </summary>
    [JsonPropertyName("file_withheld")]
    public string? FileWithheld { get; set; }

    /// <summary>The requested range of the text the file is indexed by; null when the file has no completed index or was indexed without text.</summary>
    [JsonPropertyName("text_content")]
    public string? TextContent { get; set; }

    [JsonPropertyName("text_content_start")]
    public int TextContentStart { get; set; }

    [JsonPropertyName("text_content_end")]
    public int TextContentEnd { get; set; }

    [JsonPropertyName("text_content_total_length")]
    public int TextContentTotalLength { get; set; }
}

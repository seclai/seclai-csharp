using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class FileUploadResponse
{
    [JsonPropertyName("content_version_id")]
    public string? ContentVersionId { get; set; }

    [JsonPropertyName("filename")]
    public string? Filename { get; set; }

    [JsonPropertyName("source_connection_content_version_id")]
    public string? SourceConnectionContentVersionId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Set when the file's type is not embedded directly on this source, so indexing relies on extracted text. Content with none (e.g. a photograph) will be marked FAILED.</summary>
    [JsonPropertyName("embedder_warning")]
    public string? EmbedderWarning { get; set; }
}

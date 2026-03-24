using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class ContentFileUploadResponse
{
    [JsonPropertyName("filename")]
    public string? Filename { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("content_version_id")]
    public string? ContentVersionId { get; set; }

    [JsonPropertyName("source_connection_content_version_id")]
    public string? SourceConnectionContentVersionId { get; set; }
}

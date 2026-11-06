using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class InlineTextUploadRequest
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, object?>? Metadata { get; set; }

    /// <summary>MIME type of the text. Omitted when null, which the server reads as <c>text/plain</c>.</summary>
    [JsonPropertyName("content_type")]
    public string? ContentType { get; set; }
}

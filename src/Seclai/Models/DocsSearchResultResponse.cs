using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class DocsSearchResultResponse
{
    [JsonPropertyName("anchor")]
    public string? Anchor { get; set; }

    [JsonPropertyName("doc_slug")]
    public string DocSlug { get; set; } = string.Empty;

    [JsonPropertyName("highlight")]
    public string? Highlight { get; set; }

    [JsonPropertyName("score")]
    public double Score { get; set; }

    [JsonPropertyName("snippet")]
    public string? Snippet { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
}

using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class InlineTextReplaceRequest
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class ExamplePrompt
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("label")]
    public string? Label { get; set; }
}

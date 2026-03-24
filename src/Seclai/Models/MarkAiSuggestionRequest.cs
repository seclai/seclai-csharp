using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class MarkAiSuggestionRequest
{
    [JsonPropertyName("accepted")]
    public bool Accepted { get; set; }
}

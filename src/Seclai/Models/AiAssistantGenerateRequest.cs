using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class AiAssistantGenerateRequest
{
    [JsonPropertyName("user_input")]
    public string? UserInput { get; set; }

    /// <summary>Optional ISO 8601 timestamp. When set, only conversation turns created at or after it are loaded as context, scoping history to the current session so the assistant remembers earlier turns in a create flow.</summary>
    [JsonPropertyName("history_since")]
    public string? HistorySince { get; set; }
}

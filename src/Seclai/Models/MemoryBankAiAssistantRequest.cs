using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class MemoryBankAiAssistantRequest
{
    [JsonPropertyName("user_input")]
    public string? UserInput { get; set; }

    [JsonPropertyName("conversation_id")]
    public string? ConversationId { get; set; }

    [JsonPropertyName("current_config")]
    public Dictionary<string, JsonElement>? CurrentConfig { get; set; }

    /// <summary>Optional ISO 8601 timestamp. When set, only conversation turns created at or after it are loaded as context, scoping history to the current session so the assistant remembers earlier turns in a multi-turn refinement.</summary>
    [JsonPropertyName("history_since")]
    public string? HistorySince { get; set; }
}

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
}

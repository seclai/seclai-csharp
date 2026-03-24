using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class AddConversationTurnRequest
{
    [JsonPropertyName("user_input")]
    public string? UserInput { get; set; }

    [JsonPropertyName("ai_response")]
    public string? AiResponse { get; set; }

    [JsonPropertyName("actions_taken")]
    public Dictionary<string, JsonElement>? ActionsTaken { get; set; }
}

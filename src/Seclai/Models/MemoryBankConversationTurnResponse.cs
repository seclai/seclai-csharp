using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class MemoryBankConversationTurnResponse
{
    [JsonPropertyName("user_input")]
    public string? UserInput { get; set; }

    [JsonPropertyName("ai_note")]
    public string? AiNote { get; set; }

    [JsonPropertyName("accepted")]
    public bool? Accepted { get; set; }

    /// <summary>Unique ID of this conversation turn.</summary>
    [JsonPropertyName("conversation_id")]
    public string? ConversationId { get; set; }

    /// <summary>The configuration proposed in this turn.</summary>
    [JsonPropertyName("resulting_config")]
    public Dictionary<string, JsonElement>? ResultingConfig { get; set; }
}

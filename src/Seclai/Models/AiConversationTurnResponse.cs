using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class AiConversationTurnResponse
{
    [JsonPropertyName("conversation_id")]
    public string? ConversationId { get; set; }

    [JsonPropertyName("user_input")]
    public string? UserInput { get; set; }

    [JsonPropertyName("ai_note")]
    public string? AiNote { get; set; }

    [JsonPropertyName("accepted")]
    public bool? Accepted { get; set; }

    [JsonPropertyName("step_id")]
    public string? StepId { get; set; }

    [JsonPropertyName("step_type")]
    public string? StepType { get; set; }

    [JsonPropertyName("resulting_config")]
    public Dictionary<string, JsonElement>? ResultingConfig { get; set; }
}

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class AiAssistantFeedbackRequest
{
    [JsonPropertyName("feature")]
    public string? Feature { get; set; }

    [JsonPropertyName("rating")]
    public string? Rating { get; set; }

    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    [JsonPropertyName("context")]
    public Dictionary<string, JsonElement>? Context { get; set; }

    [JsonPropertyName("conversation_id")]
    public string? ConversationId { get; set; }

    [JsonPropertyName("agent_conversation_id")]
    public string? AgentConversationId { get; set; }

    [JsonPropertyName("prompt_call_id")]
    public string? PromptCallId { get; set; }
}

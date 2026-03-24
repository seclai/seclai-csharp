using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class MemoryBankAiAssistantResponse
{
    [JsonPropertyName("conversation_id")]
    public string? ConversationId { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("success")]
    public bool? Success { get; set; }

    [JsonPropertyName("config")]
    public JsonElement? Config { get; set; }

    [JsonPropertyName("example_prompts")]
    public List<ExamplePrompt>? ExamplePrompts { get; set; }

    [JsonPropertyName("prompt_call_id")]
    public string? PromptCallId { get; set; }
}

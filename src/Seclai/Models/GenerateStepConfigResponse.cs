using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class GenerateStepConfigResponse
{
    [JsonPropertyName("conversation_id")]
    public string? ConversationId { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("step_type")]
    public string? StepType { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("resulting_config")]
    public Dictionary<string, JsonElement>? ResultingConfig { get; set; }

    [JsonPropertyName("example_prompts")]
    public List<ExamplePrompt>? ExamplePrompts { get; set; }
}

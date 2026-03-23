using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class AiAssistantGenerateResponse
{
    [JsonPropertyName("conversation_id")]
    public string? ConversationId { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("proposed_actions")]
    public List<ProposedActionResponse>? ProposedActions { get; set; }

    [JsonPropertyName("success")]
    public bool? Success { get; set; }

    [JsonPropertyName("requires_delete_confirmation")]
    public bool? RequiresDeleteConfirmation { get; set; }

    [JsonPropertyName("example_prompts")]
    public List<ExamplePrompt>? ExamplePrompts { get; set; }
}

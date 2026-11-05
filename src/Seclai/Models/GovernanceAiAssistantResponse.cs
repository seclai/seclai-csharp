using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class GovernanceAiAssistantResponse
{
    [JsonPropertyName("assistant_response")]
    public string? AssistantResponse { get; set; }

    /// <summary>Conversation ID to accept or decline this plan with.</summary>
    [JsonPropertyName("conversation_id")]
    public string? ConversationId { get; set; }

    /// <summary>Whether the plan was generated.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>Summary of the proposed changes.</summary>
    [JsonPropertyName("note")]
    public string? Note { get; set; }

    /// <summary>Policy actions the assistant proposes, in execution order.</summary>
    [JsonPropertyName("proposed_actions")]
    public List<ProposedPolicyActionResponse> ProposedActions { get; set; } = new();

    /// <summary>Prompt call ID for credit tracking, or null.</summary>
    [JsonPropertyName("prompt_call_id")]
    public string? PromptCallId { get; set; }

    /// <summary>Example prompts that show what the governance assistant can do.</summary>
    [JsonPropertyName("example_prompts")]
    public List<ExamplePrompt>? ExamplePrompts { get; set; }
}

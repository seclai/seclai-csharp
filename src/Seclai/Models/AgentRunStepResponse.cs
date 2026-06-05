using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class AgentRunStepResponse
{
    [JsonPropertyName("agent_step_id")]
    public string? AgentStepId { get; set; }

    [JsonPropertyName("step_type")]
    public string? StepType { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("output")]
    public string? Output { get; set; }

    [JsonPropertyName("output_content_type")]
    public string? OutputContentType { get; set; }

    [JsonPropertyName("started_at")]
    public string? StartedAt { get; set; }

    [JsonPropertyName("ended_at")]
    public string? EndedAt { get; set; }

    [JsonPropertyName("duration_seconds")]
    public float? DurationSeconds { get; set; }

    [JsonPropertyName("credits_used")]
    public float CreditsUsed { get; set; }

    /// <summary>LLM tool calls made during this step (prompt_call steps only), ordered by execution.</summary>
    [JsonPropertyName("tool_calls")]
    public List<AgentRunToolCallResponse>? ToolCalls { get; set; }
}

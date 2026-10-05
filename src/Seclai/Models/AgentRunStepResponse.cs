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

    /// <summary>
    /// Output text produced by the step; its files are in <see cref="Attachments"/>. Below
    /// <c>Seclai-Version: 2026-09-30</c> a step that output files reports the manifest JSON instead.
    /// </summary>
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

    /// <summary>Files in this step's output, in order. Empty when it produced none, when it ran before files were listed here, and once the run's trace is purged.</summary>
    [JsonPropertyName("attachments")]
    public List<AgentRunFileResponse> Attachments { get; set; } = new();

    /// <summary>Authoring problems the step ran into, whether or not it then failed, such as a file name selector that matched none of its source's files.</summary>
    [JsonPropertyName("warnings")]
    public List<string>? Warnings { get; set; }
}

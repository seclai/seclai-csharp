using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class TestDraftEvaluationRequest
{
    [JsonPropertyName("agent_input")]
    public string? AgentInput { get; set; }

    [JsonPropertyName("step_output")]
    public string? StepOutput { get; set; }

    [JsonPropertyName("evaluation_prompt")]
    public string? EvaluationPrompt { get; set; }

    [JsonPropertyName("evaluation_tier")]
    public string? EvaluationTier { get; set; }

    [JsonPropertyName("expectation_config")]
    public Dictionary<string, JsonElement>? ExpectationConfig { get; set; }

    [JsonPropertyName("pass_threshold")]
    public float? PassThreshold { get; set; }

    [JsonPropertyName("agent_step_run_id")]
    public string? AgentStepRunId { get; set; }
}

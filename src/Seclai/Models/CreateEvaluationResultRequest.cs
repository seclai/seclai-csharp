using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class CreateEvaluationResultRequest
{
    [JsonPropertyName("agent_run_id")]
    public string? AgentRunId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("agent_step_run_id")]
    public string? AgentStepRunId { get; set; }

    [JsonPropertyName("score")]
    public float? Score { get; set; }

    [JsonPropertyName("flagged")]
    public bool? Flagged { get; set; }

    [JsonPropertyName("details")]
    public Dictionary<string, JsonElement>? Details { get; set; }

    [JsonPropertyName("retry_count")]
    public int? RetryCount { get; set; }

    [JsonPropertyName("retry_triggered")]
    public bool? RetryTriggered { get; set; }
}

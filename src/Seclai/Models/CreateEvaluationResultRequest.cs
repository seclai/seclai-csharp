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

    /// <summary>Deprecated: has no effect on any count. Left null, it is not sent.</summary>
    [JsonPropertyName("retry_count")]
    public int? RetryCount { get; set; }

    /// <summary>
    /// Retired: has no effect, since evaluations never retry a step. From <c>Seclai-Version</c> <c>2026-11-04</c>
    /// (<see cref="SeclaiApiVersion.V2026_11_04"/>), <c>true</c> returns HTTP 400.
    /// </summary>
    [JsonPropertyName("retry_triggered")]
    public bool? RetryTriggered { get; set; }
}

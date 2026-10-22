using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class EvaluationResultResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("criteria_id")]
    public string? CriteriaId { get; set; }

    [JsonPropertyName("agent_run_id")]
    public string? AgentRunId { get; set; }

    [JsonPropertyName("agent_step_run_id")]
    public string? AgentStepRunId { get; set; }

    /// <summary><c>pending</c>, <c>passed</c>, <c>failed</c>, <c>skipped</c>, or <c>error</c> when the evaluator returned no verdict.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [Obsolete("The API does not send passed, so this is always false. Read Status.")]
    [JsonPropertyName("passed")]
    public bool Passed { get; set; }

    [JsonPropertyName("score")]
    public float? Score { get; set; }

    [JsonPropertyName("details")]
    public Dictionary<string, JsonElement>? Details { get; set; }

    [Obsolete("The API does not send comment, so this is always null. Read Details.")]
    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    [JsonPropertyName("retry_triggered")]
    public bool RetryTriggered { get; set; }

    [JsonPropertyName("retry_count")]
    public int RetryCount { get; set; }

    [JsonPropertyName("flagged")]
    public bool Flagged { get; set; }

    [JsonPropertyName("evaluated_at")]
    public string? EvaluatedAt { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }
}

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class AgentRunResponse
{
    [JsonPropertyName("attempts")]
    public List<AgentRunAttemptResponse> Attempts { get; set; } = new();

    [JsonPropertyName("credits")]
    public float? Credits { get; set; }

    [JsonPropertyName("error_count")]
    public int ErrorCount { get; set; }

    [JsonPropertyName("input")]
    public string? Input { get; set; }

    [JsonPropertyName("output")]
    public string? Output { get; set; }

    /// <summary>
    /// MIME type of <see cref="Output"/> — mirrors the terminal step's output_content_type.
    /// For example <c>application/vnd.seclai.manifest+json</c> is a multi-asset manifest,
    /// <c>text/*</c> is free-form text, and <c>application/json</c> is a JSON document.
    /// </summary>
    [JsonPropertyName("output_content_type")]
    public string? OutputContentType { get; set; }

    [JsonPropertyName("priority")]
    public bool Priority { get; set; }

    [JsonPropertyName("run_id")]
    public string? RunId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("steps")]
    public List<AgentRunStepResponse>? Steps { get; set; }

    /// <summary>Governance policies that produced at least one BLOCK verdict during this run.</summary>
    [JsonPropertyName("blocked_policies")]
    public List<GovernancePolicyRefResponse>? BlockedPolicies { get; set; }

    /// <summary>Governance policies that produced at least one FLAG verdict during this run.</summary>
    [JsonPropertyName("flagged_policies")]
    public List<GovernancePolicyRefResponse>? FlaggedPolicies { get; set; }

    /// <summary>Result of the prompt injection scan: safe, unsafe, skipped, timed_out, or error.</summary>
    [JsonPropertyName("input_scan_status")]
    public string? InputScanStatus { get; set; }

    /// <summary>Milliseconds spent waiting for prompt injection scan.</summary>
    [JsonPropertyName("scan_wait_ms")]
    public int? ScanWaitMs { get; set; }

    /// <summary>Result of the governance input evaluation: safe, blocked, skipped, or timed_out.</summary>
    [JsonPropertyName("governance_input_status")]
    public string? GovernanceInputStatus { get; set; }

    /// <summary>Milliseconds spent waiting for governance input evaluation.</summary>
    [JsonPropertyName("governance_input_wait_ms")]
    public int? GovernanceInputWaitMs { get; set; }

    /// <summary>
    /// Cumulative milliseconds the run was parked waiting for a human decision on a
    /// human_in_the_loop step. Subtracted from active duration in run-detail and
    /// duration-stats responses.
    /// </summary>
    [JsonPropertyName("hitl_wait_ms")]
    public int? HitlWaitMs { get; set; }

    /// <summary>Cumulative milliseconds the run was parked on standard-mode wait steps. Subtracted from active duration in run-detail and duration-stats responses, exactly like hitl_wait_ms. Priority waits block inline and are not counted here.</summary>
    [JsonPropertyName("wait_ms")]
    public int? WaitMs { get; set; }
}

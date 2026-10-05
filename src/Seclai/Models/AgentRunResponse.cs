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

    /// <summary>
    /// The run's output text; its files are in <see cref="Attachments"/>. Below
    /// <c>Seclai-Version: 2026-09-30</c> an output that has files is instead the manifest JSON.
    /// </summary>
    [JsonPropertyName("output")]
    public string? Output { get; set; }

    /// <summary>
    /// MIME type of <see cref="Output"/> — mirrors the terminal step's output_content_type.
    /// <c>text/*</c> is free-form text and <c>application/json</c> is a JSON document. Below
    /// <c>Seclai-Version: 2026-09-30</c> an output that has files reads
    /// <c>application/vnd.seclai.manifest+json</c>.
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

    /// <summary>Files in the run's output, in order. Empty when it produced none, when it ran before files were listed here, and once the run's trace is purged.</summary>
    [JsonPropertyName("attachments")]
    public List<AgentRunFileResponse> Attachments { get; set; } = new();

    /// <summary>When this run's trace content was deleted under the account's agent-trace retention window. Non-null means the run's and every step's input and output are null by design: the run aged out, it did not fail.</summary>
    [JsonPropertyName("trace_purged_at")]
    public string? TracePurgedAt { get; set; }
}

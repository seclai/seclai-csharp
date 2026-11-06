using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class UpdateAgentRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("is_public")]
    public bool? IsPublic { get; set; }

    [JsonPropertyName("solution_id")]
    public string? SolutionId { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Optional payload in the same format produced by <c>GET /agents/{id}/export</c>.
    /// When provided, the agent's workflow is replaced from <c>agent.definition</c> and
    /// metadata fields the request does not set explicitly are taken from the payload.
    /// Unlike <c>POST /agents</c>, update does NOT apply <c>alert_configs</c>,
    /// <c>evaluation_criteria</c>, <c>governance_policies</c>, <c>schedules</c>, or
    /// solution links from the imported file. Validation errors are returned as
    /// <see cref="AgentDefinitionImportErrorResponse"/> on HTTP 422.
    /// </summary>
    [JsonPropertyName("agent_definition")]
    public Dictionary<string, JsonElement>? AgentDefinition { get; set; }

    /// <summary>
    /// Optional UUID-substitution map applied to the imported workflow before save
    /// (same shape as on <c>POST /agents</c>).
    /// </summary>
    [JsonPropertyName("entity_remap")]
    public Dictionary<string, string>? EntityRemap { get; set; }

    /// <summary>With <see cref="AgentDefinition"/>, also replace the agent's run limits with the payload's (a null there removes a limit). Off by default: the agent keeps its limits.</summary>
    [JsonPropertyName("apply_import_run_limits")]
    public bool? ApplyImportRunLimits { get; set; }

    /// <summary>Credit limit per billing period of the paying account: once reached, runs stop at their next step and new runs are refused. Leave null to keep it; to remove it, leave this null and set <see cref="SetMaxCreditsPerPeriod"/>.</summary>
    [JsonPropertyName("max_credits_per_period")]
    public double? MaxCreditsPerPeriod { get; set; }

    /// <summary>When true and <see cref="MaxCreditsPerPeriod"/> is null, removes that limit.</summary>
    [JsonPropertyName("set_max_credits_per_period")]
    public bool? SetMaxCreditsPerPeriod { get; set; }

    /// <summary>Credit limit per run: a run that reaches it is stopped before its next step. Leave null to keep it; to remove it, leave this null and set <see cref="SetMaxCreditsPerRun"/>.</summary>
    [JsonPropertyName("max_credits_per_run")]
    public double? MaxCreditsPerRun { get; set; }

    /// <summary>When true and <see cref="MaxCreditsPerRun"/> is null, removes that limit.</summary>
    [JsonPropertyName("set_max_credits_per_run")]
    public bool? SetMaxCreditsPerRun { get; set; }

    /// <summary>Runs per day; at most the plan's run quota. Leave null to keep it; to remove it, leave this null and set <see cref="SetMaxRunsPerDay"/>.</summary>
    [JsonPropertyName("max_runs_per_day")]
    public int? MaxRunsPerDay { get; set; }

    /// <summary>When true and <see cref="MaxRunsPerDay"/> is null, removes that limit.</summary>
    [JsonPropertyName("set_max_runs_per_day")]
    public bool? SetMaxRunsPerDay { get; set; }

    /// <summary>Runs per hour; at most the plan's run quota. Leave null to keep it; to remove it, leave this null and set <see cref="SetMaxRunsPerHour"/>.</summary>
    [JsonPropertyName("max_runs_per_hour")]
    public int? MaxRunsPerHour { get; set; }

    /// <summary>When true and <see cref="MaxRunsPerHour"/> is null, removes that limit.</summary>
    [JsonPropertyName("set_max_runs_per_hour")]
    public bool? SetMaxRunsPerHour { get; set; }

    /// <summary>Runs in total; once reached, new runs are refused. Leave null to keep it; to remove it, leave this null and set <see cref="SetMaxRunsTotal"/>.</summary>
    [JsonPropertyName("max_runs_total")]
    public int? MaxRunsTotal { get; set; }

    /// <summary>When true and <see cref="MaxRunsTotal"/> is null, removes that limit.</summary>
    [JsonPropertyName("set_max_runs_total")]
    public bool? SetMaxRunsTotal { get; set; }

    /// <summary>Default evaluation tier: <c>fast</c>, <c>balanced</c>, or <c>thorough</c>. Leave null to keep it; to clear it (system default), leave this null and set <see cref="SetDefaultEvaluationTier"/>.</summary>
    [JsonPropertyName("default_evaluation_tier")]
    public string? DefaultEvaluationTier { get; set; }

    /// <summary>When true and <see cref="DefaultEvaluationTier"/> is null, clears the tier to null (system default).</summary>
    [JsonPropertyName("set_default_evaluation_tier")]
    public bool? SetDefaultEvaluationTier { get; set; }

    /// <summary>
    /// Evaluation mode: <c>output_expectation</c> or <c>sample_and_flag</c>. Leave null to keep it.
    /// From <c>Seclai-Version</c> <c>2026-11-04</c> (<see cref="SeclaiApiVersion.V2026_11_04"/>), <c>eval_and_retry</c> is refused with HTTP 400;
    /// earlier versions store it as <c>sample_and_flag</c>.
    /// </summary>
    [JsonPropertyName("evaluation_mode")]
    public string? EvaluationMode { get; set; }

    /// <summary>
    /// Sampling configuration for <c>sample_and_flag</c> mode, in the form
    /// <c>{"combinator": "and"|"or", "rules": [...]}</c>. Leave null to keep it; to clear it,
    /// leave this null and set <see cref="SetSamplingConfig"/>.
    /// </summary>
    [JsonPropertyName("sampling_config")]
    public Dictionary<string, JsonElement>? SamplingConfig { get; set; }

    /// <summary>When true and <see cref="SamplingConfig"/> is null, clears the config to null.</summary>
    [JsonPropertyName("set_sampling_config")]
    public bool? SetSamplingConfig { get; set; }

    /// <summary>Auto-upgrade strategy for the agent's prompt models: <c>none</c>, <c>early_adopter</c>, <c>middle_of_road</c>, or <c>cautious_adopter</c>. Leave null to keep it.</summary>
    [JsonPropertyName("prompt_model_auto_upgrade_strategy")]
    public string? PromptModelAutoUpgradeStrategy { get; set; }

    /// <summary>Enable or disable automatic rollback for upgraded models. Leave null to keep it.</summary>
    [JsonPropertyName("prompt_model_auto_rollback_enabled")]
    public bool? PromptModelAutoRollbackEnabled { get; set; }

    /// <summary>
    /// Failure signals that trigger rollback: <c>governance_flag</c>, <c>governance_block</c>, <c>agent_run_failed</c>.
    /// <c>agent_eval_fail</c> is accepted and never fires; when it is in the list, a run with a failed evaluation is
    /// never picked as the rollback target. Leave null to keep it; to revert to the system defaults, leave this null
    /// and set <see cref="SetPromptModelAutoRollbackTriggers"/>.
    /// </summary>
    [JsonPropertyName("prompt_model_auto_rollback_triggers")]
    public List<string>? PromptModelAutoRollbackTriggers { get; set; }

    /// <summary>When true and <see cref="PromptModelAutoRollbackTriggers"/> is null, clears the list to null (system defaults).</summary>
    [JsonPropertyName("set_prompt_model_auto_rollback_triggers")]
    public bool? SetPromptModelAutoRollbackTriggers { get; set; }

    /// <summary>Accepted for compatibility and has no effect: the API ignores it.</summary>
    [JsonPropertyName("max_retries")]
    public int? MaxRetries { get; set; }

    /// <summary>Accepted for compatibility and has no effect: the API ignores it.</summary>
    [JsonPropertyName("retry_on_failure")]
    public bool? RetryOnFailure { get; set; }
}

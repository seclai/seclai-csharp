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
}

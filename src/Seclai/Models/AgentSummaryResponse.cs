using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class AgentSummaryResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("is_public")]
    public bool IsPublic { get; set; }

    /// <summary>The solution the agent belongs to. On a create from an <c>agent_template</c> that created resources, the new solution grouping the agent with them.</summary>
    [JsonPropertyName("solution_id")]
    public string? SolutionId { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }

    /// <summary>
    /// One entry per item dropped or substituted during an <c>agent_definition</c> import.
    /// Present only on endpoints that accept <c>agent_definition</c> (returned by
    /// <c>POST /agents</c> and <c>PUT /agents/{id}</c>); <c>null</c> on other calls;
    /// empty list when the import had no skips.
    /// </summary>
    [JsonPropertyName("import_warnings")]
    public List<ImportSkipResponse>? ImportWarnings { get; set; }

    /// <summary>Whether the agent is paused (disabled).</summary>
    [JsonPropertyName("disabled")]
    public bool? Disabled { get; set; }

    /// <summary>ISO 8601 timestamp the agent was paused.</summary>
    [JsonPropertyName("disabled_at")]
    public string? DisabledAt { get; set; }

    /// <summary>Why the agent is paused: 'manual' or 'email_overload'.</summary>
    [JsonPropertyName("disabled_reason")]
    public string? DisabledReason { get; set; }

    /// <summary>ID of the agent's trigger, for the <c>/agents/{agent_id}/triggers/{trigger_id}/…</c> endpoints such as <see cref="SeclaiClient.SetAcceptedAttachmentsAsync"/> and <see cref="SeclaiClient.SetEmailTriggerConfigAsync"/>.</summary>
    [JsonPropertyName("trigger_id")]
    public string? TriggerId { get; set; }

    /// <summary>On a create from an <c>agent_template</c>: <c>{slot: resource_id}</c> for each resource <see cref="CreateAgentRequest.CreateMissingResources"/> created. Null otherwise.</summary>
    [JsonPropertyName("created_resources")]
    public Dictionary<string, string>? CreatedResources { get; set; }

    /// <summary>Credit limit per billing period; null when none.</summary>
    [JsonPropertyName("max_credits_per_period")]
    public double? MaxCreditsPerPeriod { get; set; }

    /// <summary>Credit limit per run; null when none.</summary>
    [JsonPropertyName("max_credits_per_run")]
    public double? MaxCreditsPerRun { get; set; }

    /// <summary>Runs per day limit; null when none.</summary>
    [JsonPropertyName("max_runs_per_day")]
    public int? MaxRunsPerDay { get; set; }

    /// <summary>Runs per hour limit; null when none.</summary>
    [JsonPropertyName("max_runs_per_hour")]
    public int? MaxRunsPerHour { get; set; }

    /// <summary>Limit on runs in total; null when none.</summary>
    [JsonPropertyName("max_runs_total")]
    public int? MaxRunsTotal { get; set; }
}

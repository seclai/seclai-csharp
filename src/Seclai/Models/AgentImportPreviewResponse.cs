using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>
/// Summary of a successfully validated <c>agent_definition</c> import payload (no DB writes).
///
/// Counts reflect what the payload requested; cross-account skips (recipients, KB names)
/// only happen on commit.
/// </summary>
public sealed class AgentImportPreviewResponse
{
    /// <summary>Always true on a 200 response; failures use HTTP 422.</summary>
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    /// <summary>Imported agent name, if any.</summary>
    [JsonPropertyName("agent_name")]
    public string? AgentName { get; set; }

    /// <summary>Imported agent description, if any.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Total number of steps in the workflow tree (recursive).</summary>
    [JsonPropertyName("step_count")]
    public int StepCount { get; set; }

    /// <summary>Number of trigger schedules in the payload.</summary>
    [JsonPropertyName("schedules")]
    public int Schedules { get; set; }

    /// <summary>Number of alert configs in the payload.</summary>
    [JsonPropertyName("alert_configs")]
    public int AlertConfigs { get; set; }

    /// <summary>Number of evaluation criteria in the payload.</summary>
    [JsonPropertyName("evaluation_criteria")]
    public int EvaluationCriteria { get; set; }

    /// <summary>Number of agent-scoped governance policies in the payload.</summary>
    [JsonPropertyName("governance_policies")]
    public int GovernancePolicies { get; set; }

    /// <summary>
    /// Number of solutions the source agent belonged to.  Solutions are matched by name
    /// in the target account; unmatched names are silently skipped on commit.
    /// </summary>
    [JsonPropertyName("solutions")]
    public int Solutions { get; set; }

    /// <summary>Export-format version this server understands.</summary>
    [JsonPropertyName("supported_export_version")]
    public string? SupportedExportVersion { get; set; }

    /// <summary>
    /// Export-format version the payload claims (or null for legacy payloads).
    /// Differences against <see cref="SupportedExportVersion"/> indicate cross-version imports.
    /// </summary>
    [JsonPropertyName("payload_export_version")]
    public string? PayloadExportVersion { get; set; }

    /// <summary>
    /// Entity references in the imported workflow that don't exist in the target account.
    /// Each entry contains <c>category</c>, <c>ref_id</c>, optional <c>ref_name</c>,
    /// <c>locations</c>, and <c>alternatives</c>. Pass <c>{source_uuid: target_uuid}</c>
    /// as <c>entity_remap</c> on the create/update call to substitute these references
    /// before save.
    /// </summary>
    [JsonPropertyName("unresolved_refs")]
    public List<Dictionary<string, JsonElement>>? UnresolvedRefs { get; set; }
}

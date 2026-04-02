using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>Portable JSON snapshot of an agent definition.</summary>
public sealed class AgentExportResponse
{
    [JsonPropertyName("export_version")]
    public string? ExportVersion { get; set; }

    [JsonPropertyName("exported_at")]
    public string? ExportedAt { get; set; }

    [JsonPropertyName("software_version")]
    public string? SoftwareVersion { get; set; }

    [JsonPropertyName("agent")]
    public Dictionary<string, JsonElement>? Agent { get; set; }

    [JsonPropertyName("trigger")]
    public Dictionary<string, JsonElement>? Trigger { get; set; }

    [JsonPropertyName("alert_configs")]
    public List<Dictionary<string, JsonElement>>? AlertConfigs { get; set; }

    [JsonPropertyName("evaluation_criteria")]
    public List<Dictionary<string, JsonElement>>? EvaluationCriteria { get; set; }

    [JsonPropertyName("governance_policies")]
    public List<Dictionary<string, JsonElement>>? GovernancePolicies { get; set; }

    [JsonPropertyName("dependencies")]
    public Dictionary<string, JsonElement>? Dependencies { get; set; }
}

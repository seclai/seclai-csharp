using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>A governance policy action proposed by <see cref="SeclaiClient.GenerateGovernanceAiPlanAsync"/>.</summary>
public sealed class ProposedPolicyActionResponse
{
    /// <summary>Type of action: <c>create</c>, <c>update</c>, <c>delete</c>, <c>enable</c>, or <c>disable</c>.</summary>
    [JsonPropertyName("action_type")]
    public string? ActionType { get; set; }

    /// <summary>What the action will do.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Parameters for the action, such as <c>policy_document_id</c> or thresholds.</summary>
    [JsonPropertyName("params")]
    public Dictionary<string, JsonElement>? Params { get; set; }
}

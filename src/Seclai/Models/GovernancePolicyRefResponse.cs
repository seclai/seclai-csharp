using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>Reference to a governance policy (id and optional display name).</summary>
public sealed class GovernancePolicyRefResponse
{
    /// <summary>Governance policy identifier.</summary>
    [JsonPropertyName("policy_id")]
    public string? PolicyId { get; set; }

    /// <summary>Display name of the policy at evaluation time. May be null when the policy has been deleted.</summary>
    [JsonPropertyName("policy_name")]
    public string? PolicyName { get; set; }
}

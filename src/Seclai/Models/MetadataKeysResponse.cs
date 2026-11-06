using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>The run metadata keys a trigger declares, and the ones it could declare.</summary>
public sealed class MetadataKeysResponse
{
    [JsonPropertyName("trigger_id")]
    public string TriggerId { get; set; } = string.Empty;

    [JsonPropertyName("agent_id")]
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    /// The keys the agent's steps may reference. Static trigger metadata and Slack form fields are declared
    /// custom keys too, and are not repeated here.
    /// </summary>
    [JsonPropertyName("metadata_keys")]
    public List<MetadataKeyDeclaration> MetadataKeys { get; set; } = new();

    /// <summary>The system and source keys this trigger's type can declare.</summary>
    [JsonPropertyName("declarable_keys")]
    public List<DeclarableMetadataKey> DeclarableKeys { get; set; } = new();

    /// <summary>True for content triggers, which may declare any item-metadata key.</summary>
    [JsonPropertyName("accepts_any_source_key")]
    public bool AcceptsAnySourceKey { get; set; }

    /// <summary>
    /// Set by <see cref="SeclaiClient.SetMetadataKeysAsync"/> when it changes the declaration: agents whose
    /// <c>call_agent</c> mapping no longer fits it. Always empty from <see cref="SeclaiClient.GetMetadataKeysAsync"/>.
    /// </summary>
    [JsonPropertyName("warnings")]
    public List<string> Warnings { get; set; } = new();
}

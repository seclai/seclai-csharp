using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>Replaces the run metadata keys a trigger declares.</summary>
public sealed class SetMetadataKeysRequest
{
    /// <summary>The whole declaration; it replaces the trigger's current one. An empty list declares no keys.</summary>
    [JsonPropertyName("metadata_keys")]
    public List<MetadataKeyDeclaration> MetadataKeys { get; set; } = new();
}

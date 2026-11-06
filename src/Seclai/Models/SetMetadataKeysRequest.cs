using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>Replaces the run metadata keys a trigger declares.</summary>
public sealed class SetMetadataKeysRequest
{
    /// <summary>
    /// The whole declaration; it replaces the trigger's current one. Required:
    /// <see cref="SeclaiClient.SetMetadataKeysAsync"/> throws <see cref="System.ArgumentException"/> when it is
    /// null, so a request built without it cannot wipe the declaration. To declare no keys, set an empty list.
    /// </summary>
    [JsonPropertyName("metadata_keys")]
    public List<MetadataKeyDeclaration>? MetadataKeys { get; set; }
}

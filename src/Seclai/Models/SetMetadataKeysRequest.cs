using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>Replaces the run metadata keys a trigger declares.</summary>
public sealed class SetMetadataKeysRequest
{
    /// <summary>Creates a request that replaces the trigger's declaration with <paramref name="metadataKeys"/>.</summary>
    /// <param name="metadataKeys">The whole declaration. An empty list declares no keys.</param>
    /// <exception cref="ArgumentNullException"><paramref name="metadataKeys"/> is null.</exception>
    public SetMetadataKeysRequest(List<MetadataKeyDeclaration> metadataKeys)
    {
        MetadataKeys = metadataKeys ?? throw new ArgumentNullException(nameof(metadataKeys));
    }

    /// <summary>
    /// The whole declaration; it replaces the trigger's current one. To declare no keys, use an empty list.
    /// <see cref="SeclaiClient.SetMetadataKeysAsync"/> still throws <see cref="ArgumentException"/> if it is set to
    /// null afterwards, so such a request cannot wipe the declaration.
    /// </summary>
    [JsonPropertyName("metadata_keys")]
    public List<MetadataKeyDeclaration> MetadataKeys { get; set; }
}

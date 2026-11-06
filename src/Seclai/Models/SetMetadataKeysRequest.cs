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
        _metadataKeys = metadataKeys ?? throw new ArgumentNullException(nameof(metadataKeys));
    }

    private List<MetadataKeyDeclaration> _metadataKeys;

    /// <summary>
    /// The whole declaration; it replaces the trigger's current one. To declare no keys, use an empty list.
    /// </summary>
    /// <exception cref="ArgumentNullException">Set to null, which would wipe the declaration.</exception>
    [JsonPropertyName("metadata_keys")]
    public List<MetadataKeyDeclaration> MetadataKeys
    {
        get => _metadataKeys;
        set => _metadataKeys = value ?? throw new ArgumentNullException(nameof(MetadataKeys));
    }
}

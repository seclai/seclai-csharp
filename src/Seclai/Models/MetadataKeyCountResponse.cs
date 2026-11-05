using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>How often one item-metadata key appears in a window.</summary>
public sealed class MetadataKeyCountResponse
{
    /// <summary>The item-metadata key.</summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    /// <summary>Items in the window carrying the key.</summary>
    [JsonPropertyName("items")]
    public int Items { get; set; }

    /// <summary>How many different values it takes.</summary>
    [JsonPropertyName("distinct_values")]
    public int DistinctValues { get; set; }

    /// <summary>Its 5 most frequent values, most frequent first.</summary>
    [JsonPropertyName("top_values")]
    public List<MetadataValueCountResponse> TopValues { get; set; } = new();
}

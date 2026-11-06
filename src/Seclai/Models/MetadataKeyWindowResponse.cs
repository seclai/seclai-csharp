using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>Item-metadata key counts over one time window.</summary>
public sealed class MetadataKeyWindowResponse
{
    /// <summary><c>all_time</c>, <c>last_30_days</c> or <c>last_7_days</c>.</summary>
    [JsonPropertyName("window")]
    public string Window { get; set; } = string.Empty;

    /// <summary>The window's start (ISO 8601); null for all time.</summary>
    [JsonPropertyName("since")]
    public string? Since { get; set; }

    /// <summary>Items in the window, including those with no metadata.</summary>
    [JsonPropertyName("total_items")]
    public int TotalItems { get; set; }

    /// <summary>Keys seen in the window, most frequent first.</summary>
    [JsonPropertyName("keys")]
    public List<MetadataKeyCountResponse> Keys { get; set; } = new();
}

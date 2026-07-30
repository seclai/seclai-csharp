using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class UpdateSourceRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("polling")]
    public string? Polling { get; set; }

    [JsonPropertyName("retention_days")]
    public int? RetentionDays { get; set; }

    /// <summary>Media kinds to extract from indexed content and embed as multi-modal KB chunks. Subset of ['images', 'video']. Only kinds the source's embedder can index are honored; unsupported values are dropped. [] disables media extraction (text-only).</summary>
    [JsonPropertyName("media_types")]
    public List<string>? MediaTypes { get; set; }
}

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class CreateExportRequest
{
    [JsonPropertyName("format")]
    public string? Format { get; set; }

    [JsonPropertyName("filters")]
    public Dictionary<string, JsonElement>? Filters { get; set; }

    /// <summary>Only include content created on or after this ISO 8601 timestamp.</summary>
    [JsonPropertyName("date_from")]
    public string? DateFrom { get; set; }

    /// <summary>Only include content created on or before this ISO 8601 timestamp.</summary>
    [JsonPropertyName("date_to")]
    public string? DateTo { get; set; }

    /// <summary>JSONB containment filter applied to content metadata.</summary>
    [JsonPropertyName("metadata_filter")]
    public Dictionary<string, JsonElement>? MetadataFilter { get; set; }

    /// <summary>Title substring filter, case-insensitive.</summary>
    [JsonPropertyName("query_filter")]
    public string? QueryFilter { get; set; }
}

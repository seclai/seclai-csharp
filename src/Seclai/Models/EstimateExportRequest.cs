using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class EstimateExportRequest
{
    [JsonPropertyName("format")]
    public string? Format { get; set; }

    [JsonPropertyName("date_from")]
    public string? DateFrom { get; set; }

    [JsonPropertyName("date_to")]
    public string? DateTo { get; set; }

    [JsonPropertyName("metadata_filter")]
    public Dictionary<string, JsonElement>? MetadataFilter { get; set; }

    [JsonPropertyName("query_filter")]
    public string? QueryFilter { get; set; }
}

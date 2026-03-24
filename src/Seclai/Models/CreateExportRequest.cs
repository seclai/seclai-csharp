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
}

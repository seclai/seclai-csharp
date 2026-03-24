using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class ExportListResponse
{
    [JsonPropertyName("data")]
    public List<ExportResponse> Data { get; set; } = new();

    [JsonPropertyName("total")]
    public int Total { get; set; }
}

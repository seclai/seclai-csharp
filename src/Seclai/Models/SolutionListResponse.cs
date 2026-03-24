using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class SolutionListResponse
{
    [JsonPropertyName("data")]
    public List<SolutionResponse> Data { get; set; } = new();

    [JsonPropertyName("total")]
    public int Total { get; set; }
}

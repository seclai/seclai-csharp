using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class AgentListResponse
{
    [JsonPropertyName("data")]
    public List<AgentSummaryResponse> Data { get; set; } = new();

    [JsonPropertyName("total")]
    public int Total { get; set; }
}

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class AgentRunListResponse
{
    [JsonPropertyName("data")]
    public List<AgentRunResponse> Data { get; set; } = new();

    [JsonPropertyName("pagination")]
    public PaginationResponse Pagination { get; set; } = new();
}

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>A page of agent-email opt-outs plus the total (for pagination).</summary>
public sealed class AgentEmailOptOutListResponse
{
    [JsonPropertyName("items")]
    public List<AgentEmailOptOutResponse> Items { get; set; } = new();

    [JsonPropertyName("total")]
    public int Total { get; set; }
}

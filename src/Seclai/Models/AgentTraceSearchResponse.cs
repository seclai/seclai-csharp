using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class AgentTraceSearchResponse
{
    [JsonPropertyName("results")]
    public List<Dictionary<string, JsonElement>>? Results { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }
}

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class AgentTraceSearchResponse
{
    /// <summary>The matching trace entries, read from the <c>matches</c> key the endpoint returns.</summary>
    [JsonPropertyName("matches")]
    public List<Dictionary<string, JsonElement>>? Results { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }
}

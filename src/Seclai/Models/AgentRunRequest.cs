using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class AgentRunRequest
{
    [JsonPropertyName("input")]
    public string? Input { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, JsonElement>? Metadata { get; set; }

    [JsonPropertyName("priority")]
    public bool? Priority { get; set; }
}

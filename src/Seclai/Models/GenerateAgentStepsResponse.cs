using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class GenerateAgentStepsResponse
{
    [JsonPropertyName("steps")]
    public List<Dictionary<string, JsonElement>>? Steps { get; set; }
}

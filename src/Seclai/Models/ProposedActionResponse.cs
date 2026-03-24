using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class ProposedActionResponse
{
    [JsonPropertyName("action_type")]
    public string? ActionType { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("params")]
    public Dictionary<string, JsonElement>? Params { get; set; }

    [JsonPropertyName("is_destructive")]
    public bool? IsDestructive { get; set; }
}

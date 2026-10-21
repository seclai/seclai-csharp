using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class AgentRunStreamRequest
{
    [JsonPropertyName("input")]
    public string? Input { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, JsonElement>? Metadata { get; set; }

    /// <summary>
    /// Receive <c>stream_reasoning</c> events carrying the model's reasoning while it thinks. They are sent
    /// only for a <c>streaming_result</c> step whose author enabled <c>show_reasoning</c>, and never when a
    /// governance policy redacts that step. Null omits the field (server default false).
    /// </summary>
    [JsonPropertyName("include_reasoning")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IncludeReasoning { get; set; }
}

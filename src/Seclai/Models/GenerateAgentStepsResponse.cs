using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class GenerateAgentStepsResponse
{
    [JsonPropertyName("steps")]
    public List<Dictionary<string, JsonElement>>? Steps { get; set; }

    /// <summary>How the assistant interpreted the request: 'clear' when steps were generated, or an ask-path value (e.g. 'ambiguous_output', 'cannot_build') when it returned no steps and put a clarifying question or blocker in <c>note</c>. Mirrors the MCP surface so callers can distinguish a clarification pause from a h</summary>
    [JsonPropertyName("intent_assessment")]
    public string? IntentAssessment { get; set; }
}

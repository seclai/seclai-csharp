using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class GenerateAgentStepsResponse
{
    [JsonPropertyName("steps")]
    public List<Dictionary<string, JsonElement>>? Steps { get; set; }

    /// <summary>
    /// How the assistant interpreted the request: <c>clear</c> when steps were generated,
    /// or an ask-path value (<c>ambiguous_output</c>, <c>cannot_build</c>) when it returned
    /// no steps and put a clarifying question or blocker in <c>note</c>. Lets callers tell a
    /// clarification pause apart from a hard failure.
    /// </summary>
    [JsonPropertyName("intent_assessment")]
    public string? IntentAssessment { get; set; }
}

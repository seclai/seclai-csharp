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

    /// <summary>
    /// Dry-run preview the solution AI assistant attaches to <c>create_agent</c> and <c>update_agent</c> actions: <c>steps</c> (the generated
    /// step tree), <c>step_count</c>, <c>warnings</c> (structural issues and unreferenced or unknown resources), and <c>skipped</c> /
    /// <c>skipped_reason</c> when the preview could not run, for example because the action depends on resources created earlier in the same
    /// plan. Null for other actions or when generation failed.
    /// </summary>
    [JsonPropertyName("preview")]
    public Dictionary<string, JsonElement>? Preview { get; set; }
}

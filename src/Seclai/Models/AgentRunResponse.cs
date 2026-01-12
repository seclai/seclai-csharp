using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class AgentRunResponse
{
    [JsonPropertyName("attempts")]
    public List<AgentRunAttemptResponse> Attempts { get; set; } = new();

    [JsonPropertyName("credits")]
    public float? Credits { get; set; }

    [JsonPropertyName("error_count")]
    public int ErrorCount { get; set; }

    [JsonPropertyName("input")]
    public string? Input { get; set; }

    [JsonPropertyName("output")]
    public string? Output { get; set; }

    [JsonPropertyName("priority")]
    public bool Priority { get; set; }

    [JsonPropertyName("run_id")]
    public string? RunId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

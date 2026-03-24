using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class AgentTraceSearchRequest
{
    [JsonPropertyName("agent_id")]
    public string? AgentId { get; set; }

    [JsonPropertyName("query")]
    public string? Query { get; set; }

    [JsonPropertyName("run_status")]
    public string? RunStatus { get; set; }

    [JsonPropertyName("step_type")]
    public string? StepType { get; set; }

    [JsonPropertyName("top_n")]
    public int? TopN { get; set; }
}

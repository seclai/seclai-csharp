using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class GenerateStepConfigRequest
{
    [JsonPropertyName("step_type")]
    public string? StepType { get; set; }

    [JsonPropertyName("user_input")]
    public string? UserInput { get; set; }

    [JsonPropertyName("step_id")]
    public string? StepId { get; set; }

    [JsonPropertyName("current_config")]
    public Dictionary<string, JsonElement>? CurrentConfig { get; set; }

    [JsonPropertyName("agent_steps")]
    public List<Dictionary<string, JsonElement>>? AgentSteps { get; set; }
}

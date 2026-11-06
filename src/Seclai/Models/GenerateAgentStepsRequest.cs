using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class GenerateAgentStepsRequest
{
    [JsonPropertyName("user_input")]
    public string? UserInput { get; set; }

    /// <summary><c>generate_full</c> (the server default) to create steps from scratch, or <c>modify_workflow</c> to refine <see cref="AgentSteps"/>.</summary>
    [JsonPropertyName("mode")]
    public string? Mode { get; set; }

    /// <summary>The agent's current step hierarchy, for context when modifying.</summary>
    [JsonPropertyName("agent_steps")]
    public List<Dictionary<string, JsonElement>>? AgentSteps { get; set; }

    /// <summary>Agent description, for additional context.</summary>
    [JsonPropertyName("agent_description")]
    public string? AgentDescription { get; set; }

    /// <summary>The agent's trigger type, for context (for example <c>dynamic_input</c>, <c>content_added</c>).</summary>
    [JsonPropertyName("trigger_type")]
    public string? TriggerType { get; set; }
}

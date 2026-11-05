using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>An agent linked to a solution, as listed in <see cref="SolutionResponse.Agents"/>.</summary>
public sealed class SolutionAgentResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>Knowledge bases connected to this agent through its triggers.</summary>
    [JsonPropertyName("knowledge_base_ids")]
    public List<string>? KnowledgeBaseIds { get; set; }
}

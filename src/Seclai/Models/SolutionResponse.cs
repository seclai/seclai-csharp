using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class SolutionResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }

    /// <summary>Agents linked to the solution.</summary>
    [JsonPropertyName("agents")]
    public List<SolutionAgentResponse> Agents { get; set; } = new();

    /// <summary>Knowledge bases linked to the solution.</summary>
    [JsonPropertyName("knowledge_bases")]
    public List<SolutionKnowledgeBaseResponse> KnowledgeBases { get; set; } = new();

    /// <summary>Source connections linked to the solution.</summary>
    [JsonPropertyName("source_connections")]
    public List<SolutionSourceConnectionResponse> SourceConnections { get; set; } = new();
}

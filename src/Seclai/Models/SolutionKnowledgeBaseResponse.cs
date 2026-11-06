using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>A knowledge base linked to a solution, as listed in <see cref="SolutionResponse.KnowledgeBases"/>.</summary>
public sealed class SolutionKnowledgeBaseResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>Source connections linked to this knowledge base.</summary>
    [JsonPropertyName("source_connection_ids")]
    public List<string>? SourceConnectionIds { get; set; }
}

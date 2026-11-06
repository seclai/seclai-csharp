using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>A source connection linked to a solution, as listed in <see cref="SolutionResponse.SourceConnections"/>.</summary>
public sealed class SolutionSourceConnectionResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class CreateKnowledgeBaseRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("solution_id")]
    public string? SolutionId { get; set; }
}

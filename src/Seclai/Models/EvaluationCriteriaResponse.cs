using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class EvaluationCriteriaResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

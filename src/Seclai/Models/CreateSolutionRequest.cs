using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class CreateSolutionRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

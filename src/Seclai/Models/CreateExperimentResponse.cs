using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class CreateExperimentResponse
{
    [JsonPropertyName("experiment_id")]
    public string ExperimentId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

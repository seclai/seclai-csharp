using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class CancelExperimentResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

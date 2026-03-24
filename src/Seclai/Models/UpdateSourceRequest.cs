using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class UpdateSourceRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("polling")]
    public string? Polling { get; set; }

    [JsonPropertyName("retention_days")]
    public int? RetentionDays { get; set; }
}

using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class DnsRecordResponse
{
    [JsonPropertyName("detail")]
    public string? Detail { get; set; }

    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("mx_host")]
    public string? MxHost { get; set; }

    [JsonPropertyName("mx_priority")]
    public int? MxPriority { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [JsonPropertyName("relative_name")]
    public string RelativeName { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

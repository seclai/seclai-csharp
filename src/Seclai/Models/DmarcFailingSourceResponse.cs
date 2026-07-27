using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class DmarcFailingSourceResponse
{
    [JsonPropertyName("failed_count")]
    public int FailedCount { get; set; }

    [JsonPropertyName("header_from")]
    public string? HeaderFrom { get; set; }

    [JsonPropertyName("source_ip")]
    public string SourceIp { get; set; } = string.Empty;
}

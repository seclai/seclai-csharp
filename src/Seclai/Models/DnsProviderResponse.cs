using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class DnsProviderResponse
{
    [JsonPropertyName("dashboard_url")]
    public string? DashboardUrl { get; set; }

    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("mx_priority_separate")]
    public bool? MxPrioritySeparate { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("tips")]
    public List<string>? Tips { get; set; }

    [JsonPropertyName("txt_quotes")]
    public string? TxtQuotes { get; set; }
}

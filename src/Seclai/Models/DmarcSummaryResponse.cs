using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class DmarcSummaryResponse
{
    [JsonPropertyName("dispositions")]
    public Dictionary<string, int>? Dispositions { get; set; }

    [JsonPropertyName("failed_messages")]
    public int FailedMessages { get; set; }

    [JsonPropertyName("monitored")]
    public bool? Monitored { get; set; }

    [JsonPropertyName("pass_rate")]
    public double? PassRate { get; set; }

    [JsonPropertyName("passed_messages")]
    public int PassedMessages { get; set; }

    [JsonPropertyName("report_count")]
    public int ReportCount { get; set; }

    [JsonPropertyName("top_failing_sources")]
    public List<DmarcFailingSourceResponse>? TopFailingSources { get; set; }

    [JsonPropertyName("total_messages")]
    public int TotalMessages { get; set; }

    [JsonPropertyName("window_days")]
    public int WindowDays { get; set; }
}

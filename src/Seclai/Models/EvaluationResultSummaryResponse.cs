using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class EvaluationResultSummaryResponse
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("passed")]
    public int Passed { get; set; }

    [JsonPropertyName("failed")]
    public int Failed { get; set; }

    [JsonPropertyName("flagged")]
    public int Flagged { get; set; }

    [JsonPropertyName("error")]
    public int Error { get; set; }

    [JsonPropertyName("average_score")]
    public float? AverageScore { get; set; }
}

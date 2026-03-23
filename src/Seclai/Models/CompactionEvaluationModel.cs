using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class CompactionEvaluationModel
{
    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("verdict")]
    public string? Verdict { get; set; }

    [JsonPropertyName("reasoning")]
    public string? Reasoning { get; set; }
}

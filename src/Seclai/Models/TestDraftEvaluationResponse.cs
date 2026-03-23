using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class TestDraftEvaluationResponse
{
    [JsonPropertyName("passed")]
    public bool Passed { get; set; }

    [JsonPropertyName("score")]
    public float Score { get; set; }

    [JsonPropertyName("explanation")]
    public string? Explanation { get; set; }
}

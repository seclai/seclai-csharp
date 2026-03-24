using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class EvaluationResultResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("criteria_id")]
    public string? CriteriaId { get; set; }

    [JsonPropertyName("passed")]
    public bool Passed { get; set; }

    [JsonPropertyName("score")]
    public float? Score { get; set; }

    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }
}

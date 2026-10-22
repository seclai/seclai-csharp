using System;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class EvaluationResultResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("criteria_id")]
    public string? CriteriaId { get; set; }

    /// <summary><c>pending</c>, <c>passed</c>, <c>failed</c>, <c>skipped</c>, or <c>error</c> when the evaluator returned no verdict.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [Obsolete("The API does not send passed, so this is always false. Read Status.")]
    [JsonPropertyName("passed")]
    public bool Passed { get; set; }

    [JsonPropertyName("score")]
    public float? Score { get; set; }

    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }
}

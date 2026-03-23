using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class EvaluationResultListResponse
{
    [JsonPropertyName("data")]
    public List<EvaluationResultResponse> Data { get; set; } = new();

    [JsonPropertyName("total")]
    public int Total { get; set; }
}

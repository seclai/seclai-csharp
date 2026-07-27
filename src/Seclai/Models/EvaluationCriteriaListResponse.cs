using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>Paginated list of evaluation criteria.</summary>
public sealed class EvaluationCriteriaListResponse
{
    [JsonPropertyName("data")]
    public List<EvaluationCriteriaResponse>? Data { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }
}

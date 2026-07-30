using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>A page of evaluation criteria.</summary>
/// <remarks>
/// The endpoint returns a bare array by default and only emits the canonical
/// <c>{data, pagination}</c> envelope once the caller opts in with
/// <see cref="SeclaiClientOptions.ApiVersion"/> of <c>2026-07-27</c> or later, so
/// <see cref="Pagination"/> is <c>null</c> unless opted in.
/// </remarks>
public sealed class EvaluationCriteriaListResponse
{
    [JsonPropertyName("data")]
    public List<EvaluationCriteriaResponse>? Data { get; set; }

    [JsonPropertyName("pagination")]
    public PaginationResponse? Pagination { get; set; }
}

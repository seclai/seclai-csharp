using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>A page of evaluation results with criteria context.</summary>
/// <remarks>
/// Two endpoints share this shape and populate different halves of it:
/// <list type="bullet">
/// <item><description>
/// <c>GET /agents/{id}/evaluation-results</c> is always paginated and fills
/// <see cref="Total"/>, <see cref="Page"/> and <see cref="Limit"/>.
/// </description></item>
/// <item><description>
/// <c>GET /agents/{id}/runs/{runId}/evaluation-results</c> is version-gated: a
/// bare array by default, and the canonical <c>{data, pagination}</c> envelope
/// once <see cref="SeclaiClientOptions.ApiVersion"/> is <c>2026-07-27</c> or
/// later — in which case the metadata is on <see cref="Pagination"/> and the
/// flat properties stay zero.
/// </description></item>
/// </list>
/// </remarks>
public sealed class EvaluationResultWithCriteriaListResponse
{
    [JsonPropertyName("data")]
    public List<JsonElement>? Data { get; set; }

    /// <summary>Canonical pagination metadata. Null on the flat and legacy shapes.</summary>
    [JsonPropertyName("pagination")]
    public PaginationResponse? Pagination { get; set; }

    /// <summary>Total items on the flat shape. Zero when <see cref="Pagination"/> is set.</summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary>Page number on the flat shape. Zero when <see cref="Pagination"/> is set.</summary>
    [JsonPropertyName("page")]
    public int Page { get; set; }

    /// <summary>Page size on the flat shape. Zero when <see cref="Pagination"/> is set.</summary>
    [JsonPropertyName("limit")]
    public int Limit { get; set; }
}

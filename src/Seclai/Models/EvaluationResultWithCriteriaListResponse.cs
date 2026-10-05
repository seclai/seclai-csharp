using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>A page of evaluation results with criteria context.</summary>
/// <remarks>
/// Returned by the agent-level and the run-level evaluation-result listings. Both are
/// version-gated; the client fills <see cref="Data"/>, <see cref="Total"/>,
/// <see cref="Page"/> and <see cref="Limit"/> from whichever shape arrives. The run-level
/// default shape is a bare array that states no counts, so all three stay zero there.
/// </remarks>
public sealed class EvaluationResultWithCriteriaListResponse : IListPage<JsonElement>
{
    [JsonPropertyName("data")]
    public List<JsonElement>? Data { get; set; }

    /// <summary>Paging metadata, present once <c>Seclai-Version</c> is <c>2026-07-27</c> or later. Null on the default shape.</summary>
    [JsonPropertyName("pagination")]
    public PaginationResponse? Pagination { get; set; }

    /// <summary>Total items. Read from <see cref="Pagination"/> when that arrives.</summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary>Page number. Read from <see cref="Pagination"/> when that arrives.</summary>
    [JsonPropertyName("page")]
    public int Page { get; set; }

    /// <summary>Page size. Read from <see cref="Pagination"/> when that arrives.</summary>
    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    void IListPage<JsonElement>.Fill(List<JsonElement> items, ListShape shape)
    {
        Data = items;
        Pagination = shape.Pagination;
        if (shape.Total is int total) Total = total;
        if (shape.Page is int page) Page = page;
        if (shape.Limit is int limit) Limit = limit;
    }
}

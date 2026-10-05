using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class EvaluationResultListResponse : IListPage<EvaluationResultResponse>
{
    [JsonPropertyName("data")]
    public List<EvaluationResultResponse> Data { get; set; } = new();

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    /// <summary>Paging metadata, present once <c>Seclai-Version</c> is <c>2026-07-27</c> or later. Null on the default shape.</summary>
    [JsonPropertyName("pagination")]
    public PaginationResponse? Pagination { get; set; }

    void IListPage<EvaluationResultResponse>.Fill(List<EvaluationResultResponse> items, ListShape shape)
    {
        Data = items;
        Pagination = shape.Pagination;
        if (shape.Total is int total) Total = total;
        if (shape.Page is int page) Page = page;
        if (shape.Limit is int limit) Limit = limit;
    }
}

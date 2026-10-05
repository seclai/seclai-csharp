using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>``GET /models/playground/experiments`` legacy/default shape; 2026-07-27+ clients get the canonical ``{data, pagination}`` envelope.</summary>
public sealed class ExperimentListResponse : IListPage<ExperimentSummaryResponse>
{
    [JsonPropertyName("experiments")]
    public List<ExperimentSummaryResponse> Experiments { get; set; } = new();

    [JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary>Paging metadata, present once <c>Seclai-Version</c> is <c>2026-07-27</c> or later. Null on the default shape.</summary>
    [JsonPropertyName("pagination")]
    public PaginationResponse? Pagination { get; set; }

    void IListPage<ExperimentSummaryResponse>.Fill(List<ExperimentSummaryResponse> items, ListShape shape)
    {
        Experiments = items;
        Pagination = shape.Pagination;
        if (shape.Total is int total) Total = total;
    }
}

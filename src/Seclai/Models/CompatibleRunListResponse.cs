using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class CompatibleRunListResponse : IListPage<JsonElement>
{
    [JsonPropertyName("data")]
    public List<JsonElement>? Data { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    /// <summary>Paging metadata, present once <c>Seclai-Version</c> is <c>2026-07-27</c> or later. Null on the default shape.</summary>
    [JsonPropertyName("pagination")]
    public PaginationResponse? Pagination { get; set; }

    void IListPage<JsonElement>.Fill(List<JsonElement> items, ListShape shape)
    {
        Data = items;
        Pagination = shape.Pagination;
        if (shape.Total is int total) Total = total;
        if (shape.Page is int page) Page = page;
        if (shape.Limit is int limit) Limit = limit;
    }
}

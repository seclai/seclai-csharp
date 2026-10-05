using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>A page of memory banks.</summary>
/// <remarks>
/// The wire shape is version-gated: <c>{memory_banks, page, limit, total}</c> by default
/// and <c>{data, pagination}</c> once <see cref="SeclaiClientOptions.ApiVersion"/>
/// is <c>2026-07-27</c> or later. <see cref="Data"/>, <see cref="Total"/>,
/// <see cref="Page"/> and <see cref="Limit"/> are filled from whichever arrived.
/// </remarks>
public sealed class MemoryBankListResponse : IListPage<MemoryBankResponse>
{
    private PaginationResponse? _pagination;

    [JsonPropertyName("data")]
    public List<MemoryBankResponse> Data { get; set; } = new();

    /// <summary>The default-shape key. The same list as <see cref="Data"/>.</summary>
    [JsonPropertyName("memory_banks")]
    public List<MemoryBankResponse> MemoryBanks
    {
        get => Data;
        set => Data = value ?? new List<MemoryBankResponse>();
    }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    /// <summary>Canonical pagination metadata. Null on the default shape.</summary>
    [JsonPropertyName("pagination")]
    public PaginationResponse? Pagination
    {
        get => _pagination;
        set
        {
            _pagination = value;
            if (value is null) return;
            Total = value.Total;
            Page = value.Page;
            Limit = value.Limit;
        }
    }

    void IListPage<MemoryBankResponse>.Fill(List<MemoryBankResponse> items, ListShape shape)
    {
        Data = items;
        Pagination = shape.Pagination;
        if (shape.Total is int total) Total = total;
        if (shape.Page is int page) Page = page;
        if (shape.Limit is int limit) Limit = limit;
    }
}

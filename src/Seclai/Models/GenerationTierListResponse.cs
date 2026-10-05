using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>``GET /models/generation-tiers`` legacy/default shape; 2026-07-27+ clients get the canonical ``{data, pagination}`` envelope.</summary>
public sealed class GenerationTierListResponse : IListPage<GenerationTierResponse>
{
    [JsonPropertyName("tiers")]
    public List<GenerationTierResponse> Tiers { get; set; } = new();

    /// <summary>Paging metadata, present once <c>Seclai-Version</c> is <c>2026-07-27</c> or later. Null on the default shape.</summary>
    [JsonPropertyName("pagination")]
    public PaginationResponse? Pagination { get; set; }

    void IListPage<GenerationTierResponse>.Fill(List<GenerationTierResponse> items, ListShape shape)
    {
        Tiers = items;
        Pagination = shape.Pagination;
    }
}

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>A page of agent-email opt-outs plus the total (for pagination).</summary>
public sealed class AgentEmailOptOutListResponse : IListPage<AgentEmailOptOutResponse>
{
    [JsonPropertyName("items")]
    public List<AgentEmailOptOutResponse> Items { get; set; } = new();

    [JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary>Paging metadata, present once <c>Seclai-Version</c> is <c>2026-07-27</c> or later. Null on the default shape.</summary>
    [JsonPropertyName("pagination")]
    public PaginationResponse? Pagination { get; set; }

    void IListPage<AgentEmailOptOutResponse>.Fill(List<AgentEmailOptOutResponse> items, ListShape shape)
    {
        Items = items;
        Pagination = shape.Pagination;
        if (shape.Total is int total) Total = total;
    }
}

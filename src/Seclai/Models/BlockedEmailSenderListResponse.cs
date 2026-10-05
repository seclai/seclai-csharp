using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>A page of blocked senders + the account's auto-block mode.</summary>
public sealed class BlockedEmailSenderListResponse : IListPage<BlockedEmailSenderResponse>
{
    [JsonPropertyName("auto_block_mode")]
    public string AutoBlockMode { get; set; } = string.Empty;

    [JsonPropertyName("items")]
    public List<BlockedEmailSenderResponse> Items { get; set; } = new();

    [JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary>Paging metadata, present once <c>Seclai-Version</c> is <c>2026-07-27</c> or later. Null on the default shape.</summary>
    [JsonPropertyName("pagination")]
    public PaginationResponse? Pagination { get; set; }

    void IListPage<BlockedEmailSenderResponse>.Fill(List<BlockedEmailSenderResponse> items, ListShape shape)
    {
        Items = items;
        Pagination = shape.Pagination;
        if (shape.Total is int total) Total = total;
    }
}

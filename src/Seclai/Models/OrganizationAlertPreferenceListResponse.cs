using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class OrganizationAlertPreferenceListResponse : IListPage<JsonElement>
{
    [JsonPropertyName("preferences")]
    public List<JsonElement>? Preferences { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary>Paging metadata, present once <c>Seclai-Version</c> is <c>2026-07-27</c> or later. Null on the default shape.</summary>
    [JsonPropertyName("pagination")]
    public PaginationResponse? Pagination { get; set; }

    void IListPage<JsonElement>.Fill(List<JsonElement> items, ListShape shape)
    {
        Preferences = items;
        Pagination = shape.Pagination;
        if (shape.Total is int total) Total = total;
    }
}

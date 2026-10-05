using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>A page of model lifecycle alerts.</summary>
/// <remarks>
/// The top-level key is version-gated. By default the alerts arrive under
/// <c>alerts</c> alongside <c>total</c>; once
/// <see cref="SeclaiClientOptions.ApiVersion"/> is <c>2026-07-27</c> or later the
/// endpoint returns the canonical <c>{data, pagination}</c> envelope instead.
/// Use <see cref="Items"/> to read whichever arrived.
/// </remarks>
public sealed class ModelAlertListResponse : IListPage<ModelAlertResponse>
{
    /// <summary>Legacy key. Empty once the canonical envelope is in use.</summary>
    [JsonPropertyName("alerts")]
    public List<ModelAlertResponse> Alerts { get; set; } = new();

    /// <summary>Canonical key. Empty on the legacy shape.</summary>
    [JsonPropertyName("data")]
    public List<ModelAlertResponse> Data { get; set; } = new();

    /// <summary>Paging metadata, present once <c>Seclai-Version</c> is <c>2026-07-27</c> or later. Null on the default shape.</summary>
    [JsonPropertyName("pagination")]
    public PaginationResponse? Pagination { get; set; }

    /// <summary>Total alerts matching the query. Read from <see cref="Pagination"/> when that arrives.</summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary>The alerts, from whichever key the response used.</summary>
    [JsonIgnore]
    public List<ModelAlertResponse> Items => Data.Count > 0 ? Data : Alerts;

    void IListPage<ModelAlertResponse>.Fill(List<ModelAlertResponse> items, ListShape shape)
    {
        if (shape.FromData)
        {
            Data = items;
        }
        else
        {
            Alerts = items;
        }
        Pagination = shape.Pagination;
        if (shape.Total is int total) Total = total;
    }
}

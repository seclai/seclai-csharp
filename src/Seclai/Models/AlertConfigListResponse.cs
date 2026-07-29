using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>A page of alert configurations.</summary>
/// <remarks>
/// The top-level key is version-gated. By default the configurations arrive
/// under <c>configs</c> alongside <c>total</c>; once
/// <see cref="SeclaiClientOptions.ApiVersion"/> is <c>2026-07-27</c> or later the
/// endpoint returns the canonical <c>{data, pagination}</c> envelope instead.
/// Use <see cref="Items"/> to read whichever arrived.
/// </remarks>
public sealed class AlertConfigListResponse
{
    /// <summary>Legacy key. Empty once the canonical envelope is in use.</summary>
    [JsonPropertyName("configs")]
    public List<AlertConfigResponse> Configs { get; set; } = new();

    /// <summary>Canonical key. Empty on the legacy shape.</summary>
    [JsonPropertyName("data")]
    public List<AlertConfigResponse> Data { get; set; } = new();

    /// <summary>Canonical pagination metadata. Null on the legacy shape.</summary>
    [JsonPropertyName("pagination")]
    public PaginationResponse? Pagination { get; set; }

    /// <summary>Total configurations on the legacy shape. Zero once <see cref="Pagination"/> is set.</summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary>The configurations, from whichever key the response used.</summary>
    [JsonIgnore]
    public List<AlertConfigResponse> Items => Data.Count > 0 ? Data : Configs;
}

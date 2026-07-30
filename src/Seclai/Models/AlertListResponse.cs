using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>A page of alerts. Always the canonical <c>{data, pagination}</c> envelope.</summary>
public sealed class AlertListResponse
{
    [JsonPropertyName("data")]
    public List<AlertResponse> Data { get; set; } = new();

    [JsonPropertyName("pagination")]
    public PaginationResponse? Pagination { get; set; }
}

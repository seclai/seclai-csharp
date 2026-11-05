using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class AgentListResponse
{
    private PaginationResponse? _pagination;

    [JsonPropertyName("data")]
    public List<AgentSummaryResponse> Data { get; set; } = new();

    /// <summary>Total items across all pages. Filled from <see cref="Pagination"/>.</summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary>Paging metadata. Setting it also fills <see cref="Total"/>, which the response does not carry on its own.</summary>
    [JsonPropertyName("pagination")]
    public PaginationResponse? Pagination
    {
        get => _pagination;
        set
        {
            _pagination = value;
            if (value is not null) Total = value.Total;
        }
    }
}

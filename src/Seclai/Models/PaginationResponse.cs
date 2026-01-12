using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class PaginationResponse
{
    [JsonPropertyName("has_next")]
    public bool HasNext { get; set; }

    [JsonPropertyName("has_prev")]
    public bool HasPrev { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pages")]
    public int Pages { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }
}

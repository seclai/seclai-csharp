using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class SourceListResponse
{
    [JsonPropertyName("data")]
    public List<SourceResponse> Data { get; set; } = new();

    [JsonPropertyName("pagination")]
    public PaginationResponse Pagination { get; set; } = new();
}

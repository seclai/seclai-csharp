using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>Response model for a paginated per-item indexing status list.</summary>
public sealed class SourceContentStatusListResponse
{
    [JsonPropertyName("data")]
    public List<SourceContentStatusResponse> Data { get; set; } = new();

    [JsonPropertyName("pagination")]
    public PaginationResponse? Pagination { get; set; }
}

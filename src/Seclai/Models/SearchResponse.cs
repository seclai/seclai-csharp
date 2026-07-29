using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>Ranked results, NOT a paginated collection — the ``{results}`` shape is a deliberate carve-out matching the MCP ``search_resources`` tool.</summary>
public sealed class SearchResponse
{
    [JsonPropertyName("results")]
    public List<SearchResultResponse> Results { get; set; } = new();
}

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>Ranked results, NOT a paginated collection — the ``{results}`` shape is a deliberate carve-out matching the MCP ``search_docs`` tool.</summary>
public sealed class DocsSearchResponse
{
    [JsonPropertyName("results")]
    public List<DocsSearchResultResponse> Results { get; set; } = new();
}

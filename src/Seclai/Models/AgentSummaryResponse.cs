using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class AgentSummaryResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("is_public")]
    public bool IsPublic { get; set; }

    [JsonPropertyName("solution_id")]
    public string? SolutionId { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }

    /// <summary>
    /// One entry per item dropped or substituted during an <c>agent_definition</c> import.
    /// Present only on endpoints that accept <c>agent_definition</c> (returned by
    /// <c>POST /agents</c> and <c>PUT /agents/{id}</c>); <c>null</c> on other calls;
    /// empty list when the import had no skips.
    /// </summary>
    [JsonPropertyName("import_warnings")]
    public List<ImportSkipResponse>? ImportWarnings { get; set; }
}

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class CreateAgentRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("is_public")]
    public bool? IsPublic { get; set; }

    [JsonPropertyName("solution_id")]
    public string? SolutionId { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Optional payload in the same format produced by <c>GET /agents/{id}/export</c>.
    /// When provided, replaces any template-derived workflow and pre-fills metadata/trigger
    /// fields the request does not specify explicitly. Validation errors include 1-indexed
    /// line/column references against a canonical pretty-printed echo of the supplied payload
    /// (returned as <see cref="AgentDefinitionImportErrorResponse"/> on HTTP 422).
    /// </summary>
    [JsonPropertyName("agent_definition")]
    public Dictionary<string, JsonElement>? AgentDefinition { get; set; }

    /// <summary>
    /// Optional UUID-substitution map applied to the imported workflow before save.
    /// Keys are source-account UUIDs (as returned by <c>POST /agents/preview-import</c>'s
    /// <c>unresolved_refs</c>); values are the target-account UUIDs to substitute. Used to
    /// relink knowledge bases, memory banks, source connections, and sub-agents on
    /// cross-account imports.
    /// </summary>
    [JsonPropertyName("entity_remap")]
    public Dictionary<string, string>? EntityRemap { get; set; }
}

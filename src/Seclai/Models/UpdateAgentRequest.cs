using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class UpdateAgentRequest
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
    /// When provided, the agent's workflow is replaced from <c>agent.definition</c> and
    /// metadata fields the request does not set explicitly are taken from the payload.
    /// Unlike <c>POST /agents</c>, update does NOT apply <c>alert_configs</c>,
    /// <c>evaluation_criteria</c>, <c>governance_policies</c>, <c>schedules</c>, or
    /// solution links from the imported file. Validation errors are returned as
    /// <see cref="AgentDefinitionImportErrorResponse"/> on HTTP 422.
    /// </summary>
    [JsonPropertyName("agent_definition")]
    public Dictionary<string, JsonElement>? AgentDefinition { get; set; }

    /// <summary>
    /// Optional UUID-substitution map applied to the imported workflow before save
    /// (same shape as on <c>POST /agents</c>).
    /// </summary>
    [JsonPropertyName("entity_remap")]
    public Dictionary<string, string>? EntityRemap { get; set; }
}

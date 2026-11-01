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

    /// <summary>
    /// The input attachments the trigger accepts. Left null, the field is omitted: the
    /// <see cref="AgentDefinition"/> trigger's declaration is kept when the export carries one;
    /// otherwise a <c>dynamic_input</c> or <c>email_received</c> trigger accepts any files.
    /// </summary>
    [JsonPropertyName("accepted_attachments")]
    public List<AcceptedAttachmentSlot>? AcceptedAttachments { get; set; }

    /// <summary>Agent gallery entry to start from: a <see cref="SeclaiClient.ListAgentExamplesAsync"/> entry's <c>key</c>, or <c>blank</c>.</summary>
    [JsonPropertyName("agent_template")]
    public string? AgentTemplate { get; set; }

    /// <summary>For an <see cref="AgentTemplate"/>: the existing resource to use for each of the template's resource slots, as <c>{slot: resource_id}</c>. <see cref="SeclaiClient.ListAgentExamplesAsync"/> lists each template's slots.</summary>
    [JsonPropertyName("resource_bindings")]
    public Dictionary<string, string>? ResourceBindings { get; set; }

    /// <summary>For an <see cref="AgentTemplate"/>: create each creatable resource slot left out of <see cref="ResourceBindings"/>, and group the agent with them in a new solution. When false (the server default), an unbound slot the template needs is an error. The created ids come back in <see cref="AgentSummaryResponse.CreatedResources"/>.</summary>
    [JsonPropertyName("create_missing_resources")]
    public bool? CreateMissingResources { get; set; }
}

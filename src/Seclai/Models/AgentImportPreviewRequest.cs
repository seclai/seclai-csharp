using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>
/// Dry-run import request body for <c>POST /agents/preview-import</c>.
/// Carries the same payload shape produced by <see cref="AgentExportResponse"/>.
/// </summary>
public sealed class AgentImportPreviewRequest
{
    /// <summary>The agent_definition payload to validate. Same shape as <c>GET /agents/{id}/export</c>.</summary>
    [JsonPropertyName("agent_definition")]
    public Dictionary<string, JsonElement>? AgentDefinition { get; set; }
}

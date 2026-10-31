using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>
/// A page of the agent gallery. Pass an entry's <c>key</c> as <see cref="CreateAgentRequest.AgentTemplate"/>,
/// with <see cref="CreateAgentRequest.ResourceBindings"/> for the resources it needs.
/// </summary>
/// <remarks>The API does not declare the entry shape, so each entry is the JSON object as sent.</remarks>
public sealed class AgentExampleListResponse
{
    [JsonPropertyName("data")]
    public List<JsonElement> Data { get; set; } = new();

    [JsonPropertyName("pagination")]
    public PaginationResponse? Pagination { get; set; }
}

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>The input attachments a trigger accepts, as stored.</summary>
public sealed class AcceptedAttachmentsResponse
{
    [JsonPropertyName("agent_id")]
    public string AgentId { get; set; } = string.Empty;

    [JsonPropertyName("trigger_id")]
    public string TriggerId { get; set; } = string.Empty;

    [JsonPropertyName("accepted_attachments")]
    public List<AcceptedAttachmentSlot> AcceptedAttachments { get; set; } = new();
}

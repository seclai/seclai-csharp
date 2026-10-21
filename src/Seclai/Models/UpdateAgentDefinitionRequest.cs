using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class UpdateAgentDefinitionRequest
{
    [JsonPropertyName("definition")]
    public Dictionary<string, JsonElement>? Definition { get; set; }

    [JsonPropertyName("expected_change_id")]
    public string? ExpectedChangeId { get; set; }

    /// <summary>
    /// When given, declared on the main branch's file-taking triggers in the same save, so the steps may
    /// read its slots as <c>{{agent.attachments.&lt;slot&gt;}}</c>. Left null, the field is omitted and the
    /// triggers keep theirs.
    /// </summary>
    [JsonPropertyName("accepted_attachments")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<AcceptedAttachmentSlot>? AcceptedAttachments { get; set; }
}

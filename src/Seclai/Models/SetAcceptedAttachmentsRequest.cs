using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>Replaces the input attachments a trigger accepts.</summary>
public sealed class SetAcceptedAttachmentsRequest
{
    /// <summary>
    /// The input attachments the trigger accepts. <c>null</c> is sent as-is and accepts any files
    /// (one attachment named <c>files</c>). Only <c>dynamic_input</c> and <c>email_received</c>
    /// triggers take files.
    /// </summary>
    [JsonPropertyName("accepted_attachments")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public List<AcceptedAttachmentSlot>? AcceptedAttachments { get; set; }
}

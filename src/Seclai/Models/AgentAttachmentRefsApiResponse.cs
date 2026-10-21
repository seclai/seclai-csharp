using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>
/// Static attachment-reference contract for an agent — what files (if any) its
/// templates expect on a run, so uploads can be staged before starting the run.
/// </summary>
public sealed class AgentAttachmentRefsApiResponse
{
    /// <summary>
    /// Aggregated selector summary across all consumer steps. Exact names must each
    /// appear in the upload batch; indexes_max + 1 is the minimum file count; every
    /// pattern glob must match at least one upload.
    /// </summary>
    [JsonPropertyName("agent")]
    public AttachmentRefsSourceApiSummary? Agent { get; set; }

    /// <summary>
    /// When false the agent's definition does NOT reference any uploaded attachments.
    /// When true the <see cref="Agent"/> block lists the specific selectors a run-time
    /// batch must satisfy.
    /// </summary>
    [JsonPropertyName("requires_uploads")]
    public bool RequiresUploads { get; set; }

    /// <summary>
    /// The attachments the agent's trigger declares. A run's files are each assigned to one; a file
    /// none accepts, or an attachment with fewer files than its <c>min</c>, rejects the run with HTTP 400.
    /// Steps reference one as <c>{{agent.attachments.&lt;name&gt;}}</c>.
    /// </summary>
    [JsonPropertyName("accepted_attachments")]
    public List<AcceptedAttachmentSlot>? AcceptedAttachments { get; set; }
}

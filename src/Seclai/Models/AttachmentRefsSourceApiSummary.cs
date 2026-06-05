using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>Per-source attachment-reference summary within an <see cref="AgentAttachmentRefsApiResponse"/>.</summary>
public sealed class AttachmentRefsSourceApiSummary
{
    /// <summary>Filenames that must each appear in the upload batch.</summary>
    [JsonPropertyName("exact_names")]
    public List<string>? ExactNames { get; set; }

    /// <summary>Highest referenced upload index; the batch must contain at least this many + 1 files.</summary>
    [JsonPropertyName("indexes_max")]
    public int? IndexesMax { get; set; }

    /// <summary>Attachment kinds referenced by the agent's templates.</summary>
    [JsonPropertyName("kinds")]
    public List<string>? Kinds { get; set; }

    /// <summary>fnmatch glob patterns that must each match at least one uploaded filename.</summary>
    [JsonPropertyName("patterns")]
    public List<string>? Patterns { get; set; }
}

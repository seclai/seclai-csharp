using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>
/// One input attachment a trigger accepts. A run's files are each assigned to one declared
/// attachment before any step runs; steps reference one as <c>{{agent.attachments.&lt;name&gt;}}</c>.
/// </summary>
public sealed class AcceptedAttachmentSlot
{
    /// <summary>Lowercase letters, digits and underscores, starting with a letter. Steps reference the files as <c>{{agent.attachments.&lt;name&gt;}}</c>.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Shown to people.</summary>
    [JsonPropertyName("label")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Label { get; set; }

    /// <summary>Accepted types, e.g. <c>application/pdf</c> or <c>image/*</c>; empty accepts any type.</summary>
    [JsonPropertyName("content_types")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? ContentTypes { get; set; }

    /// <summary>Fewest files a run must include; fewer refuses the run, except on an email trigger set to run anyway, which lists the shortfall.</summary>
    [JsonPropertyName("min")]
    public int Min { get; set; }

    /// <summary>Most files a run may include; null is no limit.</summary>
    [JsonPropertyName("max")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Max { get; set; }

    /// <summary>Filename glob a file must also match, ignoring case.</summary>
    [JsonPropertyName("name_pattern")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? NamePattern { get; set; }
}

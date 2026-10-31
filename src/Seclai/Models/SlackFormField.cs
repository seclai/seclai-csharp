using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>
/// One field of a SLACK_COMMAND trigger's form, in <see cref="SetSlackTriggerConfigRequest.Form"/> and
/// <see cref="SlackTriggerConfigResponse.Form"/>. The value a person enters reaches the run as
/// <c>{{metadata.&lt;key&gt;}}</c>.
/// </summary>
public sealed class SlackFormField
{
    /// <summary>The metadata key the value is stored under.</summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    /// <summary>The label shown in Slack.</summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// One of <c>short_text</c>, <c>long_text</c>, <c>number</c>, <c>single_choice</c>, <c>multi_choice</c>,
    /// <c>yes_no</c>, <c>date</c>, <c>slack_user</c> or <c>slack_channel</c>.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>Whether the field must be filled in. Defaults to false.</summary>
    [JsonPropertyName("required")]
    public bool Required { get; set; }

    /// <summary>Help text shown under the field.</summary>
    [JsonPropertyName("hint")]
    public string? Hint { get; set; }

    /// <summary>The options of a <c>single_choice</c> or <c>multi_choice</c> field; distinct, and only for those types.</summary>
    [JsonPropertyName("choices")]
    public List<string>? Choices { get; set; }

    /// <summary>The longest value a text field accepts.</summary>
    [JsonPropertyName("max_length")]
    public int? MaxLength { get; set; }

    /// <summary>Properties this client does not model yet, kept so a field read back can be sent again unchanged.</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}

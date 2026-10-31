using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>
/// Slack connection, form, channels, visibility and conversation settings for an agent's
/// SLACK_COMMAND trigger, which people run from Slack with <c>/seclai</c> or the message shortcut.
/// </summary>
/// <remarks>
/// A property left <c>null</c> is omitted from the request and the server leaves that setting unchanged.
/// The server refuses a request that leaves the trigger without a Slack (Bot) connection.
/// </remarks>
public sealed class SetSlackTriggerConfigRequest
{
    /// <summary>The account's connected Slack (Bot) MCP client.</summary>
    [JsonPropertyName("mcp_client_id")]
    public string? McpClientId { get; set; }

    /// <summary>
    /// Form fields, each <c>{key, label, type, required, hint, choices, max_length}</c>. <c>type</c> is one of
    /// <c>short_text</c>, <c>long_text</c>, <c>number</c>, <c>single_choice</c>, <c>multi_choice</c>,
    /// <c>yes_no</c>, <c>date</c>, <c>slack_user</c> or <c>slack_channel</c>. Each value reaches the run as
    /// <c>{{metadata.&lt;key&gt;}}</c>. An empty list removes the form; null leaves it unchanged.
    /// </summary>
    [JsonPropertyName("form")]
    public List<Dictionary<string, JsonElement>>? Form { get; set; }

    /// <summary>The trigger's input template. Each <c>{{metadata.&lt;key&gt;}}</c> in it must be a form field key or a platform key; it is checked together with <see cref="Form"/>.</summary>
    [JsonPropertyName("input")]
    public string? Input { get; set; }

    /// <summary><c>selected_channels</c> (see <see cref="ChannelIds"/>) or <c>bot_channels</c>.</summary>
    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    /// <summary>The channels the trigger runs in when <see cref="Scope"/> is <c>selected_channels</c>. An empty list removes them; null leaves them unchanged.</summary>
    [JsonPropertyName("channel_ids")]
    public List<string>? ChannelIds { get; set; }

    /// <summary>Whether the trigger can be run in direct messages with the app. Needs the <c>im:read</c> scope.</summary>
    [JsonPropertyName("allow_dms")]
    public bool? AllowDms { get; set; }

    /// <summary><c>everywhere</c> in scope, or <c>channel_opt_in</c>.</summary>
    [JsonPropertyName("activation")]
    public string? Activation { get; set; }

    /// <summary><c>channel</c>, or <c>private</c> (replies in the person's DM).</summary>
    [JsonPropertyName("visibility")]
    public string? Visibility { get; set; }

    /// <summary>Whether a run may start without the form when no required field is missing. Off by default.</summary>
    [JsonPropertyName("start_without_form")]
    public bool? StartWithoutForm { get; set; }

    /// <summary>Whether a reply in the run's thread continues it as a new turn. Off by default, and refused for a background trigger.</summary>
    [JsonPropertyName("turns_enabled")]
    public bool? TurnsEnabled { get; set; }

    /// <summary>Whether anyone in the channel may continue the thread, rather than only the person who started its first run.</summary>
    [JsonPropertyName("turns_anyone")]
    public bool? TurnsAnyone { get; set; }

    /// <summary>Cap on turns per thread. A null here is omitted, so it leaves the cap unchanged; to remove the cap, use <see cref="SeclaiClient.ClearSlackTriggerMaxTurnsAsync"/>.</summary>
    [JsonPropertyName("max_turns")]
    public int? MaxTurns { get; set; }

    /// <summary>Whether the run's metadata carries the invoking person's email. Needs the <c>users:read.email</c> scope.</summary>
    [JsonPropertyName("include_invoker_email")]
    public bool? IncludeInvokerEmail { get; set; }

    /// <summary>Whether a run that fails after its answer was posted adds a note in the thread.</summary>
    [JsonPropertyName("notify_failure_after_answer")]
    public bool? NotifyFailureAfterAnswer { get; set; }
}

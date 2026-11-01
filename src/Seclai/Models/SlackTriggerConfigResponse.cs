using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>The settings of an agent's SLACK_COMMAND trigger, as returned by <see cref="SeclaiClient.SetSlackTriggerConfigAsync"/>.</summary>
public sealed class SlackTriggerConfigResponse
{
    [JsonPropertyName("trigger_id")]
    public string TriggerId { get; set; } = string.Empty;

    [JsonPropertyName("agent_id")]
    public string AgentId { get; set; } = string.Empty;

    /// <summary>The connected Slack (Bot) MCP client, or null when none is set.</summary>
    [JsonPropertyName("mcp_client_id")]
    public string? McpClientId { get; set; }

    /// <summary>The Slack workspace of <see cref="McpClientId"/>, or null when none is set.</summary>
    [JsonPropertyName("slack_team_id")]
    public string? SlackTeamId { get; set; }

    [JsonPropertyName("form")]
    public List<SlackFormField> Form { get; set; } = new();

    /// <summary><c>selected_channels</c> or <c>bot_channels</c>.</summary>
    [JsonPropertyName("scope")]
    public string Scope { get; set; } = string.Empty;

    [JsonPropertyName("channel_ids")]
    public List<string> ChannelIds { get; set; } = new();

    [JsonPropertyName("allow_dms")]
    public bool AllowDms { get; set; }

    /// <summary><c>everywhere</c> or <c>channel_opt_in</c>.</summary>
    [JsonPropertyName("activation")]
    public string Activation { get; set; } = string.Empty;

    /// <summary><c>channel</c> or <c>private</c>.</summary>
    [JsonPropertyName("visibility")]
    public string Visibility { get; set; } = string.Empty;

    [JsonPropertyName("start_without_form")]
    public bool StartWithoutForm { get; set; }

    [JsonPropertyName("turns_enabled")]
    public bool TurnsEnabled { get; set; }

    [JsonPropertyName("turns_anyone")]
    public bool TurnsAnyone { get; set; }

    [JsonPropertyName("max_turns")]
    public int? MaxTurns { get; set; }

    [JsonPropertyName("include_invoker_email")]
    public bool IncludeInvokerEmail { get; set; }

    [JsonPropertyName("notify_failure_after_answer")]
    public bool NotifyFailureAfterAnswer { get; set; }

    /// <summary>Scopes the Slack connection lacks for optional features.</summary>
    [JsonPropertyName("missing_scopes")]
    public List<string> MissingScopes { get; set; } = new();
}

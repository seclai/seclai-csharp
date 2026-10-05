using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class AgentUsingCloudDriveResponse
{
    /// <summary>Agent identifier.</summary>
    [JsonPropertyName("agent_id")]
    public string AgentId { get; set; } = string.Empty;

    /// <summary>Agent name.</summary>
    [JsonPropertyName("agent_name")]
    public string AgentName { get; set; } = string.Empty;

    /// <summary>File-change trigger types bound to this drive.</summary>
    [JsonPropertyName("trigger_types")]
    public List<string> TriggerTypes { get; set; } = new();

    /// <summary>Uses a prompt_call cloud-drive tool.</summary>
    [JsonPropertyName("via_prompt_tool")]
    public bool ViaPromptTool { get; set; }

    /// <summary>Uses a list/read/write cloud-drive step.</summary>
    [JsonPropertyName("via_step")]
    public bool ViaStep { get; set; }
}

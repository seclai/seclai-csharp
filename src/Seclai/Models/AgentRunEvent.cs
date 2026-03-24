namespace Seclai.Models;

/// <summary>
/// Represents a single event from an SSE agent run stream.
/// </summary>
public sealed class AgentRunEvent
{
    /// <summary>The SSE event type (e.g. "init", "update", "done").</summary>
    public string? Event { get; set; }

    /// <summary>The raw JSON data payload.</summary>
    public string? Data { get; set; }

    /// <summary>The parsed AgentRunResponse if the data could be decoded, otherwise null.</summary>
    public AgentRunResponse? Run { get; set; }
}

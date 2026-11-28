namespace Seclai.Models;

/// <summary>
/// Represents a single event from an SSE agent run stream.
/// </summary>
public sealed class AgentRunEvent
{
    /// <summary>The SSE event type (e.g. "init", "stream_token", "result", "done").</summary>
    public string? Event { get; set; }

    /// <summary>The raw JSON data payload.</summary>
    public string? Data { get; set; }

    /// <summary>
    /// The run read from the event's data: the snapshot on <c>init</c>, <c>result</c> and <c>done</c>; on a
    /// <c>status</c> event, only <see cref="AgentRunResponse.RunId"/> and <see cref="AgentRunResponse.Status"/>; on
    /// <c>error</c> and <c>timeout</c>, only <see cref="AgentRunResponse.RunId"/>, the run to poll. Null when the
    /// data cannot be read as a run, and on events that carry no run, such as <c>stream_token</c>,
    /// <c>stream_reasoning</c>, <c>stream_commentary</c>, <c>step_progress</c> and <c>tool_call_*</c>, whose payload
    /// is in <see cref="Data"/>.
    /// </summary>
    public AgentRunResponse? Run { get; set; }
}

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
    /// The run carried by a run snapshot (<c>init</c>, <c>update</c>, <c>result</c> or <c>done</c>) or a
    /// <c>status</c> event (only <see cref="AgentRunResponse.RunId"/> and <see cref="AgentRunResponse.Status"/>
    /// set), or null when its data cannot be read as a run. Null for every other event, such as
    /// <c>stream_token</c>, <c>stream_reasoning</c>, <c>stream_commentary</c>, <c>error</c> and <c>timeout</c>,
    /// whose payload is in <see cref="Data"/>.
    /// </summary>
    public AgentRunResponse? Run { get; set; }
}

using System;
using Seclai.Models;

namespace Seclai.Exceptions;

/// <summary>
/// Thrown when an agent run helper gives up before the run finished: an SSE streaming run times out, its stream
/// ends without a <c>done</c> event or breaks mid-stream, or a polled run's own timeout elapses.
/// </summary>
public sealed class StreamingException : Exception
{
    /// <summary>Creates a <see cref="StreamingException"/> with the given message.</summary>
    public StreamingException(string message)
        : base(message)
    {
    }

    /// <summary>Creates a <see cref="StreamingException"/> with a message and inner exception.</summary>
    public StreamingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Creates a <see cref="StreamingException"/> carrying the latest snapshot of the unfinished run.</summary>
    public StreamingException(string message, Exception? innerException, AgentRunResponse? run)
        : base(message, innerException)
    {
        Run = run;
    }

    /// <summary>
    /// The latest snapshot of the run, which had not finished, or null if none arrived. For a streaming run it is
    /// the <c>result</c> snapshot if that event arrived, else the <c>init</c> one; for a polled run, the last poll.
    /// When <see cref="AgentRunResponse.ResultReportedAt"/> is set, <see cref="AgentRunResponse.Output"/> is the
    /// answer even though <see cref="AgentRunResponse.Status"/> is not terminal.
    /// </summary>
    public AgentRunResponse? Run { get; }
}

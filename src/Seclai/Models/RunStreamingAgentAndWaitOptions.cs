using System;

namespace Seclai.Models;

/// <summary>Options for <see cref="SeclaiClient.RunStreamingAgentAndWaitWithOptionsAsync(string, AgentRunStreamRequest, RunStreamingAgentAndWaitOptions, System.Threading.CancellationToken)"/>.</summary>
/// <remarks>
/// A <c>default</c> value waits exactly as <see cref="SeclaiClient.RunStreamingAgentAndWaitAsync"/> does with its defaults.
/// </remarks>
public struct RunStreamingAgentAndWaitOptions
{
    /// <summary>
    /// Maximum time to wait (default: 60 s). When it elapses before the run finished, a
    /// <see cref="Seclai.Exceptions.StreamingException"/> is thrown carrying the latest snapshot in
    /// <see cref="Seclai.Exceptions.StreamingException.Run"/>.
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Return the <c>result</c> snapshot as soon as that event arrives (<see cref="AgentRunResponse.ResultReportedAt"/>
    /// is set and <see cref="AgentRunResponse.Output"/> is the answer), instead of waiting for <c>done</c>.
    /// <see cref="AgentRunResponse.Status"/> may then still be <c>processing</c>, and the run can still fail after it,
    /// so check <see cref="AgentRunResponse.ResultReportedAt"/>, not <see cref="AgentRunResponse.Status"/>.
    /// </summary>
    public bool ReturnOnResult { get; set; }

    /// <summary>
    /// Never return a run that has not finished. When the stream ends without a <c>done</c> event (or a <c>result</c>
    /// one with <see cref="ReturnOnResult"/>), or breaks with an <see cref="System.IO.IOException"/> or
    /// <see cref="System.Net.Http.HttpRequestException"/>, throw <see cref="Seclai.Exceptions.StreamingException"/>
    /// carrying the latest snapshot in <see cref="Seclai.Exceptions.StreamingException.Run"/>, instead of returning
    /// that snapshot or letting the transport exception through. Cancelling the caller's token then throws
    /// <see cref="System.OperationCanceledException"/> rather than <see cref="Seclai.Exceptions.StreamingException"/>.
    /// This becomes the default in the next major version.
    /// </summary>
    public bool ThrowOnIncomplete { get; set; }
}

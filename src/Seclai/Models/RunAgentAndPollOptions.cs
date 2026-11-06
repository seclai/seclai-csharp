using System;

namespace Seclai.Models;

/// <summary>Options for <see cref="SeclaiClient.RunAgentAndPollAsync(string, AgentRunRequest, RunAgentAndPollOptions, System.Threading.CancellationToken)"/>.</summary>
/// <remarks>
/// A struct rather than a class so that a bare <c>null</c> argument cannot convert to it and binds to the method's
/// original overload. A <c>default</c> value polls exactly as the original overload does with its defaults.
/// </remarks>
public struct RunAgentAndPollOptions
{
    /// <summary>Time between polls (default: 2 s).</summary>
    public TimeSpan? PollInterval { get; set; }

    /// <summary>
    /// Maximum time to wait for an unfinished run; null waits until the run ends. The request that starts the run is
    /// not bounded by it, since the server may already have created the run: as in 1.7.0, the <c>HttpClient</c>
    /// timeout (<see cref="SeclaiClientOptions.Timeout"/>) and the caller's token bound it. Nor is the fetch of step
    /// outputs for a run that ended at once.
    /// When it elapses before the run finished, <see cref="System.OperationCanceledException"/> is thrown, or, with
    /// <see cref="ThrowOnIncomplete"/>, <see cref="Seclai.Exceptions.StreamingException"/>.
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Whether each poll asks for step outputs. The response that starts the run carries none, so when that response
    /// would end the poll, the run is fetched once more with them.
    /// </summary>
    public bool IncludeStepOutputs { get; set; }

    /// <summary>
    /// Return as soon as the run reports its result (<see cref="AgentRunResponse.ResultReportedAt"/> is set and
    /// <see cref="AgentRunResponse.Output"/> is the answer), instead of waiting for the remaining steps.
    /// <see cref="AgentRunResponse.Status"/> may then still be <c>processing</c>, and the run can still fail after it,
    /// so check <see cref="AgentRunResponse.ResultReportedAt"/>, not <see cref="AgentRunResponse.Status"/>.
    /// </summary>
    public bool ReturnOnResult { get; set; }

    /// <summary>
    /// When <see cref="Timeout"/> elapses, throw <see cref="Seclai.Exceptions.StreamingException"/> carrying the last
    /// polled snapshot in <see cref="Seclai.Exceptions.StreamingException.Run"/>, instead of
    /// <see cref="System.OperationCanceledException"/>. Cancelling the caller's token still throws
    /// <see cref="System.OperationCanceledException"/>. This becomes the default in the next major version.
    /// </summary>
    public bool ThrowOnIncomplete { get; set; }
}

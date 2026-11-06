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
    /// Maximum time to wait; null waits until the run ends. If it elapses after the run reported its result
    /// (<see cref="AgentRunResponse.ResultReportedAt"/> is set), the last snapshot is returned instead of throwing.
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>Whether each poll asks for step outputs.</summary>
    public bool IncludeStepOutputs { get; set; }

    /// <summary>
    /// Return as soon as the run reports its result (<see cref="AgentRunResponse.ResultReportedAt"/> is set and
    /// <see cref="AgentRunResponse.Output"/> is the answer), instead of waiting for the remaining steps.
    /// <see cref="AgentRunResponse.Status"/> may then still be <c>processing</c>, and the run can still fail after it.
    /// </summary>
    public bool ReturnOnResult { get; set; }
}

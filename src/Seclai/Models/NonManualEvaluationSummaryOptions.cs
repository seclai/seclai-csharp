namespace Seclai.Models;

/// <summary>Scope and period for <see cref="SeclaiClient.GetNonManualEvaluationSummaryAsync(NonManualEvaluationSummaryOptions, System.Threading.CancellationToken)"/>.</summary>
/// <remarks>A struct rather than a class so that a bare <c>null</c> argument cannot convert to it and binds to the method's original overload.</remarks>
public struct NonManualEvaluationSummaryOptions
{
    /// <summary>Scope the summary to a single agent. Leave null for account-wide.</summary>
    public string? AgentId { get; set; }

    /// <summary>Days back to cover, ending today (server default 30). Ignored when <see cref="StartDate"/> is set.</summary>
    public int? Days { get; set; }

    /// <summary>Start of the period, as an ISO 8601 date. Overrides <see cref="Days"/>; without <see cref="EndDate"/> the period runs to today.</summary>
    public string? StartDate { get; set; }

    /// <summary>End of the period, as an ISO 8601 date. Ignored unless <see cref="StartDate"/> is also set.</summary>
    public string? EndDate { get; set; }
}

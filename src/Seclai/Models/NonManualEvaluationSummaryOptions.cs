namespace Seclai.Models;

/// <summary>Scope and period for <see cref="SeclaiClient.GetNonManualEvaluationSummaryAsync(NonManualEvaluationSummaryOptions, System.Threading.CancellationToken)"/>.</summary>
public sealed class NonManualEvaluationSummaryOptions
{
    /// <summary>Scope the summary to a single agent. Leave null for account-wide.</summary>
    public string? AgentId { get; set; }

    /// <summary>Days back to cover (server default 30).</summary>
    public int? Days { get; set; }

    /// <summary>Start of the period, as an ISO 8601 date.</summary>
    public string? StartDate { get; set; }

    /// <summary>End of the period, as an ISO 8601 date.</summary>
    public string? EndDate { get; set; }
}

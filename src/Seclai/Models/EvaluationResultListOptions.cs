using System;

namespace Seclai.Models;

/// <summary>Filters and paging for <see cref="SeclaiClient.ListEvaluationResultsAsync(string, EvaluationResultListOptions, System.Threading.CancellationToken)"/>.</summary>
/// <remarks>A struct rather than a class so that a bare <c>null</c> argument cannot convert to it and binds to the method's original overload.</remarks>
public struct EvaluationResultListOptions
{
    /// <summary>1-indexed page number.</summary>
    public int? Page { get; set; }

    /// <summary>Items per page (server default 20).</summary>
    public int? Limit { get; set; }

    /// <summary>Only results with this status.</summary>
    public string? Status { get; set; }

    /// <summary>When true, only flagged results.</summary>
    public bool? FlaggedOnly { get; set; }

    /// <summary>Only results from this time on.</summary>
    public DateTimeOffset? TimeFrom { get; set; }

    /// <summary>Only results up to this time.</summary>
    public DateTimeOffset? TimeTo { get; set; }
}

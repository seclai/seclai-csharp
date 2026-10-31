namespace Seclai.Models;

/// <summary>Period for <see cref="SeclaiClient.GetMemoryBankStatsAsync(string, MemoryBankStatsOptions, System.Threading.CancellationToken)"/>.</summary>
/// <remarks>A struct rather than a class so that a bare <c>null</c> argument cannot convert to it and binds to the method's original overload.</remarks>
public struct MemoryBankStatsOptions
{
    /// <summary>Days back to cover, ending today (1-730, server default 30). Ignored when both <see cref="StartDate"/> and <see cref="EndDate"/> are set.</summary>
    public int? Days { get; set; }

    /// <summary>Start of the period, as an ISO 8601 date (<c>yyyy-MM-dd</c>). Used only together with <see cref="EndDate"/>; on its own it is ignored and <see cref="Days"/> applies.</summary>
    public string? StartDate { get; set; }

    /// <summary>End of the period, as an ISO 8601 date (<c>yyyy-MM-dd</c>). Used only together with <see cref="StartDate"/>; on its own it is ignored and <see cref="Days"/> applies.</summary>
    public string? EndDate { get; set; }
}

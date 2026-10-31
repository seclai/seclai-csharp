namespace Seclai.Models;

/// <summary>
/// A source setting that <see cref="SeclaiClient.ClearSourceFieldsAsync(string, System.Collections.Generic.IEnumerable{SourceField}, System.Threading.CancellationToken)"/>
/// can clear by sending it as <c>null</c>.
/// </summary>
public enum SourceField
{
    /// <summary><c>retention_days</c>: removes the retention window, so content is kept indefinitely.</summary>
    RetentionDays,

    /// <summary><c>max_credits_per_period</c>: removes the spend limit.</summary>
    MaxCreditsPerPeriod,
}

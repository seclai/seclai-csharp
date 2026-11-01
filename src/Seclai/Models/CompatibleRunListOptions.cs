using System;

namespace Seclai.Models;

/// <summary>Filters and paging for <see cref="SeclaiClient.ListCompatibleRunsAsync(string, CompatibleRunListOptions, System.Threading.CancellationToken)"/>.</summary>
/// <remarks>A struct rather than a class so that a bare <c>null</c> argument cannot convert to it and binds to the method's original overload.</remarks>
public struct CompatibleRunListOptions
{
    /// <summary>1-indexed page number.</summary>
    public int? Page { get; set; }

    /// <summary>Items per page (server default 20).</summary>
    public int? Limit { get; set; }

    /// <summary>Only runs started after this time.</summary>
    public DateTimeOffset? StartedAfter { get; set; }
}

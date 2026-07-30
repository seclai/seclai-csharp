namespace Seclai;

/// <summary>
/// Dated API versions known to this release, for use with
/// <see cref="SeclaiClientOptions.ApiVersion"/>.
/// </summary>
/// <remarks>
/// The set is open: the API adds versions without an SDK release, and
/// <see cref="SeclaiClientOptions.ApiVersion"/> is a plain <c>string</c> so you can
/// pass a date newer than this release knows about. Treat these as convenience
/// constants, not an exhaustive list — <see cref="SeclaiClient.GetApiVersionAsync"/>
/// reports what the server actually supports.
/// </remarks>
public static class SeclaiApiVersion
{
    /// <summary>The <c>2026-07-01</c> API version.</summary>
    public const string V2026_07_01 = "2026-07-01";

    /// <summary>The <c>2026-07-27</c> API version.</summary>
    public const string V2026_07_27 = "2026-07-27";

    /// <summary>Every version this release was built against, oldest first.</summary>
    public static readonly string[] Known = { V2026_07_01, V2026_07_27 };

    /// <summary>Baseline applied to an unpinned, header-less caller.</summary>
    public const string Default = V2026_07_01;

    /// <summary>Newest version known to this SDK release. May lag the server.</summary>
    public const string Latest = V2026_07_27;
}

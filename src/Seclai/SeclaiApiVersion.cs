namespace Seclai;

/// <summary>
/// Dated API versions known to this release, for use with
/// <see cref="SeclaiClientOptions.ApiVersion"/>.
/// </summary>
/// <remarks>
/// The API adds versions without an SDK release, so the server's set can be larger
/// than this one — <see cref="SeclaiClient.GetApiVersionAsync"/> reports what it
/// supports. <see cref="Known"/> is nonetheless the client's allowlist: a version
/// absent from it is rejected at construction unless
/// <see cref="SeclaiClientOptions.AllowUnknownApiVersion"/> is set.
/// </remarks>
public static class SeclaiApiVersion
{
    /// <summary>The <c>2026-07-01</c> API version.</summary>
    public const string V2026_07_01 = "2026-07-01";

    /// <summary>The <c>2026-07-27</c> API version.</summary>
    public const string V2026_07_27 = "2026-07-27";

    /// <summary>The <c>2026-08-03</c> API version.</summary>
    public const string V2026_08_03 = "2026-08-03";

    /// <summary>The <c>2026-08-21</c> API version.</summary>
    public const string V2026_08_21 = "2026-08-21";

    /// <summary>The <c>2026-09-28</c> API version.</summary>
    public const string V2026_09_28 = "2026-09-28";

    /// <summary>The <c>2026-09-30</c> API version.</summary>
    public const string V2026_09_30 = "2026-09-30";

    /// <summary>The <c>2026-10-03</c> API version.</summary>
    public const string V2026_10_03 = "2026-10-03";

    /// <summary>Every version this release was built against, oldest first.</summary>
    public static readonly string[] Known =
    {
        V2026_07_01, V2026_07_27, V2026_08_03, V2026_08_21, V2026_09_28, V2026_09_30, V2026_10_03,
    };

    /// <summary>Baseline applied to an unpinned, header-less caller.</summary>
    public const string Default = V2026_07_01;

    /// <summary>Newest version known to this SDK release. May lag the server.</summary>
    public const string Latest = V2026_10_03;
}

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>The API version a request resolved to, and the versions available.</summary>
public sealed class ApiVersionResponse
{
    /// <summary>The account's sticky pin, or <c>null</c> when unpinned.</summary>
    [JsonPropertyName("pinned_version")]
    public string? PinnedVersion { get; set; }

    /// <summary>The version THIS request resolved to: header, then pin, then default.</summary>
    [JsonPropertyName("effective_version")]
    public string? EffectiveVersion { get; set; }

    /// <summary>Baseline for an unpinned, header-less caller.</summary>
    [JsonPropertyName("default_version")]
    public string? DefaultVersion { get; set; }

    /// <summary>Newest version the server knows about.</summary>
    [JsonPropertyName("latest_version")]
    public string? LatestVersion { get; set; }

    /// <summary>All dated versions, oldest first.</summary>
    [JsonPropertyName("known_versions")]
    public List<string>? KnownVersions { get; set; }
}

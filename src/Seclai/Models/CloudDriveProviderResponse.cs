using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class CloudDriveProviderResponse
{
    /// <summary>
    /// Mutually-exclusive permission bundles offered when connecting. Connecting happens in the
    /// app, so this is informational here — it explains what a connection's <c>access_level</c>
    /// can be.
    /// </summary>
    [JsonPropertyName("access_levels")]
    public List<CloudDriveAccessLevelResponse> AccessLevels { get; set; } = new();

    /// <summary>Human-readable provider name.</summary>
    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Provider key used as <c>provider</c> on a connection.</summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    /// <summary>OAuth permissions this provider can request.</summary>
    [JsonPropertyName("scopes")]
    public List<CloudDriveScopeResponse> Scopes { get; set; } = new();
}

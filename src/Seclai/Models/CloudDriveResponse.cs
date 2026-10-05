using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>A cloud-drive connection, without any secret material.</summary>
public sealed class CloudDriveResponse
{
    /// <summary>
    /// The permission bundle the granted scopes correspond to — <c>read_write</c> or
    /// <c>read_only</c>. Null when the grant matches no level the provider currently offers;
    /// treat that as unknown rather than assuming write access.
    /// </summary>
    [JsonPropertyName("access_level")]
    public string? AccessLevel { get; set; }

    /// <summary>True when the connection is usable.</summary>
    [JsonPropertyName("connected")]
    public bool Connected { get; set; }

    /// <summary>When the connection was created.</summary>
    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    /// <summary>
    /// Opaque id of the shared drive the folder resolves to, or null for the user's own drive.
    /// Stable across renames — compare on this rather than on the name in <c>folder_path</c>.
    /// </summary>
    [JsonPropertyName("drive_id")]
    public string? DriveId { get; set; }

    /// <summary>Display name the shared drive last resolved to. Presentation only; never match on it.</summary>
    [JsonPropertyName("drive_name")]
    public string? DriveName { get; set; }

    /// <summary>
    /// True when <c>drive_name</c> could not be re-confirmed (the drive was deleted, access was
    /// lost, or the provider was unreachable). The last known name is still reported — treat it
    /// as possibly out of date rather than current.
    /// </summary>
    [JsonPropertyName("drive_name_stale")]
    public bool DriveNameStale { get; set; }

    /// <summary>The provider's own opaque account identifier (never an email).</summary>
    [JsonPropertyName("external_account_id")]
    public string? ExternalAccountId { get; set; }

    /// <summary>
    /// Watched folder; empty string means the drive root. A folder on a shared drive is written
    /// <c>/Shared drives/&lt;drive name&gt;/&lt;folder&gt;</c>.
    /// </summary>
    [JsonPropertyName("folder_path")]
    public string FolderPath { get; set; } = string.Empty;

    /// <summary>Connection identifier.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Most recent sync or authorization error, if any.</summary>
    [JsonPropertyName("last_error")]
    public string? LastError { get; set; }

    /// <summary>When the connection last synced successfully.</summary>
    [JsonPropertyName("last_synced_at")]
    public string? LastSyncedAt { get; set; }

    /// <summary>Human-readable name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>Space-separated OAuth scopes granted to this connection.</summary>
    [JsonPropertyName("oauth_scopes")]
    public string? OauthScopes { get; set; }

    /// <summary>Provider key, e.g. <c>dropbox</c> or <c>google_drive</c>.</summary>
    [JsonPropertyName("provider")]
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// True when changes arrive via the provider's push notifications. False means the drive
    /// still syncs, but only on the scheduled backstop sweep rather than within seconds of a
    /// change.
    /// </summary>
    [JsonPropertyName("realtime_updates")]
    public bool RealtimeUpdates { get; set; }

    /// <summary>One of <c>active</c>, <c>pending_auth</c>, <c>error</c>, <c>disconnected</c>.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>When the connection was last modified.</summary>
    [JsonPropertyName("updated_at")]
    public string UpdatedAt { get; set; } = string.Empty;
}

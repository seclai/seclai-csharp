using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>Fields to change on a cloud-drive connection.</summary>
/// <remarks>A property left <c>null</c> is omitted from the request and left unchanged.</remarks>
public sealed class CloudDriveUpdateRequest
{
    /// <summary>
    /// New watched folder; empty means the whole drive. A folder on a shared drive is written
    /// <c>/Shared drives/&lt;drive name&gt;/&lt;folder&gt;</c>. Changing it resets the sync
    /// cursor, so files already in the new folder are NOT replayed as triggers — only
    /// subsequent changes fire, matching connect-time behaviour. Rejected when the new folder
    /// would make an agent that writes there re-trigger itself.
    /// </summary>
    [JsonPropertyName("folder_path")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FolderPath { get; set; }

    /// <summary>New display name for the connection.</summary>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }
}

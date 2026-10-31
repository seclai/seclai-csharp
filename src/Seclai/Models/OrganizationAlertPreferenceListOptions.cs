namespace Seclai.Models;

/// <summary>Filters for <see cref="SeclaiClient.ListOrganizationAlertPreferencesAsync(OrganizationAlertPreferenceListOptions, System.Threading.CancellationToken)"/>.</summary>
/// <remarks>A struct rather than a class so that a bare <c>null</c> argument cannot convert to it and binds to the method's original overload.</remarks>
public struct OrganizationAlertPreferenceListOptions
{
    /// <summary>Only preferences for this organization.</summary>
    public string? OrganizationId { get; set; }

    /// <summary>When true, also include the default subscribed entry for each alert type.</summary>
    public bool? IncludeDefaults { get; set; }
}

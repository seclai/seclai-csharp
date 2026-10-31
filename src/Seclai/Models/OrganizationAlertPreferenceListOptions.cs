namespace Seclai.Models;

/// <summary>Filters for <see cref="SeclaiClient.ListOrganizationAlertPreferencesAsync(OrganizationAlertPreferenceListOptions, System.Threading.CancellationToken)"/>.</summary>
public sealed class OrganizationAlertPreferenceListOptions
{
    /// <summary>Only preferences for this organization.</summary>
    public string? OrganizationId { get; set; }

    /// <summary>When true, also include the default subscribed entry for each alert type.</summary>
    public bool? IncludeDefaults { get; set; }
}

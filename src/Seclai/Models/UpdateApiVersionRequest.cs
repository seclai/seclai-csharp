using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>Sets or clears the account's sticky API version pin.</summary>
public sealed class UpdateApiVersionRequest
{
    /// <summary>
    /// A <c>YYYY-MM-DD</c> date to pin the account to, or <c>null</c> to clear the
    /// pin and revert to the default baseline.
    /// </summary>
    /// <remarks>
    /// Serialised even when <c>null</c>, because null is the documented way to
    /// clear the pin — omitting the property would leave it unchanged.
    /// </remarks>
    [JsonPropertyName("version")]
    public string? Version { get; set; }
}

using System;

namespace Seclai.Exceptions;

/// <summary>
/// Thrown when the <see cref="SeclaiClient"/> cannot be constructed due to missing
/// or invalid configuration (e.g. no API key).
/// </summary>
public sealed class ConfigurationException : Exception
{
    /// <summary>Creates a <see cref="ConfigurationException"/> with the given message.</summary>
    public ConfigurationException(string message) : base(message) { }
}

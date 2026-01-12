using System;

namespace Seclai.Exceptions;

public sealed class ConfigurationException : Exception
{
    public ConfigurationException(string message) : base(message) { }
}

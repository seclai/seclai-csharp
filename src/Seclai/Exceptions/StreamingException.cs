using System;

namespace Seclai.Exceptions;

public sealed class StreamingException : Exception
{
    public StreamingException(string message)
        : base(message)
    {
    }

    public StreamingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

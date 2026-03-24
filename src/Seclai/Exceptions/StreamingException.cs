using System;

namespace Seclai.Exceptions;

/// <summary>
/// Thrown when an SSE streaming agent run encounters an error, such as a timeout
/// or an unexpected end of stream.
/// </summary>
public sealed class StreamingException : Exception
{
    /// <summary>Creates a <see cref="StreamingException"/> with the given message.</summary>
    public StreamingException(string message)
        : base(message)
    {
    }

    /// <summary>Creates a <see cref="StreamingException"/> with a message and inner exception.</summary>
    public StreamingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

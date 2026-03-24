using System;
using System.Net;
using Seclai.Models;

namespace Seclai.Exceptions;

/// <summary>
/// Thrown when the API returns HTTP 422 (Unprocessable Entity) with structured validation errors.
/// </summary>
public sealed class ApiValidationException : ApiException
{
    /// <summary>Creates an <see cref="ApiValidationException"/> with the parsed validation error details.</summary>
    public ApiValidationException(HttpStatusCode statusCode, string method, Uri url, string? responseBody, HttpValidationError? validation)
        : base(statusCode, method, url, responseBody)
    {
        Validation = validation;
    }

    /// <summary>The parsed validation error, if the response body could be deserialized.</summary>
    public HttpValidationError? Validation { get; }

    /// <summary>Convenience accessor for the individual validation errors.</summary>
    public System.Collections.Generic.IReadOnlyList<ValidationError> Errors =>
        Validation?.Detail ?? (System.Collections.Generic.IReadOnlyList<ValidationError>)System.Array.Empty<ValidationError>();
}

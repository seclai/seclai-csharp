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

    /// <summary>Creates an <see cref="ApiValidationException"/> with the parsed validation error details and the response headers.</summary>
    /// <param name="statusCode">The HTTP status code returned by the API.</param>
    /// <param name="method">The HTTP method of the failed request.</param>
    /// <param name="url">The request URL.</param>
    /// <param name="responseBody">The raw response body, if available.</param>
    /// <param name="validation">The parsed validation error, or null.</param>
    /// <param name="headers">The response headers, or null when unknown; see <see cref="ApiException.Headers"/>.</param>
    public ApiValidationException(HttpStatusCode statusCode, string method, Uri url, string? responseBody, HttpValidationError? validation, System.Collections.Generic.IReadOnlyDictionary<string, System.Collections.Generic.IReadOnlyList<string>>? headers)
        : base(statusCode, method, url, responseBody, headers)
    {
        Validation = validation;
    }

    /// <summary>The parsed validation error, if the response body could be deserialized.</summary>
    public HttpValidationError? Validation { get; }

    /// <summary>Convenience accessor for the individual validation errors.</summary>
    public System.Collections.Generic.IReadOnlyList<ValidationError> Errors =>
        Validation?.Detail ?? (System.Collections.Generic.IReadOnlyList<ValidationError>)System.Array.Empty<ValidationError>();
}

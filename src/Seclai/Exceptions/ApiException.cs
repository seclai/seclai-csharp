using System;
using System.Net;

namespace Seclai.Exceptions;

/// <summary>
/// Thrown when the Seclai API returns a non-success HTTP status code.
/// Contains the status code, HTTP method, request URL, and raw response body.
/// </summary>
public class ApiException : Exception
{
    /// <summary>Creates an <see cref="ApiException"/> from an API error response.</summary>
    public ApiException(HttpStatusCode statusCode, string method, Uri url, string? responseBody)
        : base(BuildMessage(statusCode, method, url, responseBody))
    {
        StatusCode = statusCode;
        Method = method;
        Url = url;
        ResponseBody = responseBody;
    }

    /// <summary>The HTTP status code returned by the API.</summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>The HTTP method of the failed request (e.g. "GET", "POST").</summary>
    public string Method { get; }

    /// <summary>The request URL.</summary>
    public Uri Url { get; }

    /// <summary>The raw response body, if available.</summary>
    public string? ResponseBody { get; }

    private static string BuildMessage(HttpStatusCode statusCode, string method, Uri url, string? responseBody)
    {
        var code = (int)statusCode;
        if (!string.IsNullOrWhiteSpace(responseBody))
        {
            return $"seclai: api error ({code}) {method} {url}: {responseBody}";
        }
        return $"seclai: api error ({code}) {method} {url}";
    }
}

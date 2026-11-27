using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;

namespace Seclai.Exceptions;

/// <summary>
/// Thrown when the Seclai API returns a non-success HTTP status code, or a successful
/// response whose body is not what the method reads (a list method given a non-list).
/// Contains the status code, HTTP method, request URL, raw response body, and response headers.
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

    /// <summary>Creates an <see cref="ApiException"/> from an API error response and its headers.</summary>
    /// <param name="statusCode">The HTTP status code returned by the API.</param>
    /// <param name="method">The HTTP method of the failed request.</param>
    /// <param name="url">The request URL.</param>
    /// <param name="responseBody">The raw response body, if available.</param>
    /// <param name="headers">
    /// The response headers, or null when unknown. They are copied, with names compared case-insensitively;
    /// values of names differing only in case are combined.
    /// </param>
    public ApiException(HttpStatusCode statusCode, string method, Uri url, string? responseBody, IReadOnlyDictionary<string, IReadOnlyList<string>>? headers)
        : this(statusCode, method, url, responseBody)
    {
        Headers = CopyHeaders(headers);
    }

    internal ApiException(string message, HttpStatusCode statusCode, string method, Uri url, string? responseBody)
        : base(message)
    {
        StatusCode = statusCode;
        Method = method;
        Url = url;
        ResponseBody = responseBody;
    }

    internal ApiException(string message, HttpStatusCode statusCode, string method, Uri url, string? responseBody, IReadOnlyDictionary<string, IReadOnlyList<string>>? headers)
        : this(message, statusCode, method, url, responseBody)
    {
        Headers = CopyHeaders(headers);
    }

    /// <summary>The HTTP status code returned by the API.</summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>The HTTP method of the failed request (e.g. "GET", "POST").</summary>
    public string Method { get; }

    /// <summary>The request URL.</summary>
    public Uri Url { get; }

    /// <summary>The raw response body, if available.</summary>
    public string? ResponseBody { get; }

    /// <summary>
    /// The headers of the response the error was built from: the response headers and the content headers
    /// (such as <c>Content-Range</c>, <c>Content-Type</c> and <c>Content-Length</c>) in one map, looked up
    /// case-insensitively, each name with its values. On a <c>416</c> from
    /// <see cref="SeclaiClient.DownloadAgentRunAttachmentWithOptionsAsync"/> the file size is in
    /// <c>Content-Range: bytes */size</c>.
    /// Never null: empty when the error was created without headers.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Headers { get; } = EmptyHeaders;

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> EmptyHeaders =
        new ReadOnlyDictionary<string, IReadOnlyList<string>>(new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase));

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> CopyHeaders(IReadOnlyDictionary<string, IReadOnlyList<string>>? headers)
    {
        if (headers is null || headers.Count == 0) return EmptyHeaders;
        var merged = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in headers)
        {
            if (!merged.TryGetValue(kv.Key, out var values))
            {
                values = new List<string>();
                merged[kv.Key] = values;
            }
            if (kv.Value is not null) values.AddRange(kv.Value);
        }
        var copy = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in merged) copy[kv.Key] = kv.Value.AsReadOnly();
        return new ReadOnlyDictionary<string, IReadOnlyList<string>>(copy);
    }

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

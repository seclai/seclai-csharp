using System;
using System.Net;

namespace Seclai.Exceptions;

public class ApiException : Exception
{
    public ApiException(HttpStatusCode statusCode, string method, Uri url, string? responseBody)
        : base(BuildMessage(statusCode, method, url, responseBody))
    {
        StatusCode = statusCode;
        Method = method;
        Url = url;
        ResponseBody = responseBody;
    }

    public HttpStatusCode StatusCode { get; }

    public string Method { get; }

    public Uri Url { get; }

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

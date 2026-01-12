using System;
using System.Net;
using Seclai.Models;

namespace Seclai.Exceptions;

public sealed class ApiValidationException : ApiException
{
    public ApiValidationException(HttpStatusCode statusCode, string method, Uri url, string? responseBody, HttpValidationError? validation)
        : base(statusCode, method, url, responseBody)
    {
        Validation = validation;
    }

    public HttpValidationError? Validation { get; }
}

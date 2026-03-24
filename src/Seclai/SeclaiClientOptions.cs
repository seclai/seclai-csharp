using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Seclai;

/// <summary>
/// Configuration options for <see cref="SeclaiClient"/>.
/// </summary>
public sealed class SeclaiClientOptions
{
    /// <summary>API key for authentication. Falls back to the <c>SECLAI_API_KEY</c> environment variable.</summary>
    public string? ApiKey { get; set; }

    /// <summary>API base URL. Falls back to the <c>SECLAI_API_URL</c> environment variable, then <see cref="DefaultBaseUri"/>.</summary>
    public Uri? BaseUri { get; set; }

    /// <summary>Header name used to send the API key. Defaults to <c>x-api-key</c>.</summary>
    public string ApiKeyHeader { get; set; } = "x-api-key";

    /// <summary>
    /// Provide your own <see cref="HttpClient"/> instance (e.g. from IHttpClientFactory).
    /// When supplied, the client will <b>not</b> be disposed by <see cref="SeclaiClient"/>.
    /// </summary>
    public HttpClient? HttpClient { get; set; }

    /// <summary>
    /// Extra headers sent with every request (e.g. User-Agent, X-Correlation-Id).
    /// </summary>
    public Dictionary<string, string>? DefaultHeaders { get; set; }

    /// <summary>
    /// HTTP request timeout applied to internally-created <see cref="System.Net.Http.HttpClient"/> instances.
    /// Defaults to 120 seconds. Ignored when <see cref="HttpClient"/> is provided externally.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(120);

    /// <summary>Default API base URL (<c>https://seclai.com</c>).</summary>
    public static Uri DefaultBaseUri => new("https://seclai.com");
}

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Seclai;

/// <summary>
/// Configuration options for <see cref="SeclaiClient"/>.
/// </summary>
public sealed class SeclaiClientOptions
{
    /// <summary>API key for authentication. Falls back to the <c>SECLAI_API_KEY</c> environment variable.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Static bearer token (mutually exclusive with <see cref="ApiKey"/>).</summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Async provider that returns a bearer token per request (mutually exclusive with <see cref="ApiKey"/>).
    /// </summary>
    public Func<CancellationToken, Task<string>>? AccessTokenProvider { get; set; }

    /// <summary>SSO profile name from the config file. Falls back to <c>SECLAI_PROFILE</c>, then "default".</summary>
    public string? Profile { get; set; }

    /// <summary>Config directory override. Falls back to <c>SECLAI_CONFIG_DIR</c>, then <c>~/.seclai</c>.</summary>
    public string? ConfigDir { get; set; }

    /// <summary>Whether to auto-refresh expired SSO tokens. Defaults to <c>true</c>.</summary>
    public bool? AutoRefresh { get; set; }

    /// <summary>Account ID sent as the <c>X-Account-Id</c> header for multi-org targeting.</summary>
    public string? AccountId { get; set; }

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

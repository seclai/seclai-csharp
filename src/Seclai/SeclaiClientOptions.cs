using System;
using System.Net.Http;

namespace Seclai;

public sealed class SeclaiClientOptions
{
    public string? ApiKey { get; set; }

    public Uri? BaseUri { get; set; }

    public string ApiKeyHeader { get; set; } = "x-api-key";

    public HttpClient? HttpClient { get; set; }

    public static Uri DefaultBaseUri => new("https://seclai.com");
}

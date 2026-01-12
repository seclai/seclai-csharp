using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Seclai.Exceptions;
using Seclai.Models;

namespace Seclai;

public sealed class SeclaiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly Uri _baseUri;
    private readonly string _apiKey;
    private readonly string _apiKeyHeader;

    public SeclaiClient(SeclaiClientOptions options)
    {
        if (options is null) throw new ArgumentNullException(nameof(options));

        var apiKey = (options.ApiKey ?? Environment.GetEnvironmentVariable("SECLAI_API_KEY"))?.Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ConfigurationException("missing API key: provide SeclaiClientOptions.ApiKey or set SECLAI_API_KEY");
        }

        var baseUrl = options.BaseUri
            ?? TryGetEnvUri("SECLAI_API_URL")
            ?? SeclaiClientOptions.DefaultBaseUri;

        _apiKey = apiKey;
        _apiKeyHeader = string.IsNullOrWhiteSpace(options.ApiKeyHeader) ? "x-api-key" : options.ApiKeyHeader;
        _http = options.HttpClient ?? new HttpClient();
        _baseUri = EnsureTrailingSlash(baseUrl);
    }

    public async Task<SourceListResponse> ListSourcesAsync(
        int? page = null,
        int? limit = null,
        string? sort = null,
        string? order = null,
        string? accountId = null,
        CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["page"] = page is > 0 ? page.Value.ToString() : null,
            ["limit"] = limit is > 0 ? limit.Value.ToString() : null,
            ["sort"] = string.IsNullOrWhiteSpace(sort) ? null : sort,
            ["order"] = string.IsNullOrWhiteSpace(order) ? null : order,
            ["account_id"] = string.IsNullOrWhiteSpace(accountId) ? null : accountId,
        };

        // Note: spec path includes trailing slash.
        return await SendJsonAsync<SourceListResponse>(HttpMethod.Get, "/api/sources/", query, body: null, cancellationToken);
    }

    public async Task<AgentRunResponse> RunAgentAsync(string agentId, AgentRunRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));
        return await SendJsonAsync<AgentRunResponse>(HttpMethod.Post, $"/api/agents/{Uri.EscapeDataString(agentId)}/runs", query: null, body, cancellationToken);
    }

    public async Task<AgentRunListResponse> ListAgentRunsAsync(string agentId, int? page = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));

        var query = new Dictionary<string, string?>
        {
            ["page"] = page is > 0 ? page.Value.ToString() : null,
            ["limit"] = limit is > 0 ? limit.Value.ToString() : null,
        };

        return await SendJsonAsync<AgentRunListResponse>(HttpMethod.Get, $"/api/agents/{Uri.EscapeDataString(agentId)}/runs", query, body: null, cancellationToken);
    }

    public async Task<AgentRunResponse> GetAgentRunAsync(string agentId, string runId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));
        if (string.IsNullOrWhiteSpace(runId)) throw new ArgumentException("runId is required", nameof(runId));

        return await SendJsonAsync<AgentRunResponse>(HttpMethod.Get, $"/api/agents/{Uri.EscapeDataString(agentId)}/runs/{Uri.EscapeDataString(runId)}", query: null, body: null, cancellationToken);
    }

    public async Task<AgentRunResponse> DeleteAgentRunAsync(string agentId, string runId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));
        if (string.IsNullOrWhiteSpace(runId)) throw new ArgumentException("runId is required", nameof(runId));

        return await SendJsonAsync<AgentRunResponse>(HttpMethod.Delete, $"/api/agents/{Uri.EscapeDataString(agentId)}/runs/{Uri.EscapeDataString(runId)}", query: null, body: null, cancellationToken);
    }

    public async Task<ContentDetailResponse> GetContentDetailAsync(
        string sourceConnectionContentVersion,
        int? start = null,
        int? end = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceConnectionContentVersion))
        {
            throw new ArgumentException("sourceConnectionContentVersion is required", nameof(sourceConnectionContentVersion));
        }

        var query = new Dictionary<string, string?>
        {
            ["start"] = start is > 0 ? start.Value.ToString() : null,
            ["end"] = end is > 0 ? end.Value.ToString() : null,
        };

        return await SendJsonAsync<ContentDetailResponse>(HttpMethod.Get, $"/api/contents/{Uri.EscapeDataString(sourceConnectionContentVersion)}", query, body: null, cancellationToken);
    }

    public async Task DeleteContentAsync(string sourceConnectionContentVersion, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceConnectionContentVersion))
        {
            throw new ArgumentException("sourceConnectionContentVersion is required", nameof(sourceConnectionContentVersion));
        }

        await SendJsonAsync<object>(HttpMethod.Delete, $"/api/contents/{Uri.EscapeDataString(sourceConnectionContentVersion)}", query: null, body: null, cancellationToken, expectBody: false);
    }

    public async Task<ContentEmbeddingsListResponse> ListContentEmbeddingsAsync(
        string sourceConnectionContentVersion,
        int? page = null,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceConnectionContentVersion))
        {
            throw new ArgumentException("sourceConnectionContentVersion is required", nameof(sourceConnectionContentVersion));
        }

        var query = new Dictionary<string, string?>
        {
            ["page"] = page is > 0 ? page.Value.ToString() : null,
            ["limit"] = limit is > 0 ? limit.Value.ToString() : null,
        };

        return await SendJsonAsync<ContentEmbeddingsListResponse>(HttpMethod.Get, $"/api/contents/{Uri.EscapeDataString(sourceConnectionContentVersion)}/embeddings", query, body: null, cancellationToken);
    }

    public async Task<FileUploadResponse> UploadFileToSourceAsync(
        string sourceConnectionId,
        byte[] fileBytes,
        string fileName,
        string? title = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceConnectionId)) throw new ArgumentException("sourceConnectionId is required", nameof(sourceConnectionId));
        if (fileBytes is null || fileBytes.Length == 0) throw new ArgumentException("fileBytes must be non-empty", nameof(fileBytes));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("fileName is required", nameof(fileName));

        var url = BuildUri($"/api/sources/{Uri.EscapeDataString(sourceConnectionId)}/upload", query: null);

        using var content = new MultipartFormDataContent();
        if (!string.IsNullOrWhiteSpace(title))
        {
            content.Add(new StringContent(title, Encoding.UTF8), "title");
        }

        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "file", fileName);

        using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        req.Headers.TryAddWithoutValidation(_apiKeyHeader, _apiKey);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var resp = await _http.SendAsync(req, cancellationToken).ConfigureAwait(false);
        var responseBody = await ReadBodyAsync(resp).ConfigureAwait(false);

        if (!resp.IsSuccessStatusCode)
        {
            ThrowApiError(resp.StatusCode, req.Method.Method, url, responseBody);
        }

        var parsed = JsonSerializer.Deserialize<FileUploadResponse>(responseBody ?? string.Empty, JsonOptions);
        return parsed ?? new FileUploadResponse();
    }

    private async Task<T> SendJsonAsync<T>(HttpMethod method, string path, Dictionary<string, string?>? query, object? body, CancellationToken cancellationToken, bool expectBody = true)
    {
        var url = BuildUri(path, query);
        using var req = new HttpRequestMessage(method, url);

        req.Headers.TryAddWithoutValidation(_apiKeyHeader, _apiKey);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (body is not null)
        {
            var json = JsonSerializer.Serialize(body, JsonOptions);
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        using var resp = await _http.SendAsync(req, cancellationToken).ConfigureAwait(false);
        var responseBody = await ReadBodyAsync(resp).ConfigureAwait(false);

        if (!resp.IsSuccessStatusCode)
        {
            ThrowApiError(resp.StatusCode, method.Method, url, responseBody);
        }

        if (!expectBody)
        {
            return default!;
        }

        var parsed = JsonSerializer.Deserialize<T>(responseBody ?? string.Empty, JsonOptions);
        if (parsed is null)
        {
            throw new ApiException(resp.StatusCode, method.Method, url, responseBody);
        }
        return parsed;
    }

    private void ThrowApiError(HttpStatusCode statusCode, string method, Uri url, string? responseBody)
    {
        if ((int)statusCode == 422)
        {
            HttpValidationError? validation = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(responseBody))
                {
                    validation = JsonSerializer.Deserialize<HttpValidationError>(responseBody, JsonOptions);
                }
            }
            catch
            {
                // ignore parse issues
            }
            throw new ApiValidationException(statusCode, method, url, responseBody, validation);
        }
        throw new ApiException(statusCode, method, url, responseBody);
    }

    private Uri BuildUri(string path, Dictionary<string, string?>? query)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("path is required", nameof(path));
        var builder = new UriBuilder(new Uri(_baseUri, path));

        if (query is not null)
        {
            var parts = new List<string>();
            foreach (var kv in query)
            {
                if (string.IsNullOrWhiteSpace(kv.Key) || string.IsNullOrWhiteSpace(kv.Value)) continue;
                parts.Add($"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}");
            }
            builder.Query = string.Join("&", parts);
        }

        return builder.Uri;
    }

    private static async Task<string?> ReadBodyAsync(HttpResponseMessage response)
    {
        if (response.Content is null) return null;
        return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    }

    private static Uri? TryGetEnvUri(string envVar)
    {
        var value = Environment.GetEnvironmentVariable(envVar);
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri)) return uri;
        return null;
    }

    private static Uri EnsureTrailingSlash(Uri uri)
    {
        var s = uri.ToString();
        if (!s.EndsWith("/", StringComparison.Ordinal))
        {
            s += "/";
        }
        return new Uri(s);
    }
}

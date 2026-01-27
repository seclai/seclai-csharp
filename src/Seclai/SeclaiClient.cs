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

        var apiKeyRaw = options.ApiKey ?? Environment.GetEnvironmentVariable("SECLAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKeyRaw))
        {
            throw new ConfigurationException("missing API key: provide SeclaiClientOptions.ApiKey or set SECLAI_API_KEY");
        }

        var baseUrl = options.BaseUri
            ?? TryGetEnvUri("SECLAI_API_URL")
            ?? SeclaiClientOptions.DefaultBaseUri;

        _apiKey = apiKeyRaw.Trim();
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
        return await SendJsonAsync<SourceListResponse>(HttpMethod.Get, "/sources/", query, body: null, cancellationToken);
    }

    public async Task<AgentRunResponse> RunAgentAsync(string agentId, AgentRunRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));
        return await SendJsonAsync<AgentRunResponse>(HttpMethod.Post, $"/agents/{Uri.EscapeDataString(agentId)}/runs", query: null, body, cancellationToken);
    }

    public async Task<AgentRunResponse> RunStreamingAgentAndWaitAsync(
        string agentId,
        AgentRunStreamRequest body,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));

        var url = BuildUri($"/agents/{Uri.EscapeDataString(agentId)}/runs/stream", query: null);

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.TryAddWithoutValidation(_apiKeyHeader, _apiKey);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var json = JsonSerializer.Serialize(body, JsonOptions);
        req.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(60);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(effectiveTimeout);
        var ct = cts.Token;

        HttpResponseMessage? resp = null;
        try
        {
            resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);

            // Ensure that cancellation/timeout aborts any pending stream reads.
            using var _ = ct.Register(() =>
            {
                try { resp.Dispose(); } catch { /* ignore */ }
            });

            if (!resp.IsSuccessStatusCode)
            {
                var responseBody = await ReadBodyAsync(resp).ConfigureAwait(false);
                ThrowApiError(resp.StatusCode, req.Method.Method, url, responseBody);
            }

            var mediaType = resp.Content.Headers.ContentType?.MediaType ?? string.Empty;
            if (mediaType.IndexOf("application/json", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var responseBody = await ReadBodyAsync(resp).ConfigureAwait(false);
                var parsed = JsonSerializer.Deserialize<AgentRunResponse>(responseBody ?? string.Empty, JsonOptions);
                if (parsed is null) throw new StreamingException("Empty JSON response from streaming endpoint.");
                return parsed;
            }

            using var stream = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            string? currentEvent = null;
            var dataLines = new List<string>();
            AgentRunResponse? lastSeen = null;

            AgentRunResponse? Dispatch()
            {
                if (currentEvent is null && dataLines.Count == 0) return null;
                var evt = currentEvent;
                var data = string.Join("\n", dataLines);
                currentEvent = null;
                dataLines.Clear();

                if (string.IsNullOrWhiteSpace(evt) || string.IsNullOrWhiteSpace(data)) return null;
                if (!string.Equals(evt, "init", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(evt, "done", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                try
                {
                    var parsed = JsonSerializer.Deserialize<AgentRunResponse>(data, JsonOptions);
                    if (parsed is null) return null;
                    lastSeen = parsed;
                    if (string.Equals(evt, "done", StringComparison.OrdinalIgnoreCase)) return parsed;
                }
                catch
                {
                    // ignore malformed event payloads
                }
                return null;
            }

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                string? line;
                try
                {
                    line = await reader.ReadLineAsync().ConfigureAwait(false);
                }
                catch (Exception ex) when (ct.IsCancellationRequested)
                {
                    throw new StreamingException($"Timed out after {effectiveTimeout.TotalMilliseconds}ms waiting for streaming agent run to complete.", ex);
                }

                if (line is null) break;

                if (line.Length == 0)
                {
                    var done = Dispatch();
                    if (done is not null) return done;
                    continue;
                }

                if (line.StartsWith(":", StringComparison.Ordinal)) continue;

                if (line.StartsWith("event:", StringComparison.OrdinalIgnoreCase))
                {
                    currentEvent = line.Substring("event:".Length).Trim();
                    if (currentEvent.Length == 0) currentEvent = null;
                    continue;
                }

                if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    dataLines.Add(line.Substring("data:".Length).TrimStart());
                }
            }

            var final = Dispatch();
            if (final is not null) return final;
            if (lastSeen is not null) return lastSeen;

            throw new StreamingException("Stream ended before receiving a 'done' event.");
        }
        catch (OperationCanceledException ex)
        {
            throw new StreamingException($"Timed out after {effectiveTimeout.TotalMilliseconds}ms waiting for streaming agent run to complete.", ex);
        }
        finally
        {
            resp?.Dispose();
        }
    }

    public async Task<AgentRunListResponse> ListAgentRunsAsync(string agentId, int? page = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));

        var query = new Dictionary<string, string?>
        {
            ["page"] = page is > 0 ? page.Value.ToString() : null,
            ["limit"] = limit is > 0 ? limit.Value.ToString() : null,
        };

        return await SendJsonAsync<AgentRunListResponse>(HttpMethod.Get, $"/agents/{Uri.EscapeDataString(agentId)}/runs", query, body: null, cancellationToken);
    }

    public async Task<AgentRunResponse> GetAgentRunAsync(string agentId, string runId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));
        if (string.IsNullOrWhiteSpace(runId)) throw new ArgumentException("runId is required", nameof(runId));

        return await GetAgentRunAsync(agentId, runId, includeStepOutputs: false, cancellationToken);
    }

    public async Task<AgentRunResponse> GetAgentRunAsync(string agentId, string runId, bool includeStepOutputs, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));
        if (string.IsNullOrWhiteSpace(runId)) throw new ArgumentException("runId is required", nameof(runId));

        Dictionary<string, string?>? query = null;
        if (includeStepOutputs)
        {
            query = new Dictionary<string, string?>
            {
                ["include_step_outputs"] = "true"
            };
        }

        return await SendJsonAsync<AgentRunResponse>(
            HttpMethod.Get,
            $"/agents/{Uri.EscapeDataString(agentId)}/runs/{Uri.EscapeDataString(runId)}",
            query,
            body: null,
            cancellationToken);
    }

    public async Task<AgentRunResponse> DeleteAgentRunAsync(string agentId, string runId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));
        if (string.IsNullOrWhiteSpace(runId)) throw new ArgumentException("runId is required", nameof(runId));

        return await SendJsonAsync<AgentRunResponse>(HttpMethod.Delete, $"/agents/{Uri.EscapeDataString(agentId)}/runs/{Uri.EscapeDataString(runId)}", query: null, body: null, cancellationToken);
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

        return await SendJsonAsync<ContentDetailResponse>(HttpMethod.Get, $"/contents/{Uri.EscapeDataString(sourceConnectionContentVersion)}", query, body: null, cancellationToken);
    }

    public async Task DeleteContentAsync(string sourceConnectionContentVersion, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceConnectionContentVersion))
        {
            throw new ArgumentException("sourceConnectionContentVersion is required", nameof(sourceConnectionContentVersion));
        }

        await SendJsonAsync<object>(HttpMethod.Delete, $"/contents/{Uri.EscapeDataString(sourceConnectionContentVersion)}", query: null, body: null, cancellationToken, expectBody: false);
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

        return await SendJsonAsync<ContentEmbeddingsListResponse>(HttpMethod.Get, $"/contents/{Uri.EscapeDataString(sourceConnectionContentVersion)}/embeddings", query, body: null, cancellationToken);
    }

    /// <summary>
    /// Upload a file to a specific source connection.
    /// </summary>
    /// <remarks>
    /// <para><strong>Maximum file size:</strong> 200 MiB.</para>
    /// <para><strong>Supported MIME types:</strong></para>
    /// <list type="bullet">
    /// <item><description><c>application/epub+zip</c></description></item>
    /// <item><description><c>application/json</c></description></item>
    /// <item><description><c>application/msword</c></description></item>
    /// <item><description><c>application/pdf</c></description></item>
    /// <item><description><c>application/vnd.ms-excel</c></description></item>
    /// <item><description><c>application/vnd.ms-outlook</c></description></item>
    /// <item><description><c>application/vnd.ms-powerpoint</c></description></item>
    /// <item><description><c>application/vnd.openxmlformats-officedocument.presentationml.presentation</c></description></item>
    /// <item><description><c>application/vnd.openxmlformats-officedocument.spreadsheetml.sheet</c></description></item>
    /// <item><description><c>application/vnd.openxmlformats-officedocument.wordprocessingml.document</c></description></item>
    /// <item><description><c>application/xml</c></description></item>
    /// <item><description><c>application/zip</c></description></item>
    /// <item><description><c>audio/flac</c>, <c>audio/mp4</c>, <c>audio/mpeg</c>, <c>audio/ogg</c>, <c>audio/wav</c></description></item>
    /// <item><description><c>image/bmp</c>, <c>image/gif</c>, <c>image/jpeg</c>, <c>image/png</c>, <c>image/tiff</c>, <c>image/webp</c></description></item>
    /// <item><description><c>text/csv</c>, <c>text/html</c>, <c>text/markdown</c>, <c>text/x-markdown</c>, <c>text/plain</c>, <c>text/xml</c></description></item>
    /// <item><description><c>video/mp4</c>, <c>video/quicktime</c>, <c>video/x-msvideo</c></description></item>
    /// </list>
    /// <para>
    /// If <paramref name="mimeType"/> is omitted, the SDK attempts to infer it from <paramref name="fileName"/>.
    /// If the upload is sent as <c>application/octet-stream</c>, the server attempts to infer the type from the file extension.
    /// </para>
    /// </remarks>
    public async Task<FileUploadResponse> UploadFileToSourceAsync(
        string sourceConnectionId,
        byte[] fileBytes,
        string fileName,
        string? title = null,
        string? mimeType = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceConnectionId)) throw new ArgumentException("sourceConnectionId is required", nameof(sourceConnectionId));
        if (fileBytes is null || fileBytes.Length == 0) throw new ArgumentException("fileBytes must be non-empty", nameof(fileBytes));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("fileName is required", nameof(fileName));

        var url = BuildUri($"/sources/{Uri.EscapeDataString(sourceConnectionId)}/upload", query: null);

        using var content = new MultipartFormDataContent();
        if (!string.IsNullOrWhiteSpace(title))
        {
            content.Add(new StringContent(title, Encoding.UTF8), "title");
        }

        var inferredMimeType = string.IsNullOrWhiteSpace(mimeType)
            ? TryInferMimeTypeFromFileName(fileName)
            : mimeType;

        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(inferredMimeType) ? "application/octet-stream" : inferredMimeType);
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

    private static string? TryInferMimeTypeFromFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return null;

        var dot = fileName.LastIndexOf('.');
        if (dot < 0 || dot == fileName.Length - 1) return null;

        var ext = fileName.Substring(dot).ToLowerInvariant();
        return ext switch
        {
            ".epub" => "application/epub+zip",
            ".json" => "application/json",
            ".doc" => "application/msword",
            ".pdf" => "application/pdf",
            ".xls" => "application/vnd.ms-excel",
            ".msg" => "application/vnd.ms-outlook",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xml" => "application/xml",
            ".zip" => "application/zip",
            ".flac" => "audio/flac",
            ".m4a" => "audio/mp4",
            ".mp4" => "video/mp4",
            ".mp3" => "audio/mpeg",
            ".ogg" => "audio/ogg",
            ".wav" => "audio/wav",
            ".bmp" => "image/bmp",
            ".gif" => "image/gif",
            ".jpeg" => "image/jpeg",
            ".jpg" => "image/jpeg",
            ".png" => "image/png",
            ".tiff" => "image/tiff",
            ".tif" => "image/tiff",
            ".webp" => "image/webp",
            ".csv" => "text/csv",
            ".html" => "text/html",
            ".htm" => "text/html",
            ".md" => "text/markdown",
            ".markdown" => "text/markdown",
            ".txt" => "text/plain",
            ".avi" => "video/x-msvideo",
            ".mov" => "video/quicktime",
            _ => null
        };
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
                    validation = JsonSerializer.Deserialize<HttpValidationError>(responseBody!, JsonOptions);
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Seclai.Exceptions;
using Seclai.Models;

namespace Seclai;

/// <summary>
/// HTTP client for the Seclai REST API. Provides strongly-typed async methods for
/// agents, knowledge bases, memory banks, sources, content, evaluations, solutions,
/// governance, alerts, search, and AI assistants.
/// </summary>
public sealed class SeclaiClient : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly HttpMethod HttpPatch = new("PATCH");

    private readonly HttpClient _http;
    private readonly bool _ownsHttp;
    private readonly Uri _baseUri;
    private readonly string _apiKey;
    private readonly string _apiKeyHeader;
    private readonly Dictionary<string, string>? _defaultHeaders;

    /// <summary>Creates a new <see cref="SeclaiClient"/> from the given options.</summary>
    /// <exception cref="ConfigurationException">Thrown when no API key is provided.</exception>
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
        _defaultHeaders = options.DefaultHeaders;

        if (options.HttpClient is not null)
        {
            _http = options.HttpClient;
            _ownsHttp = false;
        }
        else
        {
            _http = new HttpClient { Timeout = options.Timeout };
            _ownsHttp = true;
        }

        _baseUri = EnsureTrailingSlash(baseUrl);
    }

    /// <summary>Disposes the underlying <see cref="HttpClient"/> if it was created by this client.</summary>
    public void Dispose()
    {
        if (_ownsHttp) _http.Dispose();
    }

    /// <summary>Lists source connections with optional pagination and sorting.</summary>
    public async Task<SourceListResponse> ListSourcesAsync(
        int? page = null,
        int? limit = null,
        string? sort = null,
        string? order = null,
        string? accountId = null,
        CancellationToken cancellationToken = default)
    {
        var query = PaginationQuery(page, limit, sort, order);
        query["account_id"] = string.IsNullOrWhiteSpace(accountId) ? null : accountId;

        // Note: spec path includes trailing slash.
        return await SendJsonAsync<SourceListResponse>(HttpMethod.Get, "/sources/", query, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Starts a new synchronous agent run.</summary>
    public async Task<AgentRunResponse> RunAgentAsync(string agentId, AgentRunRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));
        return await SendJsonAsync<AgentRunResponse>(HttpMethod.Post, $"/agents/{Uri.EscapeDataString(agentId)}/runs", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Starts an agent run via SSE streaming and blocks until the final <c>done</c> event.
    /// </summary>
    /// <param name="agentId">The agent identifier.</param>
    /// <param name="body">The run request payload.</param>
    /// <param name="timeout">Maximum time to wait for completion (default: 60 s).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="StreamingException">Thrown on timeout or if the stream ends without a <c>done</c> event.</exception>
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
        ApplyDefaultHeaders(req);

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

    /// <summary>Lists runs for an agent with optional pagination.</summary>
    public async Task<AgentRunListResponse> ListAgentRunsAsync(string agentId, int? page = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));

        var query = PaginationQuery(page, limit);

        return await SendJsonAsync<AgentRunListResponse>(HttpMethod.Get, $"/agents/{Uri.EscapeDataString(agentId)}/runs", query, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Gets an agent run by its run ID (without step outputs).</summary>
    public Task<AgentRunResponse> GetAgentRunAsync(string runId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(runId)) throw new ArgumentException("runId is required", nameof(runId));
        return GetAgentRunAsync(runId, includeStepOutputs: false, cancellationToken);
    }

    /// <summary>Gets an agent run by its run ID, optionally including step outputs.</summary>
    public async Task<AgentRunResponse> GetAgentRunAsync(string runId, bool includeStepOutputs, CancellationToken cancellationToken = default)
    {
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
            $"/agents/runs/{Uri.EscapeDataString(runId)}",
            query,
            body: null,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Deletes an agent run.</summary>
    public Task<AgentRunResponse> DeleteAgentRunAsync(string runId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(runId)) throw new ArgumentException("runId is required", nameof(runId));
        return SendJsonAsync<AgentRunResponse>(HttpMethod.Delete, $"/agents/runs/{Uri.EscapeDataString(runId)}", query: null, body: null, cancellationToken);
    }

    /// <summary>Gets content details with optional text range pagination.</summary>
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

        return await SendJsonAsync<ContentDetailResponse>(HttpMethod.Get, $"/contents/{Uri.EscapeDataString(sourceConnectionContentVersion)}", query, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Deletes a content version.</summary>
    public async Task DeleteContentAsync(string sourceConnectionContentVersion, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceConnectionContentVersion))
        {
            throw new ArgumentException("sourceConnectionContentVersion is required", nameof(sourceConnectionContentVersion));
        }

        await SendJsonAsync<object>(HttpMethod.Delete, $"/contents/{Uri.EscapeDataString(sourceConnectionContentVersion)}", query: null, body: null, cancellationToken, expectBody: false).ConfigureAwait(false);
    }

    /// <summary>Lists embeddings for a content version with pagination.</summary>
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

        var query = PaginationQuery(page, limit);

        return await SendJsonAsync<ContentEmbeddingsListResponse>(HttpMethod.Get, $"/contents/{Uri.EscapeDataString(sourceConnectionContentVersion)}/embeddings", query, body: null, cancellationToken).ConfigureAwait(false);
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
        IReadOnlyDictionary<string, object?>? metadata = null,
        string? mimeType = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceConnectionId)) throw new ArgumentException("sourceConnectionId is required", nameof(sourceConnectionId));
        if (fileBytes is null || fileBytes.Length == 0) throw new ArgumentException("fileBytes must be non-empty", nameof(fileBytes));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("fileName is required", nameof(fileName));

        using var content = BuildMultipartContent(fileBytes, fileName, title, metadata, mimeType);
        var raw = await DoUploadAsync($"/sources/{Uri.EscapeDataString(sourceConnectionId)}/upload", content, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<FileUploadResponse>(raw, JsonOptions) ?? new FileUploadResponse();
    }

    /// <summary>
    /// Uploads a file from a <see cref="Stream"/> to a source connection, avoiding loading the full file into memory.
    /// </summary>
    public async Task<FileUploadResponse> UploadFileToSourceAsync(
        string sourceConnectionId,
        Stream fileStream,
        string fileName,
        string? title = null,
        IReadOnlyDictionary<string, object?>? metadata = null,
        string? mimeType = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceConnectionId)) throw new ArgumentException("sourceConnectionId is required", nameof(sourceConnectionId));
        if (fileStream is null) throw new ArgumentNullException(nameof(fileStream));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("fileName is required", nameof(fileName));

        using var content = BuildMultipartContent(fileStream, fileName, title, metadata, mimeType);
        var raw = await DoUploadAsync($"/sources/{Uri.EscapeDataString(sourceConnectionId)}/upload", content, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<FileUploadResponse>(raw, JsonOptions) ?? new FileUploadResponse();
    }

    /// <summary>
    /// Upload a file and replace the content backing an existing content version.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This uploads a new file to <c>/contents/{source_connection_content_version}/upload</c>.
    /// It behaves like <c>UploadFileToSourceAsync</c>, but targets an existing content version ID.
    /// </para>
    /// </remarks>
    public async Task<FileUploadResponse> UploadFileToContentAsync(
        string sourceConnectionContentVersionId,
        byte[] fileBytes,
        string fileName,
        string? title = null,
        IReadOnlyDictionary<string, object?>? metadata = null,
        string? mimeType = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceConnectionContentVersionId)) throw new ArgumentException("sourceConnectionContentVersionId is required", nameof(sourceConnectionContentVersionId));
        if (fileBytes is null || fileBytes.Length == 0) throw new ArgumentException("fileBytes must be non-empty", nameof(fileBytes));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("fileName is required", nameof(fileName));

        using var content = BuildMultipartContent(fileBytes, fileName, title, metadata, mimeType);
        var raw = await DoUploadAsync($"/contents/{Uri.EscapeDataString(sourceConnectionContentVersionId)}/upload", content, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<FileUploadResponse>(raw, JsonOptions) ?? new FileUploadResponse();
    }

    /// <summary>
    /// Uploads a file from a <see cref="Stream"/> and replaces the content backing an existing content version.
    /// </summary>
    public async Task<FileUploadResponse> UploadFileToContentAsync(
        string sourceConnectionContentVersionId,
        Stream fileStream,
        string fileName,
        string? title = null,
        IReadOnlyDictionary<string, object?>? metadata = null,
        string? mimeType = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceConnectionContentVersionId)) throw new ArgumentException("sourceConnectionContentVersionId is required", nameof(sourceConnectionContentVersionId));
        if (fileStream is null) throw new ArgumentNullException(nameof(fileStream));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("fileName is required", nameof(fileName));

        using var content = BuildMultipartContent(fileStream, fileName, title, metadata, mimeType);
        var raw = await DoUploadAsync($"/contents/{Uri.EscapeDataString(sourceConnectionContentVersionId)}/upload", content, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<FileUploadResponse>(raw, JsonOptions) ?? new FileUploadResponse();
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

    private MultipartFormDataContent BuildMultipartContent(
        byte[] fileBytes,
        string fileName,
        string? title,
        IReadOnlyDictionary<string, object?>? metadata,
        string? mimeType)
    {
        var content = BuildMultipartShell(fileName, title, metadata, mimeType);
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            ResolveContentType(fileName, mimeType));
        content.Add(fileContent, "file", fileName);
        return content;
    }

    private MultipartFormDataContent BuildMultipartContent(
        Stream fileStream,
        string fileName,
        string? title,
        IReadOnlyDictionary<string, object?>? metadata,
        string? mimeType)
    {
        var content = BuildMultipartShell(fileName, title, metadata, mimeType);
        var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(
            ResolveContentType(fileName, mimeType));
        content.Add(streamContent, "file", fileName);
        return content;
    }

    private MultipartFormDataContent BuildMultipartShell(
        string fileName,
        string? title,
        IReadOnlyDictionary<string, object?>? metadata,
        string? mimeType)
    {
        var content = new MultipartFormDataContent();
        if (!string.IsNullOrWhiteSpace(title))
        {
            content.Add(new StringContent(title!, Encoding.UTF8), "title");
        }

        if (metadata is not null && metadata.Count > 0)
        {
            var metadataJson = JsonSerializer.Serialize(metadata, JsonOptions);
            content.Add(new StringContent(metadataJson, Encoding.UTF8, "text/plain"), "metadata");
        }

        return content;
    }

    private static string ResolveContentType(string fileName, string? mimeType)
    {
        if (!string.IsNullOrWhiteSpace(mimeType)) return mimeType!;
        return TryInferMimeTypeFromFileName(fileName) ?? "application/octet-stream";
    }

    private async Task<T> SendJsonAsync<T>(HttpMethod method, string path, Dictionary<string, string?>? query, object? body, CancellationToken cancellationToken, bool expectBody = true)
    {
        var url = BuildUri(path, query);
        using var req = new HttpRequestMessage(method, url);

        req.Headers.TryAddWithoutValidation(_apiKeyHeader, _apiKey);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        ApplyDefaultHeaders(req);

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

    private async Task SendNoContentAsync(HttpMethod method, string path, Dictionary<string, string?>? query, object? body, CancellationToken cancellationToken)
    {
        var url = BuildUri(path, query);
        using var req = new HttpRequestMessage(method, url);

        req.Headers.TryAddWithoutValidation(_apiKeyHeader, _apiKey);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        ApplyDefaultHeaders(req);

        if (body is not null)
        {
            var json = JsonSerializer.Serialize(body, JsonOptions);
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        using var resp = await _http.SendAsync(req, cancellationToken).ConfigureAwait(false);

        if (!resp.IsSuccessStatusCode)
        {
            var responseBody = await ReadBodyAsync(resp).ConfigureAwait(false);
            ThrowApiError(resp.StatusCode, method.Method, url, responseBody);
        }
    }

    private async Task<JsonElement> SendRawAsync(HttpMethod method, string path, Dictionary<string, string?>? query, object? body, CancellationToken cancellationToken)
    {
        var url = BuildUri(path, query);
        using var req = new HttpRequestMessage(method, url);

        req.Headers.TryAddWithoutValidation(_apiKeyHeader, _apiKey);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        ApplyDefaultHeaders(req);

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

        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return default;
        }

        using var doc = JsonDocument.Parse(responseBody!);
        return doc.RootElement.Clone();
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

    private void ApplyDefaultHeaders(HttpRequestMessage req)
    {
        if (_defaultHeaders is null) return;
        foreach (var kv in _defaultHeaders)
        {
            req.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
        }
    }

    private static Dictionary<string, string?> PaginationQuery(
        int? page = null,
        int? limit = null,
        string? sort = null,
        string? order = null)
    {
        return new Dictionary<string, string?>
        {
            ["page"] = page is > 0 ? page.Value.ToString() : null,
            ["limit"] = limit is > 0 ? limit.Value.ToString() : null,
            ["sort"] = string.IsNullOrWhiteSpace(sort) ? null : sort,
            ["order"] = string.IsNullOrWhiteSpace(order) ? null : order,
        };
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

    // ── Agents ──────────────────────────────────────────────────────────────

    /// <summary>Lists agents.</summary>
    public async Task<AgentListResponse> ListAgentsAsync(int? page = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        var query = PaginationQuery(page, limit);
        return await SendJsonAsync<AgentListResponse>(HttpMethod.Get, "/agents", query, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Creates a new agent.</summary>
    public async Task<AgentSummaryResponse> CreateAgentAsync(CreateAgentRequest body, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<AgentSummaryResponse>(HttpMethod.Post, "/agents", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Retrieves an agent by ID.</summary>
    public async Task<AgentSummaryResponse> GetAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));
        return await SendJsonAsync<AgentSummaryResponse>(HttpMethod.Get, $"/agents/{Uri.EscapeDataString(agentId)}", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Updates an agent.</summary>
    public async Task<AgentSummaryResponse> UpdateAgentAsync(string agentId, UpdateAgentRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));
        return await SendJsonAsync<AgentSummaryResponse>(HttpMethod.Put, $"/agents/{Uri.EscapeDataString(agentId)}", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Deletes an agent.</summary>
    public async Task DeleteAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));
        await SendNoContentAsync(HttpMethod.Delete, $"/agents/{Uri.EscapeDataString(agentId)}", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    // ── Agent Definitions ───────────────────────────────────────────────────

    /// <summary>Retrieves the definition (step configuration) for an agent.</summary>
    public async Task<AgentDefinitionResponse> GetAgentDefinitionAsync(string agentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));
        return await SendJsonAsync<AgentDefinitionResponse>(HttpMethod.Get, $"/agents/{Uri.EscapeDataString(agentId)}/definition", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Updates the definition for an agent.</summary>
    public async Task<AgentDefinitionResponse> UpdateAgentDefinitionAsync(string agentId, UpdateAgentDefinitionRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));
        return await SendJsonAsync<AgentDefinitionResponse>(HttpMethod.Put, $"/agents/{Uri.EscapeDataString(agentId)}/definition", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    // ── Agent Runs (additional) ─────────────────────────────────────────────

    /// <summary>Searches agent runs with filter criteria.</summary>
    public async Task<AgentTraceSearchResponse> SearchAgentRunsAsync(AgentTraceSearchRequest body, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<AgentTraceSearchResponse>(HttpMethod.Post, "/agents/runs/search", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Cancels an in-progress agent run.</summary>
    public async Task<AgentRunResponse> CancelAgentRunAsync(string runId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(runId)) throw new ArgumentException("runId is required", nameof(runId));
        return await SendJsonAsync<AgentRunResponse>(HttpMethod.Post, $"/agents/runs/{Uri.EscapeDataString(runId)}/cancel", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    // ── Agent Input Uploads ─────────────────────────────────────────────────

    /// <summary>Uploads an input file for an agent run.</summary>
    public async Task<UploadAgentInputResponse> UploadAgentInputAsync(
        string agentId,
        byte[] fileBytes,
        string fileName,
        string? mimeType = null,
        string? title = null,
        IReadOnlyDictionary<string, object?>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));
        if (fileBytes is null || fileBytes.Length == 0) throw new ArgumentException("fileBytes must be non-empty", nameof(fileBytes));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("fileName is required", nameof(fileName));

        using var content = BuildMultipartContent(fileBytes, fileName, title, metadata, mimeType);
        var raw = await DoUploadAsync($"/agents/{Uri.EscapeDataString(agentId)}/upload-input", content, cancellationToken).ConfigureAwait(false);
        var parsed = JsonSerializer.Deserialize<UploadAgentInputResponse>(raw, JsonOptions);
        return parsed ?? new UploadAgentInputResponse();
    }

    /// <summary>Checks the status of an input upload.</summary>
    public async Task<UploadAgentInputResponse> GetAgentInputUploadStatusAsync(string agentId, string uploadId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));
        if (string.IsNullOrWhiteSpace(uploadId)) throw new ArgumentException("uploadId is required", nameof(uploadId));
        return await SendJsonAsync<UploadAgentInputResponse>(HttpMethod.Get, $"/agents/{Uri.EscapeDataString(agentId)}/input-uploads/{Uri.EscapeDataString(uploadId)}", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    // ── Agent AI Assistant ──────────────────────────────────────────────────

    /// <summary>Uses the AI assistant to generate step configurations for an agent.</summary>
    public async Task<GenerateAgentStepsResponse> GenerateAgentStepsAsync(string agentId, GenerateAgentStepsRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));
        return await SendJsonAsync<GenerateAgentStepsResponse>(HttpMethod.Post, $"/agents/{Uri.EscapeDataString(agentId)}/ai-assistant/generate-steps", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Uses the AI assistant to generate configuration for a single step.</summary>
    public async Task<GenerateStepConfigResponse> GenerateStepConfigAsync(string agentId, GenerateStepConfigRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));
        return await SendJsonAsync<GenerateStepConfigResponse>(HttpMethod.Post, $"/agents/{Uri.EscapeDataString(agentId)}/ai-assistant/step-config", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Retrieves AI assistant conversation history for an agent.</summary>
    public async Task<AiConversationHistoryResponse> GetAgentAiConversationHistoryAsync(string agentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));
        return await SendJsonAsync<AiConversationHistoryResponse>(HttpMethod.Get, $"/agents/{Uri.EscapeDataString(agentId)}/ai-assistant/conversations", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Marks an AI assistant suggestion as accepted or rejected.</summary>
    public async Task MarkAgentAiSuggestionAsync(string agentId, string conversationId, MarkAiSuggestionRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));
        if (string.IsNullOrWhiteSpace(conversationId)) throw new ArgumentException("conversationId is required", nameof(conversationId));
        await SendNoContentAsync(HttpPatch, $"/agents/{Uri.EscapeDataString(agentId)}/ai-assistant/{Uri.EscapeDataString(conversationId)}", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    // ── Agent Evaluations ───────────────────────────────────────────────────

    /// <summary>Lists evaluation criteria for an agent.</summary>
    public async Task<List<EvaluationCriteriaResponse>> ListEvaluationCriteriaAsync(string agentId, int? page = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));
        var query = PaginationQuery(page, limit);
        return await SendJsonAsync<List<EvaluationCriteriaResponse>>(HttpMethod.Get, $"/agents/{Uri.EscapeDataString(agentId)}/evaluation-criteria", query, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Creates new evaluation criteria for an agent.</summary>
    public async Task<EvaluationCriteriaResponse> CreateEvaluationCriteriaAsync(string agentId, CreateEvaluationCriteriaRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));
        return await SendJsonAsync<EvaluationCriteriaResponse>(HttpMethod.Post, $"/agents/{Uri.EscapeDataString(agentId)}/evaluation-criteria", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Retrieves evaluation criteria by ID.</summary>
    public async Task<EvaluationCriteriaResponse> GetEvaluationCriteriaAsync(string criteriaId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(criteriaId)) throw new ArgumentException("criteriaId is required", nameof(criteriaId));
        return await SendJsonAsync<EvaluationCriteriaResponse>(HttpMethod.Get, $"/agents/evaluation-criteria/{Uri.EscapeDataString(criteriaId)}", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Updates evaluation criteria.</summary>
    public async Task<EvaluationCriteriaResponse> UpdateEvaluationCriteriaAsync(string criteriaId, UpdateEvaluationCriteriaRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(criteriaId)) throw new ArgumentException("criteriaId is required", nameof(criteriaId));
        return await SendJsonAsync<EvaluationCriteriaResponse>(HttpPatch, $"/agents/evaluation-criteria/{Uri.EscapeDataString(criteriaId)}", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Deletes evaluation criteria.</summary>
    public async Task DeleteEvaluationCriteriaAsync(string criteriaId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(criteriaId)) throw new ArgumentException("criteriaId is required", nameof(criteriaId));
        await SendNoContentAsync(HttpMethod.Delete, $"/agents/evaluation-criteria/{Uri.EscapeDataString(criteriaId)}", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Retrieves the summary for evaluation criteria.</summary>
    public async Task<EvaluationResultSummaryResponse> GetEvaluationCriteriaSummaryAsync(string criteriaId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(criteriaId)) throw new ArgumentException("criteriaId is required", nameof(criteriaId));
        return await SendJsonAsync<EvaluationResultSummaryResponse>(HttpMethod.Get, $"/agents/evaluation-criteria/{Uri.EscapeDataString(criteriaId)}/summary", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Lists evaluation results for criteria.</summary>
    public async Task<EvaluationResultListResponse> ListEvaluationResultsAsync(string criteriaId, int? page = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(criteriaId)) throw new ArgumentException("criteriaId is required", nameof(criteriaId));
        var query = PaginationQuery(page, limit);
        return await SendJsonAsync<EvaluationResultListResponse>(HttpMethod.Get, $"/agents/evaluation-criteria/{Uri.EscapeDataString(criteriaId)}/results", query, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Creates a new evaluation result for criteria.</summary>
    public async Task<EvaluationResultResponse> CreateEvaluationResultAsync(string criteriaId, CreateEvaluationResultRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(criteriaId)) throw new ArgumentException("criteriaId is required", nameof(criteriaId));
        return await SendJsonAsync<EvaluationResultResponse>(HttpMethod.Post, $"/agents/evaluation-criteria/{Uri.EscapeDataString(criteriaId)}/results", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Lists runs compatible with evaluation criteria.</summary>
    public async Task<CompatibleRunListResponse> ListCompatibleRunsAsync(string criteriaId, int? page = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(criteriaId)) throw new ArgumentException("criteriaId is required", nameof(criteriaId));
        var query = PaginationQuery(page, limit);
        return await SendJsonAsync<CompatibleRunListResponse>(HttpMethod.Get, $"/agents/evaluation-criteria/{Uri.EscapeDataString(criteriaId)}/compatible-runs", query, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Tests a draft evaluation criteria without persisting.</summary>
    public async Task<TestDraftEvaluationResponse> TestDraftEvaluationAsync(string agentId, TestDraftEvaluationRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));
        return await SendJsonAsync<TestDraftEvaluationResponse>(HttpMethod.Post, $"/agents/{Uri.EscapeDataString(agentId)}/evaluation-criteria/test-draft", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Lists all evaluation results for an agent.</summary>
    public async Task<EvaluationResultWithCriteriaListResponse> ListAgentEvaluationResultsAsync(string agentId, int? page = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));
        var query = PaginationQuery(page, limit);
        return await SendJsonAsync<EvaluationResultWithCriteriaListResponse>(HttpMethod.Get, $"/agents/{Uri.EscapeDataString(agentId)}/evaluation-results", query, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Lists evaluation results for a specific run.</summary>
    public async Task<EvaluationResultWithCriteriaListResponse> ListRunEvaluationResultsAsync(string agentId, string runId, int? page = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));
        if (string.IsNullOrWhiteSpace(runId)) throw new ArgumentException("runId is required", nameof(runId));
        var query = PaginationQuery(page, limit);
        return await SendJsonAsync<EvaluationResultWithCriteriaListResponse>(HttpMethod.Get, $"/agents/{Uri.EscapeDataString(agentId)}/runs/{Uri.EscapeDataString(runId)}/evaluation-results", query, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Lists evaluation run summaries for an agent.</summary>
    public async Task<EvaluationRunSummaryListResponse> ListEvaluationRunsAsync(string agentId, int? page = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));
        var query = PaginationQuery(page, limit);
        return await SendJsonAsync<EvaluationRunSummaryListResponse>(HttpMethod.Get, $"/agents/{Uri.EscapeDataString(agentId)}/evaluation-runs", query, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Retrieves a summary of non-manual evaluation results.</summary>
    public async Task<NonManualEvaluationSummaryResponse> GetNonManualEvaluationSummaryAsync(string? agentId = null, CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["agent_id"] = string.IsNullOrWhiteSpace(agentId) ? null : agentId,
        };
        return await SendJsonAsync<NonManualEvaluationSummaryResponse>(HttpMethod.Get, "/agents/evaluation-results/non-manual-summary", query, body: null, cancellationToken).ConfigureAwait(false);
    }

    // ── Knowledge Bases ─────────────────────────────────────────────────────

    /// <summary>Lists knowledge bases.</summary>
    public async Task<KnowledgeBaseListResponse> ListKnowledgeBasesAsync(int? page = null, int? limit = null, string? sort = null, string? order = null, CancellationToken cancellationToken = default)
    {
        var query = PaginationQuery(page, limit, sort, order);
        return await SendJsonAsync<KnowledgeBaseListResponse>(HttpMethod.Get, "/knowledge_bases", query, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Creates a new knowledge base.</summary>
    public async Task<KnowledgeBaseResponse> CreateKnowledgeBaseAsync(CreateKnowledgeBaseRequest body, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<KnowledgeBaseResponse>(HttpMethod.Post, "/knowledge_bases", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Retrieves a knowledge base by ID.</summary>
    public async Task<KnowledgeBaseResponse> GetKnowledgeBaseAsync(string knowledgeBaseId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(knowledgeBaseId)) throw new ArgumentException("knowledgeBaseId is required", nameof(knowledgeBaseId));
        return await SendJsonAsync<KnowledgeBaseResponse>(HttpMethod.Get, $"/knowledge_bases/{Uri.EscapeDataString(knowledgeBaseId)}", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Updates a knowledge base.</summary>
    public async Task<KnowledgeBaseResponse> UpdateKnowledgeBaseAsync(string knowledgeBaseId, UpdateKnowledgeBaseRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(knowledgeBaseId)) throw new ArgumentException("knowledgeBaseId is required", nameof(knowledgeBaseId));
        return await SendJsonAsync<KnowledgeBaseResponse>(HttpMethod.Put, $"/knowledge_bases/{Uri.EscapeDataString(knowledgeBaseId)}", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Deletes a knowledge base.</summary>
    public async Task DeleteKnowledgeBaseAsync(string knowledgeBaseId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(knowledgeBaseId)) throw new ArgumentException("knowledgeBaseId is required", nameof(knowledgeBaseId));
        await SendNoContentAsync(HttpMethod.Delete, $"/knowledge_bases/{Uri.EscapeDataString(knowledgeBaseId)}", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    // ── Memory Banks ────────────────────────────────────────────────────────

    /// <summary>Lists memory banks.</summary>
    public async Task<MemoryBankListResponse> ListMemoryBanksAsync(int? page = null, int? limit = null, string? sort = null, string? order = null, CancellationToken cancellationToken = default)
    {
        var query = PaginationQuery(page, limit, sort, order);
        return await SendJsonAsync<MemoryBankListResponse>(HttpMethod.Get, "/memory_banks", query, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Creates a new memory bank.</summary>
    public async Task<MemoryBankResponse> CreateMemoryBankAsync(CreateMemoryBankRequest body, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<MemoryBankResponse>(HttpMethod.Post, "/memory_banks", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Retrieves a memory bank by ID.</summary>
    public async Task<MemoryBankResponse> GetMemoryBankAsync(string memoryBankId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(memoryBankId)) throw new ArgumentException("memoryBankId is required", nameof(memoryBankId));
        return await SendJsonAsync<MemoryBankResponse>(HttpMethod.Get, $"/memory_banks/{Uri.EscapeDataString(memoryBankId)}", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Updates a memory bank.</summary>
    public async Task<MemoryBankResponse> UpdateMemoryBankAsync(string memoryBankId, UpdateMemoryBankRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(memoryBankId)) throw new ArgumentException("memoryBankId is required", nameof(memoryBankId));
        return await SendJsonAsync<MemoryBankResponse>(HttpMethod.Put, $"/memory_banks/{Uri.EscapeDataString(memoryBankId)}", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Deletes a memory bank.</summary>
    public async Task DeleteMemoryBankAsync(string memoryBankId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(memoryBankId)) throw new ArgumentException("memoryBankId is required", nameof(memoryBankId));
        await SendNoContentAsync(HttpMethod.Delete, $"/memory_banks/{Uri.EscapeDataString(memoryBankId)}", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Lists agents that use a memory bank.</summary>
    public async Task<JsonElement> GetAgentsUsingMemoryBankAsync(string memoryBankId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(memoryBankId)) throw new ArgumentException("memoryBankId is required", nameof(memoryBankId));
        return await SendRawAsync(HttpMethod.Get, $"/memory_banks/{Uri.EscapeDataString(memoryBankId)}/agents", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Retrieves statistics for a memory bank.</summary>
    public async Task<JsonElement> GetMemoryBankStatsAsync(string memoryBankId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(memoryBankId)) throw new ArgumentException("memoryBankId is required", nameof(memoryBankId));
        return await SendRawAsync(HttpMethod.Get, $"/memory_banks/{Uri.EscapeDataString(memoryBankId)}/stats", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Triggers compaction for a memory bank.</summary>
    public async Task CompactMemoryBankAsync(string memoryBankId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(memoryBankId)) throw new ArgumentException("memoryBankId is required", nameof(memoryBankId));
        await SendNoContentAsync(HttpMethod.Post, $"/memory_banks/{Uri.EscapeDataString(memoryBankId)}/compact", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Deletes the source associated with a memory bank.</summary>
    public async Task DeleteMemoryBankSourceAsync(string memoryBankId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(memoryBankId)) throw new ArgumentException("memoryBankId is required", nameof(memoryBankId));
        await SendNoContentAsync(HttpMethod.Delete, $"/memory_banks/{Uri.EscapeDataString(memoryBankId)}/source", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Tests compaction for a memory bank.</summary>
    public async Task<CompactionTestResponse> TestMemoryBankCompactionAsync(string memoryBankId, TestCompactionRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(memoryBankId)) throw new ArgumentException("memoryBankId is required", nameof(memoryBankId));
        return await SendJsonAsync<CompactionTestResponse>(HttpMethod.Post, $"/memory_banks/{Uri.EscapeDataString(memoryBankId)}/test-compaction", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Tests a compaction prompt without a memory bank.</summary>
    public async Task<CompactionTestResponse> TestCompactionPromptStandaloneAsync(StandaloneTestCompactionRequest body, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<CompactionTestResponse>(HttpMethod.Post, "/memory_banks/test-compaction", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Lists available memory bank templates.</summary>
    public async Task<JsonElement> ListMemoryBankTemplatesAsync(CancellationToken cancellationToken = default)
    {
        return await SendRawAsync(HttpMethod.Get, "/memory_banks/templates", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    // ── Memory Bank AI Assistant ────────────────────────────────────────────

    /// <summary>Uses the AI assistant to generate memory bank configuration.</summary>
    public async Task<MemoryBankAiAssistantResponse> GenerateMemoryBankConfigAsync(MemoryBankAiAssistantRequest body, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<MemoryBankAiAssistantResponse>(HttpMethod.Post, "/memory_banks/ai-assistant", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Retrieves the last AI assistant conversation for memory banks.</summary>
    public async Task<MemoryBankLastConversationResponse> GetMemoryBankAiLastConversationAsync(CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<MemoryBankLastConversationResponse>(HttpMethod.Get, "/memory_banks/ai-assistant/last-conversation", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Accepts an AI-generated memory bank suggestion.</summary>
    public async Task<JsonElement> AcceptMemoryBankAiSuggestionAsync(string conversationId, MemoryBankAcceptRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(conversationId)) throw new ArgumentException("conversationId is required", nameof(conversationId));
        return await SendRawAsync(HttpPatch, $"/memory_banks/ai-assistant/{Uri.EscapeDataString(conversationId)}", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    // ── Sources (additional) ────────────────────────────────────────────────

    /// <summary>Creates a new source.</summary>
    public async Task<SourceResponse> CreateSourceAsync(CreateSourceRequest body, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<SourceResponse>(HttpMethod.Post, "/sources", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Retrieves a source by ID.</summary>
    public async Task<SourceResponse> GetSourceAsync(string sourceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("sourceId is required", nameof(sourceId));
        return await SendJsonAsync<SourceResponse>(HttpMethod.Get, $"/sources/{Uri.EscapeDataString(sourceId)}", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Updates a source.</summary>
    public async Task<SourceResponse> UpdateSourceAsync(string sourceId, UpdateSourceRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("sourceId is required", nameof(sourceId));
        return await SendJsonAsync<SourceResponse>(HttpMethod.Put, $"/sources/{Uri.EscapeDataString(sourceId)}", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Deletes a source.</summary>
    public async Task DeleteSourceAsync(string sourceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("sourceId is required", nameof(sourceId));
        await SendNoContentAsync(HttpMethod.Delete, $"/sources/{Uri.EscapeDataString(sourceId)}", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Submits inline text content to a source.</summary>
    public async Task<FileUploadResponse> UploadInlineTextToSourceAsync(string sourceConnectionId, InlineTextUploadRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceConnectionId)) throw new ArgumentException("sourceConnectionId is required", nameof(sourceConnectionId));
        return await SendJsonAsync<FileUploadResponse>(HttpMethod.Post, $"/sources/{Uri.EscapeDataString(sourceConnectionId)}", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    // ── Source Exports ──────────────────────────────────────────────────────

    /// <summary>Lists exports for a source.</summary>
    public async Task<ExportListResponse> ListSourceExportsAsync(string sourceId, int? page = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("sourceId is required", nameof(sourceId));
        var query = PaginationQuery(page, limit);
        return await SendJsonAsync<ExportListResponse>(HttpMethod.Get, $"/sources/{Uri.EscapeDataString(sourceId)}/exports", query, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Creates a new export for a source.</summary>
    public async Task<ExportResponse> CreateSourceExportAsync(string sourceId, CreateExportRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("sourceId is required", nameof(sourceId));
        return await SendJsonAsync<ExportResponse>(HttpMethod.Post, $"/sources/{Uri.EscapeDataString(sourceId)}/exports", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Retrieves a source export by ID.</summary>
    public async Task<ExportResponse> GetSourceExportAsync(string sourceId, string exportId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("sourceId is required", nameof(sourceId));
        if (string.IsNullOrWhiteSpace(exportId)) throw new ArgumentException("exportId is required", nameof(exportId));
        return await SendJsonAsync<ExportResponse>(HttpMethod.Get, $"/sources/{Uri.EscapeDataString(sourceId)}/exports/{Uri.EscapeDataString(exportId)}", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Cancels a source export.</summary>
    public async Task<ExportResponse> CancelSourceExportAsync(string sourceId, string exportId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("sourceId is required", nameof(sourceId));
        if (string.IsNullOrWhiteSpace(exportId)) throw new ArgumentException("exportId is required", nameof(exportId));
        return await SendJsonAsync<ExportResponse>(HttpMethod.Post, $"/sources/{Uri.EscapeDataString(sourceId)}/exports/{Uri.EscapeDataString(exportId)}/cancel", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Deletes a source export.</summary>
    public async Task DeleteSourceExportAsync(string sourceId, string exportId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("sourceId is required", nameof(sourceId));
        if (string.IsNullOrWhiteSpace(exportId)) throw new ArgumentException("exportId is required", nameof(exportId));
        await SendNoContentAsync(HttpMethod.Delete, $"/sources/{Uri.EscapeDataString(sourceId)}/exports/{Uri.EscapeDataString(exportId)}", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Downloads a source export. Returns the raw HTTP response so the caller can stream the body.
    /// The caller must dispose the returned <see cref="HttpResponseMessage"/>.
    /// </summary>
    public async Task<HttpResponseMessage> DownloadSourceExportAsync(string sourceId, string exportId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("sourceId is required", nameof(sourceId));
        if (string.IsNullOrWhiteSpace(exportId)) throw new ArgumentException("exportId is required", nameof(exportId));

        var url = BuildUri($"/sources/{Uri.EscapeDataString(sourceId)}/exports/{Uri.EscapeDataString(exportId)}/download", query: null);
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.TryAddWithoutValidation(_apiKeyHeader, _apiKey);
        ApplyDefaultHeaders(req);

        var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

        if (!resp.IsSuccessStatusCode)
        {
            var responseBody = await ReadBodyAsync(resp).ConfigureAwait(false);
            resp.Dispose();
            ThrowApiError(resp.StatusCode, req.Method.Method, url, responseBody);
        }

        return resp;
    }

    /// <summary>Estimates the cost/size of a source export.</summary>
    public async Task<EstimateExportResponse> EstimateSourceExportAsync(string sourceId, EstimateExportRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("sourceId is required", nameof(sourceId));
        return await SendJsonAsync<EstimateExportResponse>(HttpMethod.Post, $"/sources/{Uri.EscapeDataString(sourceId)}/exports/estimate", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    // ── Source Embedding Migrations ─────────────────────────────────────────

    /// <summary>Retrieves the embedding migration status for a source.</summary>
    public async Task<SourceEmbeddingMigrationResponse> GetSourceEmbeddingMigrationAsync(string sourceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("sourceId is required", nameof(sourceId));
        return await SendJsonAsync<SourceEmbeddingMigrationResponse>(HttpMethod.Get, $"/sources/{Uri.EscapeDataString(sourceId)}/embedding-migration", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Starts an embedding migration for a source.</summary>
    public async Task<SourceEmbeddingMigrationResponse> StartSourceEmbeddingMigrationAsync(string sourceId, StartSourceEmbeddingMigrationRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("sourceId is required", nameof(sourceId));
        return await SendJsonAsync<SourceEmbeddingMigrationResponse>(HttpMethod.Post, $"/sources/{Uri.EscapeDataString(sourceId)}/embedding-migration", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Cancels an in-progress embedding migration.</summary>
    public async Task<SourceEmbeddingMigrationResponse> CancelSourceEmbeddingMigrationAsync(string sourceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("sourceId is required", nameof(sourceId));
        return await SendJsonAsync<SourceEmbeddingMigrationResponse>(HttpMethod.Post, $"/sources/{Uri.EscapeDataString(sourceId)}/embedding-migration/cancel", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    // ── Content (additional) ────────────────────────────────────────────────

    /// <summary>Replaces content with inline text.</summary>
    public async Task<ContentFileUploadResponse> ReplaceContentWithInlineTextAsync(string contentVersionId, InlineTextReplaceRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(contentVersionId)) throw new ArgumentException("contentVersionId is required", nameof(contentVersionId));
        return await SendJsonAsync<ContentFileUploadResponse>(HttpMethod.Put, $"/contents/{Uri.EscapeDataString(contentVersionId)}", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    // ── Solutions ───────────────────────────────────────────────────────────

    /// <summary>Lists solutions.</summary>
    public async Task<SolutionListResponse> ListSolutionsAsync(int? page = null, int? limit = null, string? sort = null, string? order = null, CancellationToken cancellationToken = default)
    {
        var query = PaginationQuery(page, limit, sort, order);
        return await SendJsonAsync<SolutionListResponse>(HttpMethod.Get, "/solutions", query, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Creates a new solution.</summary>
    public async Task<SolutionResponse> CreateSolutionAsync(CreateSolutionRequest body, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<SolutionResponse>(HttpMethod.Post, "/solutions", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Retrieves a solution by ID.</summary>
    public async Task<SolutionResponse> GetSolutionAsync(string solutionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(solutionId)) throw new ArgumentException("solutionId is required", nameof(solutionId));
        return await SendJsonAsync<SolutionResponse>(HttpMethod.Get, $"/solutions/{Uri.EscapeDataString(solutionId)}", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Updates a solution.</summary>
    public async Task<SolutionResponse> UpdateSolutionAsync(string solutionId, UpdateSolutionRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(solutionId)) throw new ArgumentException("solutionId is required", nameof(solutionId));
        return await SendJsonAsync<SolutionResponse>(HttpPatch, $"/solutions/{Uri.EscapeDataString(solutionId)}", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Deletes a solution.</summary>
    public async Task DeleteSolutionAsync(string solutionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(solutionId)) throw new ArgumentException("solutionId is required", nameof(solutionId));
        await SendNoContentAsync(HttpMethod.Delete, $"/solutions/{Uri.EscapeDataString(solutionId)}", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Links agents to a solution.</summary>
    public async Task<SolutionResponse> LinkAgentsToSolutionAsync(string solutionId, LinkResourcesRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(solutionId)) throw new ArgumentException("solutionId is required", nameof(solutionId));
        return await SendJsonAsync<SolutionResponse>(HttpMethod.Post, $"/solutions/{Uri.EscapeDataString(solutionId)}/agents", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Unlinks agents from a solution.</summary>
    public async Task<SolutionResponse> UnlinkAgentsFromSolutionAsync(string solutionId, UnlinkResourcesRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(solutionId)) throw new ArgumentException("solutionId is required", nameof(solutionId));
        return await SendJsonAsync<SolutionResponse>(HttpMethod.Delete, $"/solutions/{Uri.EscapeDataString(solutionId)}/agents", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Links knowledge bases to a solution.</summary>
    public async Task<SolutionResponse> LinkKnowledgeBasesToSolutionAsync(string solutionId, LinkResourcesRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(solutionId)) throw new ArgumentException("solutionId is required", nameof(solutionId));
        return await SendJsonAsync<SolutionResponse>(HttpMethod.Post, $"/solutions/{Uri.EscapeDataString(solutionId)}/knowledge-bases", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Unlinks knowledge bases from a solution.</summary>
    public async Task<SolutionResponse> UnlinkKnowledgeBasesFromSolutionAsync(string solutionId, UnlinkResourcesRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(solutionId)) throw new ArgumentException("solutionId is required", nameof(solutionId));
        return await SendJsonAsync<SolutionResponse>(HttpMethod.Delete, $"/solutions/{Uri.EscapeDataString(solutionId)}/knowledge-bases", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Links source connections to a solution.</summary>
    public async Task<SolutionResponse> LinkSourceConnectionsToSolutionAsync(string solutionId, LinkResourcesRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(solutionId)) throw new ArgumentException("solutionId is required", nameof(solutionId));
        return await SendJsonAsync<SolutionResponse>(HttpMethod.Post, $"/solutions/{Uri.EscapeDataString(solutionId)}/source-connections", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Unlinks source connections from a solution.</summary>
    public async Task<SolutionResponse> UnlinkSourceConnectionsFromSolutionAsync(string solutionId, UnlinkResourcesRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(solutionId)) throw new ArgumentException("solutionId is required", nameof(solutionId));
        return await SendJsonAsync<SolutionResponse>(HttpMethod.Delete, $"/solutions/{Uri.EscapeDataString(solutionId)}/source-connections", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    // ── Solution Conversations ──────────────────────────────────────────────

    /// <summary>Lists conversations for a solution.</summary>
    public async Task<List<SolutionConversationResponse>> ListSolutionConversationsAsync(string solutionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(solutionId)) throw new ArgumentException("solutionId is required", nameof(solutionId));
        return await SendJsonAsync<List<SolutionConversationResponse>>(HttpMethod.Get, $"/solutions/{Uri.EscapeDataString(solutionId)}/conversations", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Adds a conversation turn to a solution.</summary>
    public async Task<SolutionConversationResponse> AddSolutionConversationTurnAsync(string solutionId, AddConversationTurnRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(solutionId)) throw new ArgumentException("solutionId is required", nameof(solutionId));
        return await SendJsonAsync<SolutionConversationResponse>(HttpMethod.Post, $"/solutions/{Uri.EscapeDataString(solutionId)}/conversations", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Marks a conversation turn as accepted or rejected.</summary>
    public async Task MarkSolutionConversationTurnAsync(string solutionId, string conversationId, MarkConversationTurnRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(solutionId)) throw new ArgumentException("solutionId is required", nameof(solutionId));
        if (string.IsNullOrWhiteSpace(conversationId)) throw new ArgumentException("conversationId is required", nameof(conversationId));
        await SendNoContentAsync(HttpPatch, $"/solutions/{Uri.EscapeDataString(solutionId)}/conversations/{Uri.EscapeDataString(conversationId)}", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    // ── Solution AI Assistant ───────────────────────────────────────────────

    /// <summary>Uses the AI assistant to generate a plan for a solution.</summary>
    public async Task<AiAssistantGenerateResponse> GenerateSolutionAiPlanAsync(string solutionId, AiAssistantGenerateRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(solutionId)) throw new ArgumentException("solutionId is required", nameof(solutionId));
        return await SendJsonAsync<AiAssistantGenerateResponse>(HttpMethod.Post, $"/solutions/{Uri.EscapeDataString(solutionId)}/ai-assistant/generate", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Uses the AI assistant to generate a knowledge base plan for a solution.</summary>
    public async Task<AiAssistantGenerateResponse> GenerateSolutionAiKnowledgeBaseAsync(string solutionId, AiAssistantGenerateRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(solutionId)) throw new ArgumentException("solutionId is required", nameof(solutionId));
        return await SendJsonAsync<AiAssistantGenerateResponse>(HttpMethod.Post, $"/solutions/{Uri.EscapeDataString(solutionId)}/ai-assistant/knowledge-base", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Uses the AI assistant to generate a source plan for a solution.</summary>
    public async Task<AiAssistantGenerateResponse> GenerateSolutionAiSourceAsync(string solutionId, AiAssistantGenerateRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(solutionId)) throw new ArgumentException("solutionId is required", nameof(solutionId));
        return await SendJsonAsync<AiAssistantGenerateResponse>(HttpMethod.Post, $"/solutions/{Uri.EscapeDataString(solutionId)}/ai-assistant/source", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Accepts an AI-generated solution plan.</summary>
    public async Task<AiAssistantAcceptResponse> AcceptSolutionAiPlanAsync(string solutionId, string conversationId, AiAssistantAcceptRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(solutionId)) throw new ArgumentException("solutionId is required", nameof(solutionId));
        if (string.IsNullOrWhiteSpace(conversationId)) throw new ArgumentException("conversationId is required", nameof(conversationId));
        return await SendJsonAsync<AiAssistantAcceptResponse>(HttpMethod.Post, $"/solutions/{Uri.EscapeDataString(solutionId)}/ai-assistant/{Uri.EscapeDataString(conversationId)}/accept", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Declines an AI-generated solution plan.</summary>
    public async Task DeclineSolutionAiPlanAsync(string solutionId, string conversationId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(solutionId)) throw new ArgumentException("solutionId is required", nameof(solutionId));
        if (string.IsNullOrWhiteSpace(conversationId)) throw new ArgumentException("conversationId is required", nameof(conversationId));
        await SendNoContentAsync(HttpMethod.Post, $"/solutions/{Uri.EscapeDataString(solutionId)}/ai-assistant/{Uri.EscapeDataString(conversationId)}/decline", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    // ── Governance AI Assistant ──────────────────────────────────────────────

    /// <summary>Uses the governance AI assistant to generate a plan.</summary>
    public async Task<GovernanceAiAssistantResponse> GenerateGovernanceAiPlanAsync(GovernanceAiAssistantRequest body, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<GovernanceAiAssistantResponse>(HttpMethod.Post, "/governance/ai-assistant", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Lists governance AI assistant conversations.</summary>
    public async Task<List<GovernanceConversationResponse>> ListGovernanceAiConversationsAsync(CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<List<GovernanceConversationResponse>>(HttpMethod.Get, "/governance/ai-assistant/conversations", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Accepts a governance AI plan.</summary>
    public async Task<GovernanceAiAcceptResponse> AcceptGovernanceAiPlanAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(conversationId)) throw new ArgumentException("conversationId is required", nameof(conversationId));
        return await SendJsonAsync<GovernanceAiAcceptResponse>(HttpMethod.Post, $"/governance/ai-assistant/{Uri.EscapeDataString(conversationId)}/accept", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Declines a governance AI plan.</summary>
    public async Task DeclineGovernanceAiPlanAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(conversationId)) throw new ArgumentException("conversationId is required", nameof(conversationId));
        await SendNoContentAsync(HttpMethod.Post, $"/governance/ai-assistant/{Uri.EscapeDataString(conversationId)}/decline", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    // ── Alerts ──────────────────────────────────────────────────────────────

    /// <summary>Lists alerts.</summary>
    public async Task<JsonElement> ListAlertsAsync(int? page = null, int? limit = null, string? status = null, string? severity = null, CancellationToken cancellationToken = default)
    {
        var query = PaginationQuery(page, limit);
        query["status"] = string.IsNullOrWhiteSpace(status) ? null : status;
        query["severity"] = string.IsNullOrWhiteSpace(severity) ? null : severity;
        return await SendRawAsync(HttpMethod.Get, "/alerts", query, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Retrieves an alert by ID.</summary>
    public async Task<JsonElement> GetAlertAsync(string alertId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(alertId)) throw new ArgumentException("alertId is required", nameof(alertId));
        return await SendRawAsync(HttpMethod.Get, $"/alerts/{Uri.EscapeDataString(alertId)}", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Changes the status of an alert.</summary>
    public async Task<JsonElement> ChangeAlertStatusAsync(string alertId, ChangeStatusRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(alertId)) throw new ArgumentException("alertId is required", nameof(alertId));
        return await SendRawAsync(HttpMethod.Post, $"/alerts/{Uri.EscapeDataString(alertId)}/status", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Adds a comment to an alert.</summary>
    public async Task<JsonElement> AddAlertCommentAsync(string alertId, AddCommentRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(alertId)) throw new ArgumentException("alertId is required", nameof(alertId));
        return await SendRawAsync(HttpMethod.Post, $"/alerts/{Uri.EscapeDataString(alertId)}/comments", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Subscribes to an alert.</summary>
    public async Task<JsonElement> SubscribeToAlertAsync(string alertId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(alertId)) throw new ArgumentException("alertId is required", nameof(alertId));
        return await SendRawAsync(HttpMethod.Post, $"/alerts/{Uri.EscapeDataString(alertId)}/subscribe", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Unsubscribes from an alert.</summary>
    public async Task<JsonElement> UnsubscribeFromAlertAsync(string alertId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(alertId)) throw new ArgumentException("alertId is required", nameof(alertId));
        return await SendRawAsync(HttpMethod.Post, $"/alerts/{Uri.EscapeDataString(alertId)}/unsubscribe", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    // ── Alert Configs ───────────────────────────────────────────────────────

    /// <summary>Lists alert configurations.</summary>
    public async Task<JsonElement> ListAlertConfigsAsync(int? page = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        var query = PaginationQuery(page, limit);
        return await SendRawAsync(HttpMethod.Get, "/alerts/configs", query, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Creates a new alert configuration.</summary>
    public async Task<JsonElement> CreateAlertConfigAsync(CreateAlertConfigRequest body, CancellationToken cancellationToken = default)
    {
        return await SendRawAsync(HttpMethod.Post, "/alerts/configs", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Retrieves an alert configuration by ID.</summary>
    public async Task<JsonElement> GetAlertConfigAsync(string configId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(configId)) throw new ArgumentException("configId is required", nameof(configId));
        return await SendRawAsync(HttpMethod.Get, $"/alerts/configs/{Uri.EscapeDataString(configId)}", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Updates an alert configuration.</summary>
    public async Task<JsonElement> UpdateAlertConfigAsync(string configId, UpdateAlertConfigRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(configId)) throw new ArgumentException("configId is required", nameof(configId));
        return await SendRawAsync(HttpPatch, $"/alerts/configs/{Uri.EscapeDataString(configId)}", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Deletes an alert configuration.</summary>
    public async Task DeleteAlertConfigAsync(string configId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(configId)) throw new ArgumentException("configId is required", nameof(configId));
        await SendNoContentAsync(HttpMethod.Delete, $"/alerts/configs/{Uri.EscapeDataString(configId)}", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    // ── Alert Preferences ───────────────────────────────────────────────────

    /// <summary>Lists organization alert preferences.</summary>
    public async Task<OrganizationAlertPreferenceListResponse> ListOrganizationAlertPreferencesAsync(CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<OrganizationAlertPreferenceListResponse>(HttpMethod.Get, "/alerts/organization-preferences/list", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Updates an organization alert preference.</summary>
    public async Task<JsonElement> UpdateOrganizationAlertPreferenceAsync(string organizationId, string alertType, UpdateOrganizationAlertPreferenceRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(organizationId)) throw new ArgumentException("organizationId is required", nameof(organizationId));
        if (string.IsNullOrWhiteSpace(alertType)) throw new ArgumentException("alertType is required", nameof(alertType));
        return await SendRawAsync(HttpPatch, $"/alerts/organization-preferences/{Uri.EscapeDataString(organizationId)}/{Uri.EscapeDataString(alertType)}", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    // ── Models & Alerts ─────────────────────────────────────────────────────

    /// <summary>Lists model alerts.</summary>
    public async Task<JsonElement> ListModelAlertsAsync(int? page = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        var query = PaginationQuery(page, limit);
        return await SendRawAsync(HttpMethod.Get, "/models/alerts", query, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Marks all model alerts as read.</summary>
    public async Task MarkAllModelAlertsReadAsync(CancellationToken cancellationToken = default)
    {
        await SendNoContentAsync(HttpMethod.Post, "/models/alerts/mark-all-read", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Retrieves the count of unread model alerts.</summary>
    public async Task<JsonElement> GetUnreadModelAlertCountAsync(CancellationToken cancellationToken = default)
    {
        return await SendRawAsync(HttpMethod.Get, "/models/alerts/unread-count", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Marks a single model alert as read.</summary>
    public async Task MarkModelAlertReadAsync(string alertId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(alertId)) throw new ArgumentException("alertId is required", nameof(alertId));
        await SendNoContentAsync(HttpPatch, $"/models/alerts/{Uri.EscapeDataString(alertId)}/read", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Retrieves recommendations for a model.</summary>
    public async Task<JsonElement> GetModelRecommendationsAsync(string modelId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelId)) throw new ArgumentException("modelId is required", nameof(modelId));
        return await SendRawAsync(HttpMethod.Get, $"/models/{Uri.EscapeDataString(modelId)}/recommendations", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    // ── General Search ──────────────────────────────────────────────────────

    /// <summary>Performs a general search across resources.</summary>
    public async Task<JsonElement> SearchAsync(string? query = null, int? limit = null, string? entityType = null, CancellationToken cancellationToken = default)
    {
        var q = new Dictionary<string, string?>
        {
            ["query"] = string.IsNullOrWhiteSpace(query) ? null : query,
            ["limit"] = limit is > 0 ? limit.Value.ToString() : null,
            ["entity_type"] = string.IsNullOrWhiteSpace(entityType) ? null : entityType,
        };
        return await SendRawAsync(HttpMethod.Get, "/search", q, body: null, cancellationToken).ConfigureAwait(false);
    }

    // ── Top-Level AI Assistant ──────────────────────────────────────────────

    /// <summary>Submits feedback to the AI assistant.</summary>
    public async Task<AiAssistantFeedbackResponse> SubmitAiFeedbackAsync(AiAssistantFeedbackRequest body, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<AiAssistantFeedbackResponse>(HttpMethod.Post, "/ai-assistant/feedback", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Generates a knowledge base plan via the top-level AI assistant.</summary>
    public async Task<AiAssistantGenerateResponse> AiAssistantKnowledgeBaseAsync(AiAssistantGenerateRequest body, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<AiAssistantGenerateResponse>(HttpMethod.Post, "/ai-assistant/knowledge-base", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Generates a source plan via the top-level AI assistant.</summary>
    public async Task<AiAssistantGenerateResponse> AiAssistantSourceAsync(AiAssistantGenerateRequest body, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<AiAssistantGenerateResponse>(HttpMethod.Post, "/ai-assistant/source", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Generates a solution plan via the top-level AI assistant.</summary>
    public async Task<AiAssistantGenerateResponse> AiAssistantSolutionAsync(AiAssistantGenerateRequest body, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<AiAssistantGenerateResponse>(HttpMethod.Post, "/ai-assistant/solution", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Generates a memory bank plan via the top-level AI assistant.</summary>
    public async Task<MemoryBankAiAssistantResponse> AiAssistantMemoryBankAsync(MemoryBankAiAssistantRequest body, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<MemoryBankAiAssistantResponse>(HttpMethod.Post, "/ai-assistant/memory-bank", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Retrieves the last AI assistant memory bank conversation.</summary>
    public async Task<MemoryBankLastConversationResponse> GetAiAssistantMemoryBankHistoryAsync(CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<MemoryBankLastConversationResponse>(HttpMethod.Get, "/ai-assistant/memory-bank/last-conversation", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Accepts a top-level AI assistant plan.</summary>
    public async Task<AiAssistantAcceptResponse> AcceptAiAssistantPlanAsync(string conversationId, AiAssistantAcceptRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(conversationId)) throw new ArgumentException("conversationId is required", nameof(conversationId));
        return await SendJsonAsync<AiAssistantAcceptResponse>(HttpMethod.Post, $"/ai-assistant/{Uri.EscapeDataString(conversationId)}/accept", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Declines a top-level AI assistant plan.</summary>
    public async Task DeclineAiAssistantPlanAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(conversationId)) throw new ArgumentException("conversationId is required", nameof(conversationId));
        await SendNoContentAsync(HttpMethod.Post, $"/ai-assistant/{Uri.EscapeDataString(conversationId)}/decline", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Accepts a top-level AI memory bank suggestion.</summary>
    public async Task<JsonElement> AcceptAiMemoryBankSuggestionAsync(string conversationId, MemoryBankAcceptRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(conversationId)) throw new ArgumentException("conversationId is required", nameof(conversationId));
        return await SendRawAsync(HttpPatch, $"/ai-assistant/memory-bank/{Uri.EscapeDataString(conversationId)}", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    // ── High-Level Abstractions ─────────────────────────────────────────────

    /// <summary>
    /// Runs an agent in streaming mode and yields SSE events as they arrive.
    /// </summary>
    public async IAsyncEnumerable<AgentRunEvent> RunStreamingAgentAsync(
        string agentId,
        AgentRunStreamRequest body,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));

        var url = BuildUri($"/agents/{Uri.EscapeDataString(agentId)}/runs/stream", query: null);

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.TryAddWithoutValidation(_apiKeyHeader, _apiKey);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        ApplyDefaultHeaders(req);

        var json = JsonSerializer.Serialize(body, JsonOptions);
        req.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

        if (!resp.IsSuccessStatusCode)
        {
            var responseBody = await ReadBodyAsync(resp).ConfigureAwait(false);
            ThrowApiError(resp.StatusCode, req.Method.Method, url, responseBody);
        }

        // If the server returns JSON instead of SSE, emit a single done event.
        var mediaType = resp.Content.Headers.ContentType?.MediaType ?? string.Empty;
        if (mediaType.IndexOf("application/json", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            var responseBody = await ReadBodyAsync(resp).ConfigureAwait(false);
            AgentRunResponse? parsed = null;
            try { parsed = JsonSerializer.Deserialize<AgentRunResponse>(responseBody ?? string.Empty, JsonOptions); } catch { }
            yield return new AgentRunEvent { Event = "done", Data = responseBody ?? string.Empty, Run = parsed };
            yield break;
        }

        using var stream = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        string? currentEvent = null;
        var dataLines = new List<string>();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync().ConfigureAwait(false);
            if (line is null)
            {
                // End of stream — dispatch any pending event
                if (currentEvent is not null || dataLines.Count > 0)
                {
                    var data = string.Join("\n", dataLines);
                    AgentRunResponse? run = null;
                    try { run = JsonSerializer.Deserialize<AgentRunResponse>(data, JsonOptions); } catch { }
                    yield return new AgentRunEvent { Event = currentEvent ?? string.Empty, Data = data, Run = run };
                }
                yield break;
            }

            if (line.Length == 0)
            {
                // Empty line = dispatch event
                if (currentEvent is not null || dataLines.Count > 0)
                {
                    var data = string.Join("\n", dataLines);
                    AgentRunResponse? run = null;
                    if (!string.IsNullOrWhiteSpace(data))
                    {
                        try { run = JsonSerializer.Deserialize<AgentRunResponse>(data, JsonOptions); } catch { }
                    }
                    yield return new AgentRunEvent { Event = currentEvent ?? string.Empty, Data = data, Run = run };
                    currentEvent = null;
                    dataLines.Clear();
                }
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
    }

    /// <summary>
    /// Runs an agent and polls until it reaches a terminal status (completed or failed).
    /// </summary>
    public async Task<AgentRunResponse> RunAgentAndPollAsync(
        string agentId,
        AgentRunRequest body,
        TimeSpan? pollInterval = null,
        TimeSpan? timeout = null,
        bool includeStepOutputs = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));

        var run = await RunAgentAsync(agentId, body, cancellationToken);
        var interval = pollInterval ?? TimeSpan.FromSeconds(2);

        using var cts = timeout.HasValue
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
            : null;
        if (cts is not null) cts.CancelAfter(timeout!.Value);
        var ct = cts?.Token ?? cancellationToken;

        while (true)
        {
            switch (run.Status)
            {
                case "completed":
                case "failed":
                    return run;
            }

            await Task.Delay(interval, ct).ConfigureAwait(false);
            run = await GetAgentRunAsync(run.RunId!, includeStepOutputs, ct);
        }
    }

    // ── Shared upload helper ────────────────────────────────────────────────

    private async Task<string> DoUploadAsync(
        string path,
        MultipartFormDataContent content,
        CancellationToken cancellationToken)
    {
        var url = BuildUri(path, query: null);

        using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        req.Headers.TryAddWithoutValidation(_apiKeyHeader, _apiKey);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        ApplyDefaultHeaders(req);

        using var resp = await _http.SendAsync(req, cancellationToken).ConfigureAwait(false);
        var responseBody = await ReadBodyAsync(resp).ConfigureAwait(false);

        if (!resp.IsSuccessStatusCode)
        {
            ThrowApiError(resp.StatusCode, req.Method.Method, url, responseBody);
        }

        return responseBody ?? string.Empty;
    }
}

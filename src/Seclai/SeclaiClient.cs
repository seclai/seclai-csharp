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
    /// <summary>Serializer settings shared with <see cref="SeclaiTypedClient"/>.</summary>
    internal static JsonSerializerOptions TypedJsonOptions => JsonOptions;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly HttpMethod HttpPatch = new("PATCH");

    private readonly HttpClient _http;
    private readonly bool _ownsHttp;
    private readonly Uri _baseUri;
    private readonly AuthState _auth;
    private readonly Dictionary<string, string>? _defaultHeaders;
    private readonly string? _apiVersion;
    private readonly bool _allowUnknownApiVersion;

    /// <summary>Creates a new <see cref="SeclaiClient"/> from the given options.</summary>
    /// <exception cref="ConfigurationException">Thrown when credential options conflict (e.g. both API key and access token).</exception>
    public SeclaiClient(SeclaiClientOptions options)
    {
        if (options is null) throw new ArgumentNullException(nameof(options));

        _auth = SeclaiAuth.ResolveCredentialChain(options);

        var baseUrl = options.BaseUri
            ?? TryGetEnvUri("SECLAI_API_URL")
            ?? SeclaiClientOptions.DefaultBaseUri;

        _defaultHeaders = options.DefaultHeaders is not null
            ? new Dictionary<string, string>(options.DefaultHeaders)
            : null;

        _apiVersion = string.IsNullOrWhiteSpace(options.ApiVersion) ? null : options.ApiVersion;

        // DefaultHeaders can carry a Seclai-Version of its own. Validate whichever
        // value actually reaches the wire, not just the option — otherwise the
        // guard is one header away from being bypassed. The matching header is
        // also removed, because HttpHeaders APPENDS: leaving it would put two
        // Seclai-Version values on the request and let the server pick one.
        string? headerVersion = null;
        if (_defaultHeaders is not null)
        {
            foreach (var key in _defaultHeaders.Keys.ToList())
            {
                if (!string.Equals(key, "Seclai-Version", StringComparison.OrdinalIgnoreCase)) continue;
                headerVersion = _defaultHeaders[key];
                _defaultHeaders.Remove(key);
            }
        }
        if (headerVersion is not null)
        {
            _apiVersion = headerVersion;
        }

        _allowUnknownApiVersion = options.AllowUnknownApiVersion;
        if (_apiVersion is not null)
        {
            CheckApiVersion(_apiVersion, headerVersion is not null ? "DefaultHeaders[\"Seclai-Version\"]" : "ApiVersion");
        }

        if (options.HttpClient is not null)
        {
            _http = options.HttpClient;
            _ownsHttp = false;
            ResolveApiVersion();
        }
        else
        {
            _http = new HttpClient { Timeout = options.Timeout };
            _ownsHttp = true;
        }

        _baseUri = EnsureTrailingSlash(baseUrl);
    }

    /// <summary>
    /// Typed variants of the methods that otherwise return raw JSON.
    /// </summary>
    /// <remarks>
    /// Opt-in. The methods on the client itself keep returning
    /// <see cref="JsonElement"/> so existing call sites are unaffected; the same
    /// endpoints are available here deserialized into models. Both issue exactly
    /// the same request — only the return type differs.
    /// <code>
    /// var raw   = await client.SearchAsync("q");        // JsonElement
    /// var typed = await client.Typed.SearchAsync("q");  // SearchResponse
    /// </code>
    /// </remarks>
    public SeclaiTypedClient Typed => _typed ??= new SeclaiTypedClient(this);

    private SeclaiTypedClient? _typed;

    internal Task<T> SendTypedAsync<T>(HttpMethod method, string path, Dictionary<string, string?>? query, object? body, CancellationToken cancellationToken)
        => SendJsonAsync<T>(method, path, query, body, cancellationToken);

    internal Dictionary<string, string?> TypedPaginationQuery(int? page, int? limit, string? sort = null, string? order = null)
        => PaginationQuery(page, limit, sort, order);

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
        return await SendJsonAsync<SourceListResponse>(HttpMethod.Get, "/sources", query, body: null, cancellationToken).ConfigureAwait(false);
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
        await ApplyHeadersAsync(req, cancellationToken, "text/event-stream", "application/json").ConfigureAwait(false);

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

    /// <summary>Cancels an agent run.</summary>
    [Obsolete("This never deleted anything - the endpoint it calls is documented as \"Cancel an agent run\", and the API has no delete-a-run operation. Use CancelAgentRunAsync instead.")]
    public Task<AgentRunResponse> DeleteAgentRunAsync(string runId, CancellationToken cancellationToken = default)
        => CancelAgentRunAsync(runId, cancellationToken);

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
        var content = BuildMultipartShell(title, metadata);
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            ResolveContentType(fileName, mimeType));
        content.Add(fileContent, "file", fileName);
        return content;
    }

    /// <remarks>
    /// Disposing the returned <see cref="MultipartFormDataContent"/> (or the
    /// <see cref="HttpRequestMessage"/> that carries it) will dispose the
    /// underlying <see cref="StreamContent"/> and, consequently, the supplied
    /// <paramref name="fileStream"/>. Callers should <b>not</b> attempt to
    /// reuse the stream after the upload completes.
    /// </remarks>
    private MultipartFormDataContent BuildMultipartContent(
        Stream fileStream,
        string fileName,
        string? title,
        IReadOnlyDictionary<string, object?>? metadata,
        string? mimeType)
    {
        var content = BuildMultipartShell(title, metadata);
        var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(
            ResolveContentType(fileName, mimeType));
        content.Add(streamContent, "file", fileName);
        return content;
    }

    private MultipartFormDataContent BuildMultipartShell(
        string? title,
        IReadOnlyDictionary<string, object?>? metadata)
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

    private async Task<T> SendJsonAsync<T>(HttpMethod method, string path, Dictionary<string, string?>? query, object? body, CancellationToken cancellationToken, bool expectBody = true, Dictionary<string, IEnumerable<string>?>? repeatedQuery = null)
    {
        var url = BuildUri(path, query, repeatedQuery);
        using var req = new HttpRequestMessage(method, url);

        await ApplyHeadersAsync(req, cancellationToken, "application/json").ConfigureAwait(false);

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

    // ── Version-gated lists ─────────────────────────────────────────────────
    // A gated endpoint answers a bare array or a keyed object by default, and
    // {data, pagination, ...extras} once the request resolves to 2026-07-27 or later.

    private async Task<List<T>> SendListAsync<T>(HttpMethod method, string path, Dictionary<string, string?>? query, CancellationToken cancellationToken)
    {
        var response = await SendRawResponseAsync(method, path, query, body: null, cancellationToken).ConfigureAwait(false);
        return ReadList<T>(response, legacyKey: null, method.Method, out _);
    }

    private async Task<TPage> SendPageAsync<TPage, TItem>(HttpMethod method, string path, Dictionary<string, string?>? query, object? body, string? legacyKey, CancellationToken cancellationToken)
        where TPage : class, IListPage<TItem>, new()
    {
        var response = await SendRawResponseAsync(method, path, query, body, cancellationToken).ConfigureAwait(false);
        return ReadPage<TPage, TItem>(response, legacyKey, method.Method);
    }

    internal TPage ReadPage<TPage, TItem>(RawResponse response, string? legacyKey, string method)
        where TPage : class, IListPage<TItem>, new()
    {
        var items = ReadList<TItem>(response, legacyKey, method, out var shape, out var raw);
        var page = new TPage();
        if (raw.ValueKind == JsonValueKind.Object)
        {
            // The reader owns the list keys: a `data` that is not an array must not fail the model.
            var rest = System.Text.Json.Nodes.JsonObject.Create(raw)!;
            rest.Remove("data");
            if (legacyKey is not null) rest.Remove(legacyKey);
            page = rest.Deserialize<TPage>(JsonOptions) ?? page;
        }
        page.Fill(items, shape);
        return page;
    }

    internal List<TItem> ReadList<TItem>(RawResponse response, string? legacyKey, string method, out ListShape shape)
        => ReadList<TItem>(response, legacyKey, method, out shape, out _);

    private static List<TItem> ReadList<TItem>(RawResponse response, string? legacyKey, string method, out ListShape shape, out JsonElement raw)
    {
        try
        {
            raw = response.ToJson();
        }
        catch (JsonException)
        {
            throw NotAList(method, response.Url, response.Text);
        }
        var items = ListElement(raw, legacyKey, method, response, out shape);
        return items.Deserialize<List<TItem>>(JsonOptions) ?? new List<TItem>();
    }

    // The item array of either shape. Anything else throws: an empty list would read as "no results".
    private static JsonElement ListElement(JsonElement raw, string? legacyKey, string method, RawResponse response, out ListShape shape)
    {
        shape = new ListShape();
        if (raw.ValueKind == JsonValueKind.Array) return raw;
        if (raw.ValueKind != JsonValueKind.Object) throw NotAList(method, response.Url, response.Text);

        var hasPaging = raw.TryGetProperty("pagination", out var paging) && paging.ValueKind == JsonValueKind.Object;
        if (hasPaging) shape.Pagination = paging.Deserialize<PaginationResponse>(JsonOptions);
        shape.Total = Counter(raw, paging, hasPaging, "total");
        shape.Page = Counter(raw, paging, hasPaging, "page");
        shape.Limit = Counter(raw, paging, hasPaging, "limit");

        var hasData = raw.TryGetProperty("data", out var data);
        if (hasData && data.ValueKind == JsonValueKind.Array)
        {
            shape.FromData = true;
            return data;
        }
        if (legacyKey is not null && raw.TryGetProperty(legacyKey, out var keyed) && keyed.ValueKind == JsonValueKind.Array)
        {
            return keyed;
        }
        if (hasData && data.ValueKind == JsonValueKind.Null)
        {
            shape.FromData = true;
            return EmptyArray;
        }
        throw NotAList(method, response.Url, response.Text);
    }

    private static readonly JsonElement EmptyArray = JsonDocument.Parse("[]").RootElement.Clone();

    // A counter from `pagination` when it states one there, else the flat field.
    private static int? Counter(JsonElement raw, JsonElement paging, bool hasPaging, string name)
    {
        if (hasPaging && paging.TryGetProperty(name, out var nested) && nested.ValueKind == JsonValueKind.Number && nested.TryGetInt32(out var inner)) return inner;
        if (raw.TryGetProperty(name, out var flat) && flat.ValueKind == JsonValueKind.Number && flat.TryGetInt32(out var outer)) return outer;
        return null;
    }

    private static ApiException NotAList(string method, Uri url, string? responseBody)
    {
        var message = $"seclai: expected a list from {method} {url}: an array, or an object carrying the items under `data` or the endpoint's own key."
            + (string.IsNullOrWhiteSpace(responseBody) ? " The response body was empty." : $" Got: {responseBody}");
        return new ApiException(message, HttpStatusCode.OK, method, url, responseBody);
    }

    private async Task SendNoContentAsync(HttpMethod method, string path, Dictionary<string, string?>? query, object? body, CancellationToken cancellationToken)
    {
        var url = BuildUri(path, query);
        using var req = new HttpRequestMessage(method, url);

        await ApplyHeadersAsync(req, cancellationToken, "application/json").ConfigureAwait(false);

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
        return (await SendRawResponseAsync(method, path, query, body, cancellationToken).ConfigureAwait(false)).ToJson();
    }

    private async Task<RawResponse> SendRawResponseAsync(HttpMethod method, string path, Dictionary<string, string?>? query, object? body, CancellationToken cancellationToken)
    {
        var url = BuildUri(path, query);
        using var req = new HttpRequestMessage(method, url);

        await ApplyHeadersAsync(req, cancellationToken, "application/json").ConfigureAwait(false);

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

        return new RawResponse(url, responseBody);
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
                    var parsed = JsonSerializer.Deserialize<HttpValidationError>(responseBody!, JsonOptions);
                    // Only attach when the body is actually an HttpValidationError. The agent
                    // import endpoints return AgentDefinitionImportErrorResponse on 422, which
                    // deserializes successfully into HttpValidationError but with Detail == null
                    // (because it doesn't carry a `detail` field). Leaving Validation null in
                    // that case lets callers decode ResponseBody into the correct type.
                    if (parsed?.Detail is not null)
                    {
                        validation = parsed;
                    }
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

    private Uri BuildUri(string path, Dictionary<string, string?>? query, Dictionary<string, IEnumerable<string>?>? repeatedQuery = null)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("path is required", nameof(path));
        var builder = new UriBuilder(new Uri(_baseUri, path));

        var parts = new List<string>();
        foreach (var kv in query ?? new Dictionary<string, string?>())
        {
            if (string.IsNullOrWhiteSpace(kv.Key) || string.IsNullOrWhiteSpace(kv.Value)) continue;
            parts.Add($"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}");
        }
        foreach (var kv in repeatedQuery ?? new Dictionary<string, IEnumerable<string>?>())
        {
            foreach (var value in kv.Value ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(value)) continue;
                parts.Add($"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(value)}");
            }
        }
        if (query is not null || repeatedQuery is not null)
        {
            builder.Query = string.Join("&", parts);
        }

        return builder.Uri;
    }

    private static async Task<string?> ReadBodyAsync(HttpResponseMessage response)
    {
        if (response.Content is null) return null;
        return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    }

    // DefaultHeaders first, then the headers the SDK sets, each replacing what is there,
    // so one value is sent for every header the SDK sets.
    private async Task ApplyHeadersAsync(HttpRequestMessage req, CancellationToken cancellationToken, params string[] accept)
    {
        foreach (var kv in _defaultHeaders ?? new Dictionary<string, string>())
        {
            SeclaiAuth.SetHeader(req.Headers, kv.Key, kv.Value);
        }
        if (accept.Length > 0)
        {
            SeclaiAuth.SetHeader(req.Headers, "Accept", string.Join(", ", accept));
        }
        await SeclaiAuth.ApplyAuthHeadersAsync(req.Headers, _auth, null, cancellationToken).ConfigureAwait(false);

        // Last, after every await: the supplied HttpClient's defaults can change while a token is fetched.
        // Omitted unless the caller opts in, so upgrading the SDK alone never changes the wire contract.
        var version = ResolveApiVersion();
        if (version is not null)
        {
            SeclaiAuth.SetHeader(req.Headers, "Seclai-Version", version);
        }
    }

    // The version this request will carry. A supplied HttpClient's defaults can change after
    // construction, so they are read and checked on every request.
    private string? ResolveApiVersion()
    {
        if (_apiVersion is not null || _ownsHttp) return _apiVersion;
        if (!_http.DefaultRequestHeaders.TryGetValues("Seclai-Version", out var values)) return null;
        var distinct = values.Distinct().ToList();
        if (distinct.Count != 1)
        {
            throw new ConfigurationException(
                $"HttpClient.DefaultRequestHeaders carries {distinct.Count} Seclai-Version values; set exactly one, or use SeclaiClientOptions.ApiVersion.");
        }
        CheckApiVersion(distinct[0], "HttpClient.DefaultRequestHeaders[\"Seclai-Version\"]");
        return distinct[0];
    }

    private void CheckApiVersion(string version, string via)
    {
        if (_allowUnknownApiVersion || Array.IndexOf(SeclaiApiVersion.Known, version) >= 0) return;
        throw new ConfigurationException(
            $"Unknown API version '{version}' (via {via}). This release was built against "
            + string.Join(", ", SeclaiApiVersion.Known)
            + ". A newer API version can change response shapes, which this client "
            + "would decode incorrectly rather than reject. Upgrade the SDK, or set "
            + "SeclaiClientOptions.AllowUnknownApiVersion to proceed anyway.");
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

    // ── Agent Export / Import ─────────────────────────────────────────────

    /// <summary>Exports an agent definition as a portable JSON snapshot.</summary>
    public async Task<AgentExportResponse> ExportAgentAsync(string agentId, bool download = true, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));
        var query = new Dictionary<string, string?> { ["download"] = download.ToString().ToLowerInvariant() };
        return await SendJsonAsync<AgentExportResponse>(HttpMethod.Get, $"/agents/{Uri.EscapeDataString(agentId)}/export", query, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Validates an <c>agent_definition</c> payload (same shape as <see cref="ExportAgentAsync"/>'s
    /// response) without creating or modifying any agent.
    ///
    /// Use this before <see cref="CreateAgentAsync"/> or <see cref="UpdateAgentAsync"/> with an
    /// <c>agent_definition</c> to surface <see cref="AgentImportPreviewResponse.UnresolvedRefs"/>
    /// — workflow references to knowledge bases, memory banks, source connections, or sub-agents
    /// that don't exist in the target account. Pass the returned ids back in
    /// <c>EntityRemap</c> on the commit call to substitute them.
    ///
    /// On HTTP 422 the response body is an <see cref="AgentDefinitionImportErrorResponse"/>
    /// listing each field error with a 1-indexed line/column anchored to a canonical
    /// <c>source</c> echo. It is delivered as <see cref="ApiValidationException"/> with the
    /// raw body in <see cref="ApiException.ResponseBody"/>; deserialize that into
    /// <see cref="AgentDefinitionImportErrorResponse"/> to read the structured fields.
    /// </summary>
    public async Task<AgentImportPreviewResponse> PreviewImportAgentAsync(AgentImportPreviewRequest body, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<AgentImportPreviewResponse>(HttpMethod.Post, "/agents/preview-import", query: null, body, cancellationToken).ConfigureAwait(false);
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
    /// <remarks>
    /// Cancellation is DELETE on the run resource — the API exposes no
    /// POST .../cancel route, and no operation that deletes a run. Rejected when
    /// the run has already reached a terminal state.
    /// </remarks>
    public async Task<AgentRunResponse> CancelAgentRunAsync(string runId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(runId)) throw new ArgumentException("runId is required", nameof(runId));
        return await SendJsonAsync<AgentRunResponse>(HttpMethod.Delete, $"/agents/runs/{Uri.EscapeDataString(runId)}", query: null, body: null, cancellationToken).ConfigureAwait(false);
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

    /// <summary>
    /// Gets the static attachment-reference contract for an agent — what files (if any)
    /// its definition expects on a run. Call this before staging uploads to learn whether
    /// the agent accepts files at all (<see cref="AgentAttachmentRefsApiResponse.RequiresUploads"/>)
    /// and which specific filenames, indexes, or glob patterns its templates reference.
    /// A run-time upload batch that does not satisfy every declared selector is rejected with HTTP 400.
    /// </summary>
    public async Task<AgentAttachmentRefsApiResponse> GetAgentAttachmentReferencesAsync(string agentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));
        return await SendJsonAsync<AgentAttachmentRefsApiResponse>(HttpMethod.Get, $"/agents/{Uri.EscapeDataString(agentId)}/attachment-references", query: null, body: null, cancellationToken).ConfigureAwait(false);
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
    /// <remarks>
    /// Deprecated: the API requires a <c>step_type</c> query parameter this
    /// signature cannot supply, so every call answers 422. Use the overload
    /// taking <see cref="AiConversationHistoryOptions"/>.
    /// </remarks>
    [Obsolete("The API requires step_type, which this overload cannot send. Use the AiConversationHistoryOptions overload.")]
    public Task<AiConversationHistoryResponse> GetAgentAiConversationHistoryAsync(string agentId, CancellationToken cancellationToken = default)
        => GetAgentAiConversationHistoryAsync(agentId, new AiConversationHistoryOptions(), cancellationToken);

    /// <summary>Gets the AI assistant conversation history for an agent.</summary>
    /// <remarks>
    /// <see cref="AiConversationHistoryOptions.StepType"/> is required by the API —
    /// the endpoint answers 422 without it.
    /// </remarks>
    public async Task<AiConversationHistoryResponse> GetAgentAiConversationHistoryAsync(string agentId, AiConversationHistoryOptions options, CancellationToken cancellationToken = default)
    {
        var stepType = options?.StepType;
        var stepId = options?.StepId;
        var limit = options?.Limit;
        var offset = options?.Offset;
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));
        // Leaving it unset only omits the parameter and defers to a 422 naming
        // the wire parameter, which is the failure this overload exists to avoid.
        if (string.IsNullOrWhiteSpace(stepType))
        {
            throw new ArgumentException(
                "StepType is required by the API; set e.g. new AiConversationHistoryOptions { StepType = \"llm\" }.",
                nameof(options));
        }
        var query = new Dictionary<string, string?>
        {
            ["step_type"] = string.IsNullOrWhiteSpace(stepType) ? null : stepType,
            ["step_id"] = string.IsNullOrWhiteSpace(stepId) ? null : stepId,
            ["limit"] = limit is > 0 ? limit.Value.ToString() : null,
            ["offset"] = offset is > 0 ? offset.Value.ToString() : null,
        };
        return await SendJsonAsync<AiConversationHistoryResponse>(HttpMethod.Get, $"/agents/{Uri.EscapeDataString(agentId)}/ai-assistant/conversations", query, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Marks an AI assistant suggestion as accepted or rejected.</summary>
    public async Task MarkAgentAiSuggestionAsync(string agentId, string conversationId, MarkAiSuggestionRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));
        if (string.IsNullOrWhiteSpace(conversationId)) throw new ArgumentException("conversationId is required", nameof(conversationId));
        await SendNoContentAsync(HttpPatch, $"/agents/{Uri.EscapeDataString(agentId)}/ai-assistant/{Uri.EscapeDataString(conversationId)}", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    // ── Agent Evaluations ───────────────────────────────────────────────────

    /// <summary>
    /// Lists evaluation criteria for an agent.
    /// </summary>
    /// <remarks>
    /// By default the endpoint returns every criterion and ignores <c>page</c> and
    /// <c>limit</c>. Once <see cref="SeclaiClientOptions.ApiVersion"/> is <c>2026-07-27</c>
    /// or later it returns one page, 20 items unless <c>limit</c> is passed. Use
    /// <see cref="ListEvaluationCriteriaPageAsync"/> for the page metadata.
    /// </remarks>
    public async Task<List<EvaluationCriteriaResponse>> ListEvaluationCriteriaAsync(string agentId, int? page = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        return (await ListEvaluationCriteriaPageAsync(agentId, page, limit, cancellationToken).ConfigureAwait(false)).Data
               ?? new List<EvaluationCriteriaResponse>();
    }

    /// <summary>Lists evaluation criteria for an agent with pagination metadata.</summary>
    /// <remarks>
    /// <see cref="EvaluationCriteriaListResponse.Pagination"/> is <c>null</c> when the
    /// endpoint answers with a bare array rather than the canonical envelope.
    /// </remarks>
    public async Task<EvaluationCriteriaListResponse> ListEvaluationCriteriaPageAsync(string agentId, int? page = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));
        var query = PaginationQuery(page, limit);
        return await SendPageAsync<EvaluationCriteriaListResponse, EvaluationCriteriaResponse>(HttpMethod.Get, $"/agents/{Uri.EscapeDataString(agentId)}/evaluation-criteria", query, body: null, legacyKey: null, cancellationToken).ConfigureAwait(false);
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
        return await SendPageAsync<EvaluationResultListResponse, EvaluationResultResponse>(HttpMethod.Get, $"/agents/evaluation-criteria/{Uri.EscapeDataString(criteriaId)}/results", query, body: null, "data", cancellationToken).ConfigureAwait(false);
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
        return await SendPageAsync<CompatibleRunListResponse, JsonElement>(HttpMethod.Get, $"/agents/evaluation-criteria/{Uri.EscapeDataString(criteriaId)}/compatible-runs", query, body: null, "data", cancellationToken).ConfigureAwait(false);
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
        return await SendPageAsync<EvaluationResultWithCriteriaListResponse, JsonElement>(HttpMethod.Get, $"/agents/{Uri.EscapeDataString(agentId)}/evaluation-results", query, body: null, "data", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Lists evaluation results for a specific run.</summary>
    /// <remarks>
    /// By default the endpoint returns every result and ignores <c>page</c> and <c>limit</c>.
    /// Once <see cref="SeclaiClientOptions.ApiVersion"/> is <c>2026-07-27</c> or later it
    /// returns one page, 20 items unless <c>limit</c> is passed.
    /// </remarks>
    public async Task<EvaluationResultWithCriteriaListResponse> ListRunEvaluationResultsAsync(string agentId, string runId, int? page = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));
        if (string.IsNullOrWhiteSpace(runId)) throw new ArgumentException("runId is required", nameof(runId));
        var query = PaginationQuery(page, limit);
        return await SendPageAsync<EvaluationResultWithCriteriaListResponse, JsonElement>(HttpMethod.Get, $"/agents/{Uri.EscapeDataString(agentId)}/runs/{Uri.EscapeDataString(runId)}/evaluation-results", query, body: null, legacyKey: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Lists evaluation run summaries for an agent.</summary>
    public async Task<EvaluationRunSummaryListResponse> ListEvaluationRunsAsync(string agentId, int? page = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));
        var query = PaginationQuery(page, limit);
        return await SendPageAsync<EvaluationRunSummaryListResponse, JsonElement>(HttpMethod.Get, $"/agents/{Uri.EscapeDataString(agentId)}/evaluation-runs", query, body: null, "data", cancellationToken).ConfigureAwait(false);
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
        return await SendPageAsync<KnowledgeBaseListResponse, KnowledgeBaseResponse>(HttpMethod.Get, "/knowledge_bases", query, body: null, "knowledge_bases", cancellationToken).ConfigureAwait(false);
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
        return await SendPageAsync<MemoryBankListResponse, MemoryBankResponse>(HttpMethod.Get, "/memory_banks", query, body: null, "memory_banks", cancellationToken).ConfigureAwait(false);
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
    /// <remarks>
    /// Returns the response body as sent: a bare array by default, and the canonical
    /// <c>{data, pagination}</c> envelope once <see cref="SeclaiClientOptions.ApiVersion"/> is
    /// <c>2026-07-27</c> or later. The <see cref="Typed"/> form reads both.
    /// </remarks>
    public async Task<JsonElement> GetAgentsUsingMemoryBankAsync(string memoryBankId, CancellationToken cancellationToken = default)
    {
        return (await GetAgentsUsingMemoryBankResponseAsync(memoryBankId, cancellationToken).ConfigureAwait(false)).ToJson();
    }

    // The one definition of this request; the Typed form reads the same response.
    internal async Task<RawResponse> GetAgentsUsingMemoryBankResponseAsync(string memoryBankId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(memoryBankId)) throw new ArgumentException("memoryBankId is required", nameof(memoryBankId));
        return await SendRawResponseAsync(HttpMethod.Get, $"/memory_banks/{Uri.EscapeDataString(memoryBankId)}/agents", query: null, body: null, cancellationToken).ConfigureAwait(false);
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
    /// <remarks>
    /// Returns the response body as sent: a bare array by default, and the canonical
    /// <c>{data, pagination}</c> envelope once <see cref="SeclaiClientOptions.ApiVersion"/> is
    /// <c>2026-07-27</c> or later. The <see cref="Typed"/> form reads both.
    /// </remarks>
    public async Task<JsonElement> ListMemoryBankTemplatesAsync(CancellationToken cancellationToken = default)
    {
        return (await ListMemoryBankTemplatesResponseAsync(cancellationToken).ConfigureAwait(false)).ToJson();
    }

    // The one definition of this request; the Typed form reads the same response.
    internal async Task<RawResponse> ListMemoryBankTemplatesResponseAsync(CancellationToken cancellationToken)
    {
        return await SendRawResponseAsync(HttpMethod.Get, "/memory_banks/templates", query: null, body: null, cancellationToken).ConfigureAwait(false);
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

    /// <summary>Lists a source's content items and their indexing status.</summary>
    /// <param name="sourceId">Source identifier.</param>
    /// <param name="page">Page number (1-indexed, default 1).</param>
    /// <param name="limit">Items per page (1-100, default 20).</param>
    /// <param name="sort">Sort field: <c>created_at</c>, <c>title</c> or <c>status</c>.</param>
    /// <param name="order"><c>asc</c> or <c>desc</c>.</param>
    /// <param name="status">Keep only one status: <c>pending</c>, <c>fetching</c>, <c>transcribing</c>, <c>scanning</c>, <c>indexing</c>, <c>completed</c> or <c>failed</c>.</param>
    /// <param name="contentVersionIds">Keep only these items — the <c>ContentVersionId</c> values the upload methods return — to poll a batch of uploads in one request. Keep it to about 100: the ids travel in the query string, and a URL over 8,192 bytes is rejected with a 414. The API itself accepts at most 500. An empty list matches nothing: an empty page is returned without sending a request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<SourceContentStatusListResponse> ListSourceContentsAsync(
        string sourceId,
        int? page = null,
        int? limit = null,
        string? sort = null,
        string? order = null,
        string? status = null,
        IEnumerable<string>? contentVersionIds = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("sourceId is required", nameof(sourceId));
        var query = PaginationQuery(page, limit, sort, order);
        query["status"] = string.IsNullOrWhiteSpace(status) ? null : status;
        // An empty filter encodes as no parameter, which the API reads as "list everything".
        var ids = contentVersionIds?.Where(id => !string.IsNullOrWhiteSpace(id)).ToList();
        if (ids is { Count: 0 })
        {
            return new SourceContentStatusListResponse
            {
                Pagination = new PaginationResponse { Page = page is > 0 ? page.Value : 1, Limit = limit is > 0 ? limit.Value : 20 },
            };
        }
        var repeated = new Dictionary<string, IEnumerable<string>?>();
        repeated["content_version_id"] = ids;
        return await SendJsonAsync<SourceContentStatusListResponse>(HttpMethod.Get, $"/sources/{Uri.EscapeDataString(sourceId)}/contents", query, body: null, cancellationToken, repeatedQuery: repeated).ConfigureAwait(false);
    }

    /// <summary>Gets one content item's indexing status, by the <c>ContentVersionId</c> an upload returned.</summary>
    public async Task<SourceContentStatusResponse> GetSourceContentStatusAsync(string sourceId, string contentVersionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("sourceId is required", nameof(sourceId));
        if (string.IsNullOrWhiteSpace(contentVersionId)) throw new ArgumentException("contentVersionId is required", nameof(contentVersionId));
        return await SendJsonAsync<SourceContentStatusResponse>(HttpMethod.Get, $"/sources/{Uri.EscapeDataString(sourceId)}/contents/{Uri.EscapeDataString(contentVersionId)}", query: null, body: null, cancellationToken).ConfigureAwait(false);
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
        await ApplyHeadersAsync(req, cancellationToken).ConfigureAwait(false);

        var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

        if (!resp.IsSuccessStatusCode)
        {
            var responseBody = await ReadBodyAsync(resp).ConfigureAwait(false);
            resp.Dispose();
            ThrowApiError(resp.StatusCode, req.Method.Method, url, responseBody);
        }

        return resp;
    }

    /// <summary>
    /// Downloads a file attachment emitted by a step in an agent run. Returns the raw HTTP
    /// response so the caller can stream the body. The caller must dispose the returned
    /// <see cref="HttpResponseMessage"/>.
    /// </summary>
    /// <param name="runId">Run identifier.</param>
    /// <param name="attachmentId">
    /// The <c>Id</c> of an entry in the run's or a step's <c>Attachments</c>, or the
    /// URL-safe-base64-encoded storage key that webhook and email links carry.
    /// </param>
    /// <param name="downloadName">Optional filename hint for the download disposition.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<HttpResponseMessage> DownloadAgentRunAttachmentAsync(string runId, string attachmentId, string? downloadName = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(runId)) throw new ArgumentException("runId is required", nameof(runId));
        if (string.IsNullOrWhiteSpace(attachmentId)) throw new ArgumentException("attachmentId is required", nameof(attachmentId));

        var query = string.IsNullOrWhiteSpace(downloadName)
            ? null
            : new Dictionary<string, string?> { ["download_name"] = downloadName };
        var url = BuildUri($"/v2/agent-runs/{Uri.EscapeDataString(runId)}/attachments/{Uri.EscapeDataString(attachmentId)}", query);
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        await ApplyHeadersAsync(req, cancellationToken).ConfigureAwait(false);

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
        return await SendListAsync<SolutionConversationResponse>(HttpMethod.Get, $"/solutions/{Uri.EscapeDataString(solutionId)}/conversations", query: null, cancellationToken).ConfigureAwait(false);
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
        return await SendListAsync<GovernanceConversationResponse>(HttpMethod.Get, "/governance/ai-assistant/conversations", query: null, cancellationToken).ConfigureAwait(false);
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
    /// <remarks>
    /// <c>severity</c>:
    /// Deprecated and ignored. The API declares no severity filter on this
    /// endpoint, so it never filtered anything, and sending it is a 422 once
    /// <see cref="SeclaiClientOptions.ApiVersion"/> is <c>2026-07-27</c> or
    /// later. Accepted and dropped so existing call sites keep working.
    /// </remarks>
    public async Task<JsonElement> ListAlertsAsync(int? page = null, int? limit = null, string? status = null, string? severity = null, CancellationToken cancellationToken = default)
    {
        var query = PaginationQuery(page, limit);
        query["status"] = string.IsNullOrWhiteSpace(status) ? null : status;
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
    /// <remarks>
    /// The configurations arrive under <c>configs</c> alongside <c>total</c> by
    /// default. Once the caller opts in with
    /// <see cref="SeclaiClientOptions.ApiVersion"/> of <c>2026-07-27</c> or later
    /// the endpoint returns the canonical <c>{data, pagination}</c> envelope
    /// instead, so the top-level key changes — and it returns one page, 50 items
    /// unless <c>limit</c> is passed, where the default returns every configuration.
    /// </remarks>
    public async Task<JsonElement> ListAlertConfigsAsync(int? page = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        return (await ListAlertConfigsResponseAsync(page, limit, cancellationToken).ConfigureAwait(false)).ToJson();
    }

    // The one definition of this request; the Typed form reads the same response.
    internal async Task<RawResponse> ListAlertConfigsResponseAsync(int? page, int? limit, CancellationToken cancellationToken)
    {
        var query = PaginationQuery(page, limit);
        return await SendRawResponseAsync(HttpMethod.Get, "/alerts/configs", query, body: null, cancellationToken).ConfigureAwait(false);
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
        return await SendPageAsync<OrganizationAlertPreferenceListResponse, JsonElement>(HttpMethod.Get, "/alerts/organization-preferences/list", query: null, body: null, "preferences", cancellationToken).ConfigureAwait(false);
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
    /// <remarks>
    /// <c>page</c>:
    /// 1-indexed page number, translated to the <c>offset</c> the endpoint
    /// actually declares. It does not accept <c>page</c>, so every page after
    /// the first previously returned page 1.
    ///
    /// Returns the response body as sent: <c>{alerts, total}</c> by default, and the canonical
    /// <c>{data, pagination}</c> envelope once <see cref="SeclaiClientOptions.ApiVersion"/> is
    /// <c>2026-07-27</c> or later. The <see cref="Typed"/> form reads both.
    /// </remarks>
    public async Task<JsonElement> ListModelAlertsAsync(int? page = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        return (await ListModelAlertsResponseAsync(page, limit, cancellationToken).ConfigureAwait(false)).ToJson();
    }

    // The one definition of this request; the Typed form reads the same response.
    internal async Task<RawResponse> ListModelAlertsResponseAsync(int? page, int? limit, CancellationToken cancellationToken)
    {
        var effectiveLimit = limit is > 0 ? limit.Value : 50;
        var query = new Dictionary<string, string?>
        {
            ["offset"] = page is > 1 ? ((page.Value - 1) * effectiveLimit).ToString() : null,
            ["limit"] = limit is > 0 ? limit.Value.ToString() : null,
        };
        return await SendRawResponseAsync(HttpMethod.Get, "/models/alerts", query, body: null, cancellationToken).ConfigureAwait(false);
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

    /// <summary>Lists all enabled LLM models grouped by provider.</summary>
    /// <remarks>
    /// Returns the response body as sent: a bare array by default, and the canonical
    /// <c>{data, pagination}</c> envelope once <see cref="SeclaiClientOptions.ApiVersion"/> is
    /// <c>2026-07-27</c> or later. The <see cref="Typed"/> form reads both.
    /// </remarks>
    public async Task<JsonElement> ListModelsAsync(string? provider = null, bool? supportsToolUse = null, bool? supportsThinking = null, CancellationToken cancellationToken = default)
    {
        return (await ListModelsResponseAsync(provider, supportsToolUse, supportsThinking, cancellationToken).ConfigureAwait(false)).ToJson();
    }

    // The one definition of this request; the Typed form reads the same response.
    internal async Task<RawResponse> ListModelsResponseAsync(string? provider, bool? supportsToolUse, bool? supportsThinking, CancellationToken cancellationToken)
    {
        var query = new Dictionary<string, string?>
        {
            ["provider"] = string.IsNullOrWhiteSpace(provider) ? null : provider,
            ["supports_tool_use"] = supportsToolUse?.ToString().ToLowerInvariant(),
            ["supports_thinking"] = supportsThinking?.ToString().ToLowerInvariant(),
        };
        return await SendRawResponseAsync(HttpMethod.Get, "/models", query, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Retrieves full details for a specific model.</summary>
    public async Task<JsonElement> GetModelAsync(string modelId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelId)) throw new ArgumentException("modelId is required", nameof(modelId));
        return await SendRawAsync(HttpMethod.Get, $"/models/{Uri.EscapeDataString(modelId)}/details", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Lists the embedding models a source can index with, and their pricing.</summary>
    /// <param name="supportsInputMedia">Keep only embedders that can index this input modality — a coarse kind (<c>text</c>, <c>image</c>, <c>video</c>, <c>audio</c>) or a full MIME type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>Read the models through <see cref="EmbeddingModelListResponse.Items"/>; the key they arrive under depends on <see cref="SeclaiClientOptions.ApiVersion"/>.</remarks>
    public async Task<EmbeddingModelListResponse> ListEmbeddingModelsAsync(string? supportsInputMedia = null, CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["supports_input_media"] = string.IsNullOrWhiteSpace(supportsInputMedia) ? null : supportsInputMedia,
        };
        return await SendPageAsync<EmbeddingModelListResponse, EmbeddingModelResponse>(HttpMethod.Get, "/models/embedders", query, body: null, "models", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Lists the reranker models a knowledge base can use, and their pricing.</summary>
    /// <remarks>Read the models through <see cref="RerankerModelListResponse.Items"/>; the key they arrive under depends on <see cref="SeclaiClientOptions.ApiVersion"/>.</remarks>
    public async Task<RerankerModelListResponse> ListRerankerModelsAsync(CancellationToken cancellationToken = default)
    {
        return await SendPageAsync<RerankerModelListResponse, RerankerModelResponse>(HttpMethod.Get, "/models/rerankers", query: null, body: null, "models", cancellationToken).ConfigureAwait(false);
    }

    // ── Model Playground Experiments ────────────────────────────────────────

    /// <summary>Lists model playground experiments.</summary>
    /// <remarks>
    /// Returns the response body as sent: <c>{experiments, total}</c> by default, and the canonical
    /// <c>{data, pagination}</c> envelope once <see cref="SeclaiClientOptions.ApiVersion"/> is
    /// <c>2026-07-27</c> or later. The <see cref="Typed"/> form reads both.
    /// </remarks>
    public async Task<JsonElement> ListExperimentsAsync(int? days = null, string? startDate = null, string? endDate = null, int? limit = null, int? offset = null, CancellationToken cancellationToken = default)
    {
        return (await ListExperimentsResponseAsync(days, startDate, endDate, limit, offset, cancellationToken).ConfigureAwait(false)).ToJson();
    }

    // The one definition of this request; the Typed form reads the same response.
    internal async Task<RawResponse> ListExperimentsResponseAsync(int? days, string? startDate, string? endDate, int? limit, int? offset, CancellationToken cancellationToken)
    {
        var query = new Dictionary<string, string?>
        {
            ["days"] = days is > 0 ? days.Value.ToString() : null,
            ["start_date"] = string.IsNullOrWhiteSpace(startDate) ? null : startDate,
            ["end_date"] = string.IsNullOrWhiteSpace(endDate) ? null : endDate,
            ["limit"] = limit is > 0 ? limit.Value.ToString() : null,
            ["offset"] = offset is >= 0 ? offset.Value.ToString() : null,
        };
        return await SendRawResponseAsync(HttpMethod.Get, "/models/playground/experiments", query, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Creates a model playground experiment.</summary>
    public async Task<JsonElement> CreateExperimentAsync(PlaygroundCreateRequest body, CancellationToken cancellationToken = default)
    {
        return await SendRawAsync(HttpMethod.Post, "/models/playground/experiments", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Retrieves a model playground experiment by ID.</summary>
    public async Task<JsonElement> GetExperimentAsync(string experimentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(experimentId)) throw new ArgumentException("experimentId is required", nameof(experimentId));
        return await SendRawAsync(HttpMethod.Get, $"/models/playground/experiments/{Uri.EscapeDataString(experimentId)}", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Cancels a running model playground experiment.</summary>
    public async Task<JsonElement> CancelExperimentAsync(string experimentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(experimentId)) throw new ArgumentException("experimentId is required", nameof(experimentId));
        return await SendRawAsync(HttpMethod.Post, $"/models/playground/experiments/{Uri.EscapeDataString(experimentId)}/cancel", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Soft-deletes a model playground experiment, removing it from list/detail views
    /// while preserving audit history.
    /// </summary>
    public async Task DeleteExperimentAsync(string experimentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(experimentId)) throw new ArgumentException("experimentId is required", nameof(experimentId));
        await SendNoContentAsync(HttpMethod.Delete, $"/models/playground/experiments/{Uri.EscapeDataString(experimentId)}", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    // ── General Search ──────────────────────────────────────────────────────

    /// <summary>Performs a general search across resources.</summary>
    public async Task<JsonElement> SearchAsync(string? query = null, int? limit = null, string? entityType = null, CancellationToken cancellationToken = default)
    {
        // The spec names this `q` and marks it required. The parameter stays
        // optional so existing call sites keep compiling; a blank one fails here
        // with the field name rather than as a 422 naming the wire parameter.
        if (string.IsNullOrWhiteSpace(query)) throw new ArgumentException("query is required", nameof(query));
        var q = new Dictionary<string, string?>
        {
            ["q"] = query,
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

    // ── Identity ──────────────────────────────────────────────────────────────

    /// <summary>Gets the authenticated user's personal account ID and the organizations they belong to.</summary>
    public async Task<MeResponse> GetMeAsync(CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<MeResponse>(HttpMethod.Get, "/me", null, null, cancellationToken).ConfigureAwait(false);
    }
    /// <summary>Pauses an agent so it stops firing from every trigger path. Returns 409 when other live agents still call this one via a call_agent step.</summary>
    public async Task<AgentSummaryResponse> DisableAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));
        return await SendJsonAsync<AgentSummaryResponse>(HttpMethod.Post, $"/agents/{Uri.EscapeDataString(agentId)}/disable", null, null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Resumes a paused agent, whether paused manually or by the inbound-email overload safeguard.</summary>
    public async Task<AgentSummaryResponse> EnableAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));
        return await SendJsonAsync<AgentSummaryResponse>(HttpMethod.Post, $"/agents/{Uri.EscapeDataString(agentId)}/enable", null, null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Lists the live agents that call this agent via a call_agent step. They must be disabled before this agent can be paused.</summary>
    public async Task<List<AgentCallerApiResponse>> GetAgentCallersAsync(string agentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));
        return await SendListAsync<AgentCallerApiResponse>(HttpMethod.Get, $"/agents/{Uri.EscapeDataString(agentId)}/callers", null, cancellationToken).ConfigureAwait(false);
    }
    // ── Agent Email Triggers ──────────────────────────────────────────────────

    /// <summary>Sets the alias, sender allowlist and inbound-handling flags on an agent's EMAIL_RECEIVED trigger. A null property is left unchanged.</summary>
    public async Task<EmailTriggerConfigResponse> SetEmailTriggerConfigAsync(string agentId, string triggerId, SetEmailTriggerConfigRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("agentId is required", nameof(agentId));
        if (string.IsNullOrWhiteSpace(triggerId)) throw new ArgumentException("triggerId is required", nameof(triggerId));
        return await SendJsonAsync<EmailTriggerConfigResponse>(HttpMethod.Put, $"/agents/{Uri.EscapeDataString(agentId)}/triggers/{Uri.EscapeDataString(triggerId)}/email-config", null, body, cancellationToken).ConfigureAwait(false);
    }
    // ── API Version ───────────────────────────────────────────────────────────

    /// <summary>Reads the API version this request resolved to, and the versions available.</summary>
    /// <remarks>
    /// <c>EffectiveVersion</c> resolves as header, then account pin, then default —
    /// so it reflects <see cref="SeclaiClientOptions.ApiVersion"/> when that is set.
    /// </remarks>
    public async Task<ApiVersionResponse> GetApiVersionAsync(CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<ApiVersionResponse>(HttpMethod.Get, "/version", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Pins the account to a dated API version, or clears the pin with <c>null</c>.</summary>
    /// <remarks>
    /// Owner/admin only. The new pin applies to later header-less requests; a
    /// <c>Seclai-Version</c> header still overrides it, so
    /// <c>EffectiveVersion</c> in the response describes this request rather than
    /// the pin just written.
    /// </remarks>
    public async Task<ApiVersionResponse> UpdateApiVersionAsync(string? version, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<ApiVersionResponse>(HttpMethod.Put, "/version", query: null, new UpdateApiVersionRequest { Version = version }, cancellationToken).ConfigureAwait(false);
    }

    // ── Agent Email Governance ────────────────────────────────────────────────

    /// <summary>Lists recipients who have opted out of this account's agent emails. Account-wide opt-outs always apply.</summary>
    public async Task<AgentEmailOptOutListResponse> ListAgentEmailOptOutsAsync(string? agentId = null, int? limit = null, int? offset = null, CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["agent_id"] = string.IsNullOrWhiteSpace(agentId) ? null : agentId,
            ["limit"] = limit is > 0 ? limit.Value.ToString() : null,
            ["offset"] = offset is >= 0 ? offset.Value.ToString() : null,
        };
        return await SendPageAsync<AgentEmailOptOutListResponse, AgentEmailOptOutResponse>(HttpMethod.Get, "/agents/agent-email-optouts", query, null, "items", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Revokes an opt-out, opting the recipient back in to agent emails.</summary>
    public async Task RemoveAgentEmailOptOutAsync(string optoutId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(optoutId)) throw new ArgumentException("optoutId is required", nameof(optoutId));
        await SendNoContentAsync(HttpMethod.Delete, $"/agents/agent-email-optouts/{Uri.EscapeDataString(optoutId)}", null, null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Lists the account's blocked inbound email senders plus the governance auto-block mode. Paginates by limit/offset.</summary>
    public async Task<BlockedEmailSenderListResponse> ListBlockedEmailSendersAsync(int? limit = null, int? offset = null, CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["limit"] = limit is > 0 ? limit.Value.ToString() : null,
            ["offset"] = offset is >= 0 ? offset.Value.ToString() : null,
        };
        return await SendPageAsync<BlockedEmailSenderListResponse, BlockedEmailSenderResponse>(HttpMethod.Get, "/agents/blocked-email-senders", query, null, "items", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Adds a sender address or a whole domain to the account blocklist. Idempotent. Requires an account owner or admin.</summary>
    public async Task<BlockedEmailSenderResponse> BlockEmailSenderAsync(BlockEmailSenderRequest body, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<BlockedEmailSenderResponse>(HttpMethod.Post, "/agents/blocked-email-senders", null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Removes a blocked sender by ID. Requires an account owner or admin.</summary>
    public async Task UnblockEmailSenderAsync(string blockedId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(blockedId)) throw new ArgumentException("blockedId is required", nameof(blockedId));
        await SendNoContentAsync(HttpMethod.Delete, $"/agents/blocked-email-senders/{Uri.EscapeDataString(blockedId)}", null, null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sets whether a governance BLOCK on an authenticated inbound sender auto-adds them to the blocklist. Requires an account owner or admin.</summary>
    /// <remarks>
    /// Returns the first blocked senders. <c>Total</c> is the account's full count by default, and
    /// the number of rows returned once <see cref="SeclaiClientOptions.ApiVersion"/> is
    /// <c>2026-07-27</c> or later.
    /// </remarks>
    public async Task<BlockedEmailSenderListResponse> SetAutoBlockModeAsync(SetAutoBlockModeRequest body, CancellationToken cancellationToken = default)
    {
        return await SendPageAsync<BlockedEmailSenderListResponse, BlockedEmailSenderResponse>(HttpMethod.Put, "/agents/blocked-email-senders/mode", null, body, "items", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Lists recent inbound emails discarded before running an agent — unauthorized sender, unknown alias, spam or flood-shed.</summary>
    public async Task<List<InboundEmailRejectionResponse>> ListInboundEmailRejectionsAsync(string? agentId = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["agent_id"] = string.IsNullOrWhiteSpace(agentId) ? null : agentId,
            ["limit"] = limit is > 0 ? limit.Value.ToString() : null,
        };
        return await SendListAsync<InboundEmailRejectionResponse>(HttpMethod.Get, "/agents/inbound-email-rejections", query, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Reports whether the account-wide overload circuit breaker has paused inbound email, plus the queued backlog size.</summary>
    public async Task<InboundEmailStatusResponse> GetInboundEmailStatusAsync(CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<InboundEmailStatusResponse>(HttpMethod.Get, "/agents/inbound-email-status", null, null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Fails all of the account's QUEUED (over-quota parked) inbound-email runs. Requires an account owner or admin.</summary>
    public async Task<CancelQueuedRunsResponse> CancelQueuedEmailRunsAsync(CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<CancelQueuedRunsResponse>(HttpMethod.Post, "/agents/inbound-email-status/cancel-queued", null, null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Manually lifts the account-wide inbound-email pause. One-shot: the breaker re-arms if the backlog is still above the ceiling.</summary>
    public async Task<ResumeInboundResponse> ResumeInboundEmailAsync(CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<ResumeInboundResponse>(HttpMethod.Post, "/agents/inbound-email-status/resume", null, null, cancellationToken).ConfigureAwait(false);
    }
    // ── Email Domains ─────────────────────────────────────────────────────────

    /// <summary>Lists the account's vanity and custom agent-email domains with verification status, required DNS records and plan capabilities.</summary>
    public async Task<EmailDomainsListResponse> ListEmailDomainsAsync(CancellationToken cancellationToken = default)
    {
        return await SendPageAsync<EmailDomainsListResponse, EmailDomainResponse>(HttpMethod.Get, "/email-domains", null, null, "domains", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Adds and provisions a vanity subdomain or a custom domain, standing up the SES identity and DNS. Requires an account owner or admin.</summary>
    public async Task<EmailDomainResponse> AddEmailDomainAsync(AddEmailDomainRequest body, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<EmailDomainResponse>(HttpMethod.Post, "/email-domains", null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Removes a domain and tears down its SES identity and DNS. Returns a cleanup note when the domain was Seclai-managed.</summary>
    public async Task<RemoveEmailDomainResponse> RemoveEmailDomainAsync(string domainId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(domainId)) throw new ArgumentException("domainId is required", nameof(domainId));
        return await SendJsonAsync<RemoveEmailDomainResponse>(HttpMethod.Delete, $"/email-domains/{Uri.EscapeDataString(domainId)}", null, null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Re-polls SES and DNS for this domain now instead of waiting for the background sweep.</summary>
    public async Task<EmailDomainResponse> VerifyEmailDomainAsync(string domainId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(domainId)) throw new ArgumentException("domainId is required", nameof(domainId));
        return await SendJsonAsync<EmailDomainResponse>(HttpMethod.Post, $"/email-domains/{Uri.EscapeDataString(domainId)}/verify", null, null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Promotes a verified domain to the account's primary domain, so agent email sends from and receives on it.</summary>
    public async Task<EmailDomainResponse> SetPrimaryEmailDomainAsync(string domainId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(domainId)) throw new ArgumentException("domainId is required", nameof(domainId));
        return await SendJsonAsync<EmailDomainResponse>(HttpMethod.Post, $"/email-domains/{Uri.EscapeDataString(domainId)}/primary", null, null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Reverts to the shared agent.seclai.com domain without removing configured domains — they stay verified and can be promoted again.</summary>
    public async Task UseSharedEmailDomainAsync(CancellationToken cancellationToken = default)
    {
        await SendNoContentAsync(HttpMethod.Post, "/email-domains/use-shared-domain", null, null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sends a test message from a verified domain to the account owner's address. Never sends anywhere else.</summary>
    public async Task<SendTestEmailResponse> SendEmailDomainTestEmailAsync(string domainId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(domainId)) throw new ArgumentException("domainId is required", nameof(domainId));
        return await SendJsonAsync<SendTestEmailResponse>(HttpMethod.Post, $"/email-domains/{Uri.EscapeDataString(domainId)}/test-email", null, null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Returns pass rate, disposition breakdown and top failing source IPs from the DMARC aggregate reports.</summary>
    public async Task<DmarcSummaryResponse> GetDmarcSummaryAsync(string domainId, int? days = null, int? topSources = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(domainId)) throw new ArgumentException("domainId is required", nameof(domainId));
        var query = new Dictionary<string, string?>
        {
            ["days"] = days is > 0 ? days.Value.ToString() : null,
            ["top_sources"] = topSources is > 0 ? topSources.Value.ToString() : null,
        };
        return await SendJsonAsync<DmarcSummaryResponse>(HttpMethod.Get, $"/email-domains/{Uri.EscapeDataString(domainId)}/dmarc", query, null, cancellationToken).ConfigureAwait(false);
    }
    /// <summary>Lists the media-generation quality tiers and the model and cost each resolves to. Global routing and pricing; read-only.</summary>
    /// <remarks>
    /// Returns the response body as sent: <c>{tiers}</c> by default, and the canonical
    /// <c>{data, pagination}</c> envelope once <see cref="SeclaiClientOptions.ApiVersion"/> is
    /// <c>2026-07-27</c> or later. The <see cref="Typed"/> form reads both.
    /// </remarks>
    public async Task<JsonElement> GetGenerationTiersAsync(CancellationToken cancellationToken = default)
    {
        return (await GetGenerationTiersResponseAsync(cancellationToken).ConfigureAwait(false)).ToJson();
    }

    // The one definition of this request; the Typed form reads the same response.
    internal async Task<RawResponse> GetGenerationTiersResponseAsync(CancellationToken cancellationToken)
    {
        return await SendRawResponseAsync(HttpMethod.Get, "/models/generation-tiers", null, null, cancellationToken).ConfigureAwait(false);
    }
    /// <summary>Searches the Seclai documentation by content. Mode is keyword (default) or semantic. Results are global, not account-scoped.</summary>
    public async Task<JsonElement> SearchDocsAsync(string query, string? mode = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query)) throw new ArgumentException("query is required", nameof(query));
        var query_ = new Dictionary<string, string?>
        {
            ["q"] = query,
            ["mode"] = string.IsNullOrWhiteSpace(mode) ? null : mode,
            ["limit"] = limit is > 0 ? limit.Value.ToString() : null,
        };
        return await SendRawAsync(HttpMethod.Get, "/docs-search", query_, null, cancellationToken).ConfigureAwait(false);
    }
    // ── Cloud Drives ──────────────────────────────────────────────────────────

    /// <summary>Lists the cloud-drive providers this deployment has configured.</summary>
    public async Task<List<CloudDriveProviderResponse>> ListCloudDriveProvidersAsync(CancellationToken cancellationToken = default)
    {
        return await SendListAsync<CloudDriveProviderResponse>(HttpMethod.Get, "/cloud-drives/providers", query: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Lists the account's cloud-drive connections.</summary>
    public async Task<List<CloudDriveResponse>> ListCloudDrivesAsync(CancellationToken cancellationToken = default)
    {
        return await SendListAsync<CloudDriveResponse>(HttpMethod.Get, "/cloud-drives", query: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Gets a cloud-drive connection.</summary>
    public async Task<CloudDriveResponse> GetCloudDriveAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionId)) throw new ArgumentException("connectionId is required", nameof(connectionId));
        return await SendJsonAsync<CloudDriveResponse>(HttpMethod.Get, $"/cloud-drives/{Uri.EscapeDataString(connectionId)}", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Renames a cloud-drive connection or changes the folder it watches. A null property is left unchanged.</summary>
    public async Task<CloudDriveResponse> UpdateCloudDriveAsync(string connectionId, CloudDriveUpdateRequest body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionId)) throw new ArgumentException("connectionId is required", nameof(connectionId));
        return await SendJsonAsync<CloudDriveResponse>(HttpPatch, $"/cloud-drives/{Uri.EscapeDataString(connectionId)}", query: null, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Disconnects a cloud-drive connection, keeping the connection itself. Returns it in its disconnected state.</summary>
    public async Task<CloudDriveResponse> DisconnectCloudDriveAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionId)) throw new ArgumentException("connectionId is required", nameof(connectionId));
        return await SendJsonAsync<CloudDriveResponse>(HttpMethod.Post, $"/cloud-drives/{Uri.EscapeDataString(connectionId)}/disconnect", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Deletes a cloud-drive connection.</summary>
    public async Task DeleteCloudDriveAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionId)) throw new ArgumentException("connectionId is required", nameof(connectionId));
        await SendNoContentAsync(HttpMethod.Delete, $"/cloud-drives/{Uri.EscapeDataString(connectionId)}", query: null, body: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Lists the agents that use a cloud-drive connection.</summary>
    public async Task<List<AgentUsingCloudDriveResponse>> GetAgentsUsingCloudDriveAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionId)) throw new ArgumentException("connectionId is required", nameof(connectionId));
        return await SendListAsync<AgentUsingCloudDriveResponse>(HttpMethod.Get, $"/cloud-drives/{Uri.EscapeDataString(connectionId)}/agents", query: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Lists the files a cloud-drive connection skipped, newest first. A skipped file fires no trigger, so this is where to look when an agent did not run for a file.</summary>
    /// <param name="connectionId">Cloud-drive connection identifier.</param>
    /// <param name="limit">Maximum number of rejections (1-200, default 50).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<List<CloudDriveRejectionResponse>> ListCloudDriveRejectionsAsync(string connectionId, int? limit = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionId)) throw new ArgumentException("connectionId is required", nameof(connectionId));
        var query = new Dictionary<string, string?>
        {
            ["limit"] = limit is > 0 ? limit.Value.ToString() : null,
        };
        return await SendListAsync<CloudDriveRejectionResponse>(HttpMethod.Get, $"/cloud-drives/{Uri.EscapeDataString(connectionId)}/rejections", query, cancellationToken).ConfigureAwait(false);
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
        await ApplyHeadersAsync(req, cancellationToken, "text/event-stream", "application/json").ConfigureAwait(false);

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

        var run = await RunAgentAsync(agentId, body, cancellationToken).ConfigureAwait(false);
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
            run = await GetAgentRunAsync(run.RunId!, includeStepOutputs, ct).ConfigureAwait(false);
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
        await ApplyHeadersAsync(req, cancellationToken, "application/json").ConfigureAwait(false);

        using var resp = await _http.SendAsync(req, cancellationToken).ConfigureAwait(false);
        var responseBody = await ReadBodyAsync(resp).ConfigureAwait(false);

        if (!resp.IsSuccessStatusCode)
        {
            ThrowApiError(resp.StatusCode, req.Method.Method, url, responseBody);
        }

        return responseBody ?? string.Empty;
    }
}

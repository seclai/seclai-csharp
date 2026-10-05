using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Seclai.Models;

namespace Seclai;

/// <summary>
/// Typed variants of the <see cref="SeclaiClient"/> methods that return raw JSON.
/// </summary>
/// <remarks>
/// Reached through <see cref="SeclaiClient.Typed"/>. Every method here delegates
/// to its counterpart on the client and deserializes the result, so the request
/// issued is identical by construction and the two surfaces cannot drift apart.
/// </remarks>
public sealed class SeclaiTypedClient
{
    private readonly SeclaiClient _client;

    internal SeclaiTypedClient(SeclaiClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    private static T Deserialize<T>(JsonElement raw)
    {
        var value = raw.Deserialize<T>(SeclaiClient.TypedJsonOptions);
        if (value is null)
        {
            throw new JsonException($"The API returned a body that could not be read as {typeof(T).Name}.");
        }
        return value;
    }

    /// <inheritdoc cref="SeclaiClient.ListAlertsAsync"/>
    public async Task<AlertListResponse> ListAlertsAsync(int? page = null, int? limit = null, string? status = null, string? severity = null, CancellationToken cancellationToken = default)
    {
        var raw = await _client.ListAlertsAsync(page, limit, status, severity, cancellationToken).ConfigureAwait(false);
        return Deserialize<AlertListResponse>(raw);
    }

    /// <inheritdoc cref="SeclaiClient.GetAlertAsync"/>
    public async Task<AlertDetailResponse> GetAlertAsync(string alertId, CancellationToken cancellationToken = default)
    {
        var raw = await _client.GetAlertAsync(alertId, cancellationToken).ConfigureAwait(false);
        return Deserialize<AlertDetailResponse>(raw);
    }

    /// <inheritdoc cref="SeclaiClient.ChangeAlertStatusAsync"/>
    public async Task<AlertDetailResponse> ChangeAlertStatusAsync(string alertId, ChangeStatusRequest body, CancellationToken cancellationToken = default)
    {
        var raw = await _client.ChangeAlertStatusAsync(alertId, body, cancellationToken).ConfigureAwait(false);
        return Deserialize<AlertDetailResponse>(raw);
    }

    /// <inheritdoc cref="SeclaiClient.AddAlertCommentAsync"/>
    public async Task<AlertDetailResponse> AddAlertCommentAsync(string alertId, AddCommentRequest body, CancellationToken cancellationToken = default)
    {
        var raw = await _client.AddAlertCommentAsync(alertId, body, cancellationToken).ConfigureAwait(false);
        return Deserialize<AlertDetailResponse>(raw);
    }

    /// <inheritdoc cref="SeclaiClient.SubscribeToAlertAsync"/>
    public async Task<AlertDetailResponse> SubscribeToAlertAsync(string alertId, CancellationToken cancellationToken = default)
    {
        var raw = await _client.SubscribeToAlertAsync(alertId, cancellationToken).ConfigureAwait(false);
        return Deserialize<AlertDetailResponse>(raw);
    }

    /// <inheritdoc cref="SeclaiClient.UnsubscribeFromAlertAsync"/>
    public async Task<AlertDetailResponse> UnsubscribeFromAlertAsync(string alertId, CancellationToken cancellationToken = default)
    {
        var raw = await _client.UnsubscribeFromAlertAsync(alertId, cancellationToken).ConfigureAwait(false);
        return Deserialize<AlertDetailResponse>(raw);
    }

    /// <inheritdoc cref="SeclaiClient.ListAlertConfigsAsync"/>
    public async Task<AlertConfigListResponse> ListAlertConfigsAsync(int? page = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        var response = await _client.ListAlertConfigsResponseAsync(page, limit, cancellationToken).ConfigureAwait(false);
        return _client.ReadPage<AlertConfigListResponse, AlertConfigResponse>(response, "configs", "GET");
    }

    /// <inheritdoc cref="SeclaiClient.CreateAlertConfigAsync"/>
    public async Task<AlertConfigResponse> CreateAlertConfigAsync(CreateAlertConfigRequest body, CancellationToken cancellationToken = default)
    {
        var raw = await _client.CreateAlertConfigAsync(body, cancellationToken).ConfigureAwait(false);
        return Deserialize<AlertConfigResponse>(raw);
    }

    /// <inheritdoc cref="SeclaiClient.GetAlertConfigAsync"/>
    public async Task<AlertConfigResponse> GetAlertConfigAsync(string configId, CancellationToken cancellationToken = default)
    {
        var raw = await _client.GetAlertConfigAsync(configId, cancellationToken).ConfigureAwait(false);
        return Deserialize<AlertConfigResponse>(raw);
    }

    /// <inheritdoc cref="SeclaiClient.ListModelAlertsAsync"/>
    public async Task<ModelAlertListResponse> ListModelAlertsAsync(int? page = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        var response = await _client.ListModelAlertsResponseAsync(page, limit, cancellationToken).ConfigureAwait(false);
        return _client.ReadPage<ModelAlertListResponse, ModelAlertResponse>(response, "alerts", "GET");
    }

    /// <inheritdoc cref="SeclaiClient.GetUnreadModelAlertCountAsync"/>
    public async Task<UnreadCountResponse> GetUnreadModelAlertCountAsync(CancellationToken cancellationToken = default)
    {
        var raw = await _client.GetUnreadModelAlertCountAsync(cancellationToken).ConfigureAwait(false);
        return Deserialize<UnreadCountResponse>(raw);
    }

    /// <inheritdoc cref="SeclaiClient.GetModelRecommendationsAsync"/>
    public async Task<ModelRecommendationsResponse> GetModelRecommendationsAsync(string modelId, CancellationToken cancellationToken = default)
    {
        var raw = await _client.GetModelRecommendationsAsync(modelId, cancellationToken).ConfigureAwait(false);
        return Deserialize<ModelRecommendationsResponse>(raw);
    }

    /// <inheritdoc cref="SeclaiClient.ListModelsAsync"/>
    public async Task<List<ProviderGroupResponse>> ListModelsAsync(string? provider = null, bool? supportsToolUse = null, bool? supportsThinking = null, CancellationToken cancellationToken = default)
    {
        var response = await _client.ListModelsResponseAsync(provider, supportsToolUse, supportsThinking, cancellationToken).ConfigureAwait(false);
        return _client.ReadList<ProviderGroupResponse>(response, legacyKey: null, "GET", out _);
    }

    /// <inheritdoc cref="SeclaiClient.GetModelAsync"/>
    public async Task<PromptModelResponse> GetModelAsync(string modelId, CancellationToken cancellationToken = default)
    {
        var raw = await _client.GetModelAsync(modelId, cancellationToken).ConfigureAwait(false);
        return Deserialize<PromptModelResponse>(raw);
    }

    /// <inheritdoc cref="SeclaiClient.ListExperimentsAsync"/>
    public async Task<ExperimentListResponse> ListExperimentsAsync(int? days = null, string? startDate = null, string? endDate = null, int? limit = null, int? offset = null, CancellationToken cancellationToken = default)
    {
        var response = await _client.ListExperimentsResponseAsync(days, startDate, endDate, limit, offset, cancellationToken).ConfigureAwait(false);
        return _client.ReadPage<ExperimentListResponse, ExperimentSummaryResponse>(response, "experiments", "GET");
    }

    /// <inheritdoc cref="SeclaiClient.CreateExperimentAsync"/>
    public async Task<CreateExperimentResponse> CreateExperimentAsync(PlaygroundCreateRequest body, CancellationToken cancellationToken = default)
    {
        var raw = await _client.CreateExperimentAsync(body, cancellationToken).ConfigureAwait(false);
        return Deserialize<CreateExperimentResponse>(raw);
    }

    /// <inheritdoc cref="SeclaiClient.GetExperimentAsync"/>
    public async Task<ExperimentDetailResponse> GetExperimentAsync(string experimentId, CancellationToken cancellationToken = default)
    {
        var raw = await _client.GetExperimentAsync(experimentId, cancellationToken).ConfigureAwait(false);
        return Deserialize<ExperimentDetailResponse>(raw);
    }

    /// <inheritdoc cref="SeclaiClient.CancelExperimentAsync"/>
    public async Task<CancelExperimentResponse> CancelExperimentAsync(string experimentId, CancellationToken cancellationToken = default)
    {
        var raw = await _client.CancelExperimentAsync(experimentId, cancellationToken).ConfigureAwait(false);
        return Deserialize<CancelExperimentResponse>(raw);
    }

    /// <inheritdoc cref="SeclaiClient.SearchAsync"/>
    public async Task<SearchResponse> SearchAsync(string? query = null, int? limit = null, string? entityType = null, CancellationToken cancellationToken = default)
    {
        var raw = await _client.SearchAsync(query, limit, entityType, cancellationToken).ConfigureAwait(false);
        return Deserialize<SearchResponse>(raw);
    }

    /// <inheritdoc cref="SeclaiClient.GetGenerationTiersAsync"/>
    public async Task<GenerationTierListResponse> GetGenerationTiersAsync(CancellationToken cancellationToken = default)
    {
        var response = await _client.GetGenerationTiersResponseAsync(cancellationToken).ConfigureAwait(false);
        return _client.ReadPage<GenerationTierListResponse, GenerationTierResponse>(response, "tiers", "GET");
    }

    /// <inheritdoc cref="SeclaiClient.SearchDocsAsync"/>
    public async Task<DocsSearchResponse> SearchDocsAsync(string query, string? mode = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        var raw = await _client.SearchDocsAsync(query, mode, limit, cancellationToken).ConfigureAwait(false);
        return Deserialize<DocsSearchResponse>(raw);
    }

    /// <summary>Lists available memory bank templates, read from either response shape.</summary>
    /// <remarks>The API declares no schema for a template, so each is its JSON object.</remarks>
    public async Task<List<Dictionary<string, JsonElement>>> ListMemoryBankTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _client.ListMemoryBankTemplatesResponseAsync(cancellationToken).ConfigureAwait(false);
        return _client.ReadList<Dictionary<string, JsonElement>>(response, legacyKey: null, "GET", out _);
    }

    /// <summary>Lists agents that use a memory bank, read from either response shape.</summary>
    /// <remarks>The API declares no schema for an entry, so each is its JSON object: <c>agent_id</c> and <c>agent_name</c>.</remarks>
    public async Task<List<Dictionary<string, JsonElement>>> GetAgentsUsingMemoryBankAsync(string memoryBankId, CancellationToken cancellationToken = default)
    {
        var response = await _client.GetAgentsUsingMemoryBankResponseAsync(memoryBankId, cancellationToken).ConfigureAwait(false);
        return _client.ReadList<Dictionary<string, JsonElement>>(response, legacyKey: null, "GET", out _);
    }
}

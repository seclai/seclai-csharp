using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Seclai.Exceptions;

namespace Seclai;

/// <summary>
/// Resolved SSO profile settings from the config file.
/// </summary>
public sealed class SsoProfile
{
    /// <summary>Account ID (resolved after login via /me). May be null before first login.</summary>
    public string? SsoAccountId { get; set; }
    /// <summary>AWS region from the SSO profile.</summary>
    public string SsoRegion { get; set; } = "";
    /// <summary>OAuth client ID from the SSO profile.</summary>
    public string SsoClientId { get; set; } = "";
    /// <summary>Cognito domain from the SSO profile.</summary>
    public string SsoDomain { get; set; } = "";
}

/// <summary>
/// Cached SSO token entry on disk.
/// </summary>
public sealed class SsoCacheEntry
{
    /// <summary>JWT access token.</summary>
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = "";

    /// <summary>Refresh token for obtaining new access tokens.</summary>
    [JsonPropertyName("refreshToken")]
    public string? RefreshToken { get; set; }

    /// <summary>OIDC ID token (optional).</summary>
    [JsonPropertyName("idToken")]
    public string? IdToken { get; set; }

    /// <summary>ISO-8601 expiry timestamp for the access token.</summary>
    [JsonPropertyName("expiresAt")]
    public string ExpiresAt { get; set; } = "";

    /// <summary>Cognito app client ID.</summary>
    [JsonPropertyName("clientId")]
    public string ClientId { get; set; } = "";

    /// <summary>AWS region.</summary>
    [JsonPropertyName("region")]
    public string Region { get; set; } = "";

    /// <summary>Cognito domain.</summary>
    [JsonPropertyName("cognitoDomain")]
    public string CognitoDomain { get; set; } = "";
}

/// <summary>Authentication mode resolved by the credential chain.</summary>
internal enum AuthMode
{
    /// <summary>API key via x-api-key header.</summary>
    ApiKey,
    /// <summary>Static bearer token.</summary>
    BearerStatic,
    /// <summary>Dynamic bearer token from an async provider.</summary>
    BearerProvider,
    /// <summary>SSO profile with cached tokens and auto-refresh.</summary>
    Sso
}

/// <summary>
/// Resolved authentication state used throughout the client lifecycle.
/// Created once by <see cref="SeclaiAuth.ResolveCredentialChain"/> and used by
/// <see cref="SeclaiAuth.ApplyAuthHeadersAsync"/> on every request.
/// </summary>
internal sealed class AuthState
{
    /// <summary>Active authentication mode.</summary>
    public AuthMode Mode { get; set; }
    /// <summary>API key value (for <see cref="AuthMode.ApiKey"/> mode).</summary>
    public string? ApiKey { get; set; }
    /// <summary>Header name for API key auth.</summary>
    public string ApiKeyHeader { get; set; } = "x-api-key";
    /// <summary>Static bearer token (for <see cref="AuthMode.BearerStatic"/> mode).</summary>
    public string? AccessToken { get; set; }
    /// <summary>Async token provider (for <see cref="AuthMode.BearerProvider"/> mode).</summary>
    public Func<CancellationToken, Task<string>>? TokenProvider { get; set; }
    /// <summary>Account ID sent as X-Account-Id header.</summary>
    public string? AccountId { get; set; }
    /// <summary>Resolved SSO profile (for <see cref="AuthMode.Sso"/> mode).</summary>
    public SsoProfile? SsoProfile { get; set; }
    /// <summary>Config directory path for SSO cache lookup.</summary>
    public string? ConfigDir { get; set; }
    /// <summary>Whether to auto-refresh expired SSO tokens.</summary>
    public bool AutoRefresh { get; set; } = true;
    /// <summary>Lock to prevent concurrent SSO token refresh attempts.</summary>
    internal readonly SemaphoreSlim RefreshLock = new SemaphoreSlim(1, 1);
}

/// <summary>
/// Credential chain resolver and SSO cache utilities.
/// </summary>
public static class SeclaiAuth
{
    private const string DefaultConfigDir = ".seclai";
    private const string SsoConfigFile = "config";
    private const string SsoCacheDir = "sso/cache";
    private const int ExpiryBufferSeconds = 30;

    /// <summary>Default SSO domain (production Cognito). Override with SECLAI_SSO_DOMAIN or config file.</summary>
    public const string DefaultSsoDomain = "auth.seclai.com";
    /// <summary>Default SSO client ID (production public client). Override with SECLAI_SSO_CLIENT_ID or config file.</summary>
    public const string DefaultSsoClientId = "4bgf8v9qmc5puivbaqon9n5lmr";
    /// <summary>Default SSO region. Override with SECLAI_SSO_REGION or config file.</summary>
    public const string DefaultSsoRegion = "us-west-2";

    private static readonly JsonSerializerOptions CacheJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>Computes the SHA-1 hex hash of "domain|clientId".</summary>
    /// <param name="domain">The Cognito domain.</param>
    /// <param name="clientId">The Cognito app client ID.</param>
    /// <returns>Hex-encoded SHA-1 hash string.</returns>
    public static string CacheFileName(string domain, string clientId)
    {
        using var sha1 = SHA1.Create();
        var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(domain + "|" + clientId));
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    /// <summary>Resolves the config directory from an override, environment variable, or default.</summary>
    /// <param name="configDirOverride">Explicit directory path override.</param>
    /// <returns>Resolved config directory path, or empty string if home cannot be determined.</returns>
    internal static string ResolveConfigDir(string? configDirOverride)
    {
        if (!string.IsNullOrWhiteSpace(configDirOverride))
            return configDirOverride!;

        var env = Environment.GetEnvironmentVariable("SECLAI_CONFIG_DIR");
        if (!string.IsNullOrWhiteSpace(env))
            return env!;

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrEmpty(home))
            return "";

        return Path.Combine(home, DefaultConfigDir);
    }

    /// <summary>Parses an AWS-style INI config file into sections.</summary>
    /// <param name="reader">Text reader for the INI content.</param>
    /// <returns>Dictionary of section names to key-value pairs.</returns>
    public static Dictionary<string, Dictionary<string, string>> ParseIni(TextReader reader)
    {
        var sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        string? currentSection = null;

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";"))
                continue;

            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                var raw = line.Substring(1, line.Length - 2).Trim();
                if (raw.StartsWith("profile "))
                    currentSection = raw.Substring("profile ".Length).Trim();
                else
                    currentSection = raw;

                if (!sections.ContainsKey(currentSection))
                    sections[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                continue;
            }

            if (currentSection != null)
            {
                var eqIdx = line.IndexOf('=');
                if (eqIdx > 0)
                {
                    var key = line.Substring(0, eqIdx).Trim();
                    var val = line.Substring(eqIdx + 1).Trim();
                    sections[currentSection][key] = val;
                }
            }
        }

        return sections;
    }

    /// <summary>Loads an SSO profile from the config directory.
    /// Always returns a valid profile using built-in defaults and environment variable overrides
    /// (SECLAI_SSO_DOMAIN, SECLAI_SSO_CLIENT_ID, SECLAI_SSO_REGION).</summary>
    /// <param name="configDir">Resolved config directory path.</param>
    /// <param name="profileName">Profile name ("default" or a named profile).</param>
    /// <returns>The resolved profile with defaults applied.</returns>
    public static SsoProfile LoadSsoProfile(string configDir, string profileName)
    {
        var configPath = Path.Combine(configDir, SsoConfigFile);

        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (File.Exists(configPath))
        {
            Dictionary<string, Dictionary<string, string>> sections;
            using (var reader = new StreamReader(configPath))
            {
                sections = ParseIni(reader);
            }

            if (!sections.TryGetValue("default", out var defaultSection))
                defaultSection = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (string.Equals(profileName, "default", StringComparison.OrdinalIgnoreCase))
            {
                merged = defaultSection;
            }
            else
            {
                if (sections.TryGetValue(profileName, out var section) && section != null)
                {
                    // Inherit from default
                    foreach (var kv in defaultSection)
                        merged[kv.Key] = kv.Value;
                    foreach (var kv in section)
                        merged[kv.Key] = kv.Value;
                }
            }
        }

        // Environment variables override config file values
        var domain = Environment.GetEnvironmentVariable("SECLAI_SSO_DOMAIN");
        if (string.IsNullOrWhiteSpace(domain))
            merged.TryGetValue("sso_domain", out domain);
        if (string.IsNullOrWhiteSpace(domain))
            domain = DefaultSsoDomain;

        var clientId = Environment.GetEnvironmentVariable("SECLAI_SSO_CLIENT_ID");
        if (string.IsNullOrWhiteSpace(clientId))
            merged.TryGetValue("sso_client_id", out clientId);
        if (string.IsNullOrWhiteSpace(clientId))
            clientId = DefaultSsoClientId;

        var region = Environment.GetEnvironmentVariable("SECLAI_SSO_REGION");
        if (string.IsNullOrWhiteSpace(region))
            merged.TryGetValue("sso_region", out region);
        if (string.IsNullOrWhiteSpace(region))
            region = DefaultSsoRegion;

        merged.TryGetValue("sso_account_id", out var accountId);

        return new SsoProfile
        {
            SsoAccountId = string.IsNullOrWhiteSpace(accountId) ? null : accountId,
            SsoRegion = region!,
            SsoClientId = clientId!,
            SsoDomain = domain!
        };
    }

    private static string ResolveCachePath(string configDir, SsoProfile profile)
    {
        var hash = CacheFileName(profile.SsoDomain, profile.SsoClientId);
        return Path.Combine(configDir, SsoCacheDir, hash + ".json");
    }

    /// <summary>Reads a cached SSO token from disk.</summary>
    /// <param name="configDir">Resolved config directory path.</param>
    /// <param name="profile">SSO profile used to derive the cache filename.</param>
    /// <returns>The cached entry, or <c>null</c> if not found or unreadable.</returns>
    public static SsoCacheEntry? ReadSsoCache(string configDir, SsoProfile profile)
    {
        var cachePath = ResolveCachePath(configDir, profile);
        if (!File.Exists(cachePath))
            return null;

        try
        {
            var json = File.ReadAllText(cachePath);
            return JsonSerializer.Deserialize<SsoCacheEntry>(json, CacheJsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>Writes a cache entry to disk, replacing any existing file.</summary>
    /// <param name="configDir">Resolved config directory path.</param>
    /// <param name="profile">SSO profile used to derive the cache filename.</param>
    /// <param name="entry">Token data to persist.</param>
    public static void WriteSsoCache(string configDir, SsoProfile profile, SsoCacheEntry entry)
    {
        var cacheDir = Path.Combine(configDir, SsoCacheDir);
        Directory.CreateDirectory(cacheDir);

        var cachePath = ResolveCachePath(configDir, profile);
        var tmpPath = cachePath + ".tmp";

        var json = JsonSerializer.Serialize(entry, CacheJsonOptions);
        File.WriteAllText(tmpPath, json);
        try
        {
            if (File.Exists(cachePath))
                File.Replace(tmpPath, cachePath, null);
            else
                File.Move(tmpPath, cachePath);
        }
        catch
        {
            try { File.Delete(tmpPath); } catch { /* best effort cleanup */ }
            throw;
        }
    }

    /// <summary>Deletes a cached SSO token file.</summary>
    /// <param name="configDir">Resolved config directory path.</param>
    /// <param name="profile">SSO profile used to derive the cache filename.</param>
    public static void DeleteSsoCache(string configDir, SsoProfile profile)
    {
        var cachePath = ResolveCachePath(configDir, profile);
        if (File.Exists(cachePath))
            File.Delete(cachePath);
    }

    /// <summary>Checks if a cached token is still valid (with 30s buffer).</summary>
    /// <param name="entry">The cached token entry to check.</param>
    /// <returns><c>true</c> if the token expires more than 30 seconds in the future.</returns>
    public static bool IsTokenValid(SsoCacheEntry entry)
    {
        if (!DateTimeOffset.TryParse(entry.ExpiresAt, out var expiresAt))
            return false;
        return DateTimeOffset.UtcNow.AddSeconds(ExpiryBufferSeconds) < expiresAt;
    }

    /// <summary>Refreshes an SSO token via the Cognito token endpoint.</summary>
    /// <param name="profile">SSO profile with Cognito domain and client ID.</param>
    /// <param name="refreshToken">The refresh token to exchange.</param>
    /// <param name="httpClient">Optional pre-configured HTTP client.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A fresh <see cref="SsoCacheEntry"/> with the new tokens.</returns>
    /// <exception cref="ConfigurationException">If the Cognito token endpoint returns a non-success status.</exception>
    public static async Task<SsoCacheEntry> RefreshTokenAsync(
        SsoProfile profile,
        string refreshToken,
        HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        var tokenUrl = $"https://{profile.SsoDomain}/oauth2/token";

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("client_id", profile.SsoClientId),
            new KeyValuePair<string, string>("refresh_token", refreshToken)
        });

        var hc = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        try
        {
            var resp = await hc.PostAsync(tokenUrl, content, cancellationToken).ConfigureAwait(false);
            var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
                throw new ConfigurationException($"Token refresh failed (HTTP {(int)resp.StatusCode}): {body}");

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            var accessToken = root.GetProperty("access_token").GetString()!;
            var expiresIn = root.GetProperty("expires_in").GetInt32();

            string? idToken = null;
            if (root.TryGetProperty("id_token", out var idProp))
                idToken = idProp.GetString();

            string? rt = null;
            if (root.TryGetProperty("refresh_token", out var rtProp))
                rt = rtProp.GetString();
            if (string.IsNullOrEmpty(rt))
                rt = refreshToken;

            var expiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn).ToString("o");

            return new SsoCacheEntry
            {
                AccessToken = accessToken,
                RefreshToken = rt,
                IdToken = idToken,
                ExpiresAt = expiresAt,
                ClientId = profile.SsoClientId,
                Region = profile.SsoRegion,
                CognitoDomain = profile.SsoDomain
            };
        }
        finally
        {
            if (httpClient == null) hc.Dispose();
        }
    }

    /// <summary>
    /// Resolves the credential chain from client options. First match wins:
    /// explicit ApiKey, AccessToken, AccessTokenProvider, SECLAI_API_KEY env, SSO profile.
    /// </summary>
    /// <param name="options">Client options containing credential configuration.</param>
    /// <returns>Resolved authentication state.</returns>
    /// <exception cref="ConfigurationException">If conflicting auth options are provided or no credentials are found.</exception>
    internal static AuthState ResolveCredentialChain(SeclaiClientOptions options)
    {
        var header = string.IsNullOrWhiteSpace(options.ApiKeyHeader) ? "x-api-key" : options.ApiKeyHeader;

        var hasApiKey = !string.IsNullOrWhiteSpace(options.ApiKey);
        var hasBearer = !string.IsNullOrWhiteSpace(options.AccessToken);
        var hasProvider = options.AccessTokenProvider != null;

        var count = 0;
        if (hasApiKey) count++;
        if (hasBearer) count++;
        if (hasProvider) count++;
        if (count > 1)
            throw new ConfigurationException("Provide only one of ApiKey, AccessToken, or AccessTokenProvider.");

        // 1. Explicit API key
        if (hasApiKey)
        {
            return new AuthState
            {
                Mode = AuthMode.ApiKey,
                ApiKey = options.ApiKey!.Trim(),
                ApiKeyHeader = header,
                AccountId = options.AccountId
            };
        }

        // 2. Static access token
        if (hasBearer)
        {
            return new AuthState
            {
                Mode = AuthMode.BearerStatic,
                AccessToken = options.AccessToken!.Trim(),
                ApiKeyHeader = header,
                AccountId = options.AccountId
            };
        }

        // 3. Provider
        if (hasProvider)
        {
            return new AuthState
            {
                Mode = AuthMode.BearerProvider,
                TokenProvider = options.AccessTokenProvider,
                ApiKeyHeader = header,
                AccountId = options.AccountId
            };
        }

        // 4. SECLAI_API_KEY env var
        var envKey = Environment.GetEnvironmentVariable("SECLAI_API_KEY");
        if (!string.IsNullOrWhiteSpace(envKey))
        {
            return new AuthState
            {
                Mode = AuthMode.ApiKey,
                ApiKey = envKey!.Trim(),
                ApiKeyHeader = header,
                AccountId = options.AccountId
            };
        }

        // 5. SSO profile (always available via built-in defaults)
        var configDir = ResolveConfigDir(options.ConfigDir);
        if (!string.IsNullOrEmpty(configDir))
        {
            var profileName = options.Profile;
            if (string.IsNullOrWhiteSpace(profileName))
                profileName = Environment.GetEnvironmentVariable("SECLAI_PROFILE");
            if (string.IsNullOrWhiteSpace(profileName))
                profileName = "default";

            var ssoProfile = LoadSsoProfile(configDir, profileName!);

            var acctId = options.AccountId;
            if (string.IsNullOrWhiteSpace(acctId))
                acctId = ssoProfile.SsoAccountId;

            return new AuthState
            {
                Mode = AuthMode.Sso,
                ApiKeyHeader = header,
                AccountId = acctId,
                SsoProfile = ssoProfile,
                ConfigDir = configDir,
                AutoRefresh = options.AutoRefresh ?? true
            };
        }

        // 6. Nothing (home directory cannot be determined)
        throw new ConfigurationException(
            "Missing credentials: provide ApiKey, AccessToken, set SECLAI_API_KEY, or run `seclai auth login`.");
    }

    /// <summary>
    /// Applies authentication headers to an outgoing HTTP request.
    /// Called per-request to handle dynamic token providers and SSO cache refresh.
    /// </summary>
    /// <param name="headers">The request headers collection to modify.</param>
    /// <param name="state">Resolved authentication state.</param>
    /// <param name="httpClient">Optional HTTP client for token refresh.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    internal static async Task ApplyAuthHeadersAsync(
        HttpRequestHeaders headers,
        AuthState state,
        HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        switch (state.Mode)
        {
            case AuthMode.ApiKey:
                headers.TryAddWithoutValidation(state.ApiKeyHeader, state.ApiKey);
                break;

            case AuthMode.BearerStatic:
                headers.Authorization = new AuthenticationHeaderValue("Bearer", state.AccessToken);
                break;

            case AuthMode.BearerProvider:
                var token = await state.TokenProvider!(cancellationToken).ConfigureAwait(false);
                headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                break;

            case AuthMode.Sso:
                var ssoToken = await ResolveSsoTokenAsync(state, httpClient, cancellationToken).ConfigureAwait(false);
                headers.Authorization = new AuthenticationHeaderValue("Bearer", ssoToken);
                break;
        }

        if (!string.IsNullOrWhiteSpace(state.AccountId))
            headers.TryAddWithoutValidation("X-Account-Id", state.AccountId);
    }

    private static async Task<string> ResolveSsoTokenAsync(
        AuthState state,
        HttpClient? httpClient,
        CancellationToken cancellationToken)
    {
        var cached = ReadSsoCache(state.ConfigDir!, state.SsoProfile!);
        if (cached != null && IsTokenValid(cached))
            return cached.AccessToken;

        if (!string.IsNullOrWhiteSpace(cached?.RefreshToken) && state.AutoRefresh)
        {
            await state.RefreshLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // Re-check after acquiring lock — another task may have refreshed
                cached = ReadSsoCache(state.ConfigDir!, state.SsoProfile!);
                if (cached != null && IsTokenValid(cached))
                    return cached.AccessToken;

                if (!string.IsNullOrWhiteSpace(cached?.RefreshToken))
                {
                    var refreshed = await RefreshTokenAsync(state.SsoProfile!, cached!.RefreshToken!, httpClient, cancellationToken).ConfigureAwait(false);
                    WriteSsoCache(state.ConfigDir!, state.SsoProfile!, refreshed);
                    return refreshed.AccessToken;
                }
            }
            finally
            {
                state.RefreshLock.Release();
            }
        }

        throw new ConfigurationException("SSO token is missing or has expired. Run `seclai auth login` to authenticate or re-authenticate.");
    }
}

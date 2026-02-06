using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minisource.Common.Auth;
using Minisource.Common.Exceptions;

namespace Minisource.Sdk.Auth;

/// <summary>
/// Auth client implementation for service-to-service authentication.
/// Handles token acquisition, caching, and refresh.
/// </summary>
public class AuthClient : IAuthClient
{
    private readonly HttpClient _httpClient;
    private readonly AuthClientOptions _options;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AuthClient> _logger;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    private const string TokenCacheKey = "auth_client_token";
    private const string TokenEndpoint = "/api/v1/oauth/token";
    private const string IntrospectEndpoint = "/api/v1/oauth/introspect";

    public AuthClient(
        HttpClient httpClient,
        IOptions<AuthClientOptions> options,
        IMemoryCache cache,
        ILogger<AuthClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _cache = cache;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/'));
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    public async Task<TokenResponse> AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Authenticating with auth service as client {ClientId}", _options.ClientId);

        var request = new ClientCredentialsRequest
        {
            ClientId = _options.ClientId,
            ClientSecret = _options.ClientSecret,
            Scope = string.Join(" ", _options.Scopes)
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(TokenEndpoint, request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Authentication failed with status {StatusCode}: {Error}",
                    response.StatusCode, errorContent);
                throw new ExternalServiceException("AuthService", $"Authentication failed: {response.StatusCode}");
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken);

            if (tokenResponse is null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                throw new ExternalServiceException("AuthService", "Invalid token response received");
            }

            tokenResponse.ObtainedAt = DateTime.UtcNow;

            _logger.LogDebug("Successfully authenticated, token expires in {ExpiresIn} seconds", tokenResponse.ExpiresIn);

            return tokenResponse;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to auth service");
            throw new ExternalServiceException("AuthService", "Failed to connect to auth service", innerException: ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse auth service response");
            throw new ExternalServiceException("AuthService", "Invalid response from auth service", innerException: ex);
        }
    }

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        // Try to get from cache first
        if (_options.EnableCaching && _cache.TryGetValue(TokenCacheKey, out TokenResponse? cachedToken))
        {
            if (cachedToken is not null && !cachedToken.IsExpired(_options.RefreshBufferSeconds))
            {
                return cachedToken.AccessToken;
            }
        }

        // Need to get a new token - use lock to prevent multiple simultaneous requests
        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check cache after acquiring lock
            if (_options.EnableCaching && _cache.TryGetValue(TokenCacheKey, out cachedToken))
            {
                if (cachedToken is not null && !cachedToken.IsExpired(_options.RefreshBufferSeconds))
                {
                    return cachedToken.AccessToken;
                }
            }

            // Get new token
            var tokenResponse = await AuthenticateAsync(cancellationToken);

            // Cache the token
            if (_options.EnableCaching)
            {
                var cacheExpiry = TimeSpan.FromSeconds(tokenResponse.ExpiresIn - _options.RefreshBufferSeconds);
                if (cacheExpiry > TimeSpan.Zero)
                {
                    _cache.Set(TokenCacheKey, tokenResponse, cacheExpiry);
                }
            }

            return tokenResponse.AccessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    public async Task<TokenIntrospectionResult?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        try
        {
            var request = new TokenIntrospectionRequest
            {
                Token = token,
                ClientId = _options.ClientId,
                ClientSecret = _options.ClientSecret
            };

            var response = await _httpClient.PostAsJsonAsync(IntrospectEndpoint, request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Token introspection failed with status {StatusCode}", response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<TokenIntrospectionResult>(cancellationToken: cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token introspection request failed");
            return null;
        }
    }

    public void InvalidateToken()
    {
        _cache.Remove(TokenCacheKey);
        _logger.LogDebug("Token cache invalidated");
    }
}

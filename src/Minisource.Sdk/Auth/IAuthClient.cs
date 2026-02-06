using Minisource.Common.Auth;

namespace Minisource.Sdk.Auth;

/// <summary>
/// Auth client interface for service-to-service authentication.
/// Similar to go-sdk/auth.Client.
/// </summary>
public interface IAuthClient
{
    /// <summary>
    /// Authenticates this service and returns an access token.
    /// Uses client credentials grant.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Token response with access token</returns>
    Task<TokenResponse> AuthenticateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a valid access token, refreshing if necessary.
    /// Caches the token and automatically refreshes before expiry.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Access token string</returns>
    Task<string> GetTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a token by introspecting it with the auth service.
    /// </summary>
    /// <param name="token">Token to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Introspection result, or null if invalid</returns>
    Task<TokenIntrospectionResult?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates the cached token, forcing a new authentication on next call.
    /// </summary>
    void InvalidateToken();
}

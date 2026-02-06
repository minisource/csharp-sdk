using System.Text.Json.Serialization;

namespace Minisource.Sdk.Auth;

/// <summary>
/// Token response from OAuth token endpoint.
/// </summary>
public sealed class TokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "Bearer";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    /// <summary>When the token was obtained</summary>
    [JsonIgnore]
    public DateTime ObtainedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Calculates when the token expires</summary>
    [JsonIgnore]
    public DateTime ExpiresAt => ObtainedAt.AddSeconds(ExpiresIn);

    /// <summary>Checks if the token is expired or about to expire</summary>
    public bool IsExpired(int bufferSeconds = 60)
    {
        return DateTime.UtcNow >= ExpiresAt.AddSeconds(-bufferSeconds);
    }
}

/// <summary>
/// Request for client credentials grant.
/// </summary>
public sealed class ClientCredentialsRequest
{
    [JsonPropertyName("grant_type")]
    public string GrantType { get; } = "client_credentials";

    [JsonPropertyName("client_id")]
    public required string ClientId { get; set; }

    [JsonPropertyName("client_secret")]
    public required string ClientSecret { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }
}

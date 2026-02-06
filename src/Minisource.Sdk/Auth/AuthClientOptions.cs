namespace Minisource.Sdk.Auth;

/// <summary>
/// Configuration options for the Auth client.
/// </summary>
public class AuthClientOptions
{
    public const string SectionName = "AuthClient";

    /// <summary>Base URL of the auth service</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Client ID for this service</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Client secret for this service</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Scopes to request when authenticating</summary>
    public string[] Scopes { get; set; } = ["*"];

    /// <summary>Request timeout in seconds</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>Token refresh buffer in seconds (refresh before expiry)</summary>
    public int RefreshBufferSeconds { get; set; } = 60;

    /// <summary>Cache tokens in memory</summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>Maximum retry attempts for failed requests</summary>
    public int MaxRetries { get; set; } = 3;
}

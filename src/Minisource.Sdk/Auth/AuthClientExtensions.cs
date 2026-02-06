using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Minisource.Sdk.Auth;

/// <summary>
/// Extension methods for registering auth client services.
/// </summary>
public static class AuthClientExtensions
{
    /// <summary>
    /// Adds the AuthClient to the service collection.
    /// </summary>
    public static IServiceCollection AddAuthClient(
        this IServiceCollection services,
        Action<AuthClientOptions> configureOptions)
    {
        services.Configure(configureOptions);
        services.AddMemoryCache();
        services.AddHttpClient<IAuthClient, AuthClient>();

        return services;
    }

    /// <summary>
    /// Adds the AuthClient using configuration section.
    /// </summary>
    public static IServiceCollection AddAuthClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AuthClientOptions>(
            configuration.GetSection(AuthClientOptions.SectionName));

        services.AddMemoryCache();
        services.AddHttpClient<IAuthClient, AuthClient>();

        return services;
    }
}

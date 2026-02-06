using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Minisource.Sdk.Auth;
using Xunit;

namespace Minisource.Sdk.Tests.Auth;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMinisourceSdk_ShouldRegisterAuthClient()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new MinisourceSdkOptions
        {
            AuthServiceUrl = "http://localhost:9001"
        };

        // Act
        services.AddMinisourceSdk(options =>
        {
            options.AuthServiceUrl = config.AuthServiceUrl;
        });

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var authClient = serviceProvider.GetService<IAuthClient>();
        authClient.Should().NotBeNull();
    }

    [Fact]
    public void AddMinisourceSdk_ShouldConfigureHttpClient()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMinisourceSdk(options =>
        {
            options.AuthServiceUrl = "http://localhost:9001";
            options.Timeout = TimeSpan.FromSeconds(60);
            options.RetryCount = 5;
        });

        // Assert - verify registration doesn't throw
        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.Should().NotBeNull();
    }

    [Fact]
    public void AddMinisourceSdk_WithInvalidUrl_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var exception = Record.Exception(() =>
        {
            services.AddMinisourceSdk(options =>
            {
                options.AuthServiceUrl = ""; // Invalid
            });
            var sp = services.BuildServiceProvider();
            sp.GetRequiredService<IAuthClient>();
        });

        exception.Should().NotBeNull();
    }
}

public class MinisourceSdkOptions
{
    public string AuthServiceUrl { get; set; } = string.Empty;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public int RetryCount { get; set; } = 3;
}

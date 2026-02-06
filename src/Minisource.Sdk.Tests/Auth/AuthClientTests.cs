using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Minisource.Sdk.Auth;
using Moq;
using System.Net;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace Minisource.Sdk.Tests.Auth;

public class AuthClientTests : IDisposable
{
    private readonly WireMockServer _mockServer;
    private readonly Mock<ILogger<AuthClient>> _loggerMock;
    private readonly AuthClient _authClient;

    public AuthClientTests()
    {
        _mockServer = WireMockServer.Start();
        _loggerMock = new Mock<ILogger<AuthClient>>();

        var services = new ServiceCollection();
        services.AddHttpClient<AuthClient>(client =>
        {
            client.BaseAddress = new Uri(_mockServer.Url!);
        });
        services.AddSingleton(_loggerMock.Object);

        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient(nameof(AuthClient));

        _authClient = new AuthClient(httpClient, _loggerMock.Object);
    }

    public void Dispose()
    {
        _mockServer.Stop();
        _mockServer.Dispose();
    }

    [Fact]
    public async Task ValidateToken_ShouldReturnTrue_WhenTokenIsValid()
    {
        // Arrange
        _mockServer
            .Given(Request.Create().WithPath("/api/v1/auth/validate").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("{\"valid\": true, \"user_id\": \"user-123\"}"));

        // Act
        var result = await _authClient.ValidateTokenAsync("valid-token");

        // Assert
        result.IsValid.Should().BeTrue();
        result.UserId.Should().Be("user-123");
    }

    [Fact]
    public async Task ValidateToken_ShouldReturnFalse_WhenTokenIsInvalid()
    {
        // Arrange
        _mockServer
            .Given(Request.Create().WithPath("/api/v1/auth/validate").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithBody("{\"valid\": false, \"error\": \"Token expired\"}"));

        // Act
        var result = await _authClient.ValidateTokenAsync("invalid-token");

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task GetUserInfo_ShouldReturnUser_WhenExists()
    {
        // Arrange
        _mockServer
            .Given(Request.Create().WithPath("/api/v1/users/user-123").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("{\"id\": \"user-123\", \"email\": \"test@example.com\", \"name\": \"Test User\"}"));

        // Act
        var result = await _authClient.GetUserInfoAsync("user-123");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("user-123");
        result.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetUserInfo_ShouldReturnNull_WhenNotFound()
    {
        // Arrange
        _mockServer
            .Given(Request.Create().WithPath("/api/v1/users/non-existent").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(404));

        // Act
        var result = await _authClient.GetUserInfoAsync("non-existent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RefreshToken_ShouldReturnNewToken_WhenValid()
    {
        // Arrange
        _mockServer
            .Given(Request.Create().WithPath("/api/v1/auth/refresh").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("{\"access_token\": \"new-token\", \"expires_in\": 3600}"));

        // Act
        var result = await _authClient.RefreshTokenAsync("old-refresh-token");

        // Assert
        result.Should().NotBeNull();
        result!.AccessToken.Should().Be("new-token");
        result.ExpiresIn.Should().Be(3600);
    }

    [Fact]
    public async Task RefreshToken_ShouldReturnNull_WhenInvalid()
    {
        // Arrange
        _mockServer
            .Given(Request.Create().WithPath("/api/v1/auth/refresh").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(401));

        // Act
        var result = await _authClient.RefreshTokenAsync("invalid-refresh-token");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateToken_ShouldHandleTimeout()
    {
        // Arrange
        _mockServer
            .Given(Request.Create().WithPath("/api/v1/auth/validate").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithDelay(TimeSpan.FromSeconds(35))); // Longer than typical timeout

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
            await _authClient.ValidateTokenAsync("token", cts.Token);
        });
    }
}

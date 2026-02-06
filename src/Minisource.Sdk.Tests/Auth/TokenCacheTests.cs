using FluentAssertions;
using Microsoft.Extensions.Logging;
using Minisource.Sdk.Auth;
using Moq;
using Xunit;

namespace Minisource.Sdk.Tests.Auth;

public class TokenCacheTests
{
    private readonly Mock<ILogger<TokenCache>> _loggerMock;
    private readonly TokenCache _tokenCache;

    public TokenCacheTests()
    {
        _loggerMock = new Mock<ILogger<TokenCache>>();
        _tokenCache = new TokenCache(_loggerMock.Object);
    }

    [Fact]
    public void Set_ShouldStoreToken()
    {
        // Arrange
        var key = "user-123";
        var token = "access-token-abc";

        // Act
        _tokenCache.Set(key, token, TimeSpan.FromMinutes(5));

        // Assert
        var result = _tokenCache.Get(key);
        result.Should().Be(token);
    }

    [Fact]
    public void Get_ShouldReturnNull_WhenNotFound()
    {
        // Arrange
        var key = "non-existent-key";

        // Act
        var result = _tokenCache.Get(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Get_ShouldReturnNull_WhenExpired()
    {
        // Arrange
        var key = "user-123";
        var token = "access-token-abc";
        _tokenCache.Set(key, token, TimeSpan.FromMilliseconds(50));

        // Wait for expiration
        await Task.Delay(100);

        // Act
        var result = _tokenCache.Get(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Remove_ShouldDeleteToken()
    {
        // Arrange
        var key = "user-123";
        var token = "access-token-abc";
        _tokenCache.Set(key, token, TimeSpan.FromMinutes(5));

        // Act
        _tokenCache.Remove(key);

        // Assert
        var result = _tokenCache.Get(key);
        result.Should().BeNull();
    }

    [Fact]
    public void TryGet_ShouldReturnTrueAndValue_WhenExists()
    {
        // Arrange
        var key = "user-123";
        var token = "access-token-abc";
        _tokenCache.Set(key, token, TimeSpan.FromMinutes(5));

        // Act
        var exists = _tokenCache.TryGet(key, out var result);

        // Assert
        exists.Should().BeTrue();
        result.Should().Be(token);
    }

    [Fact]
    public void TryGet_ShouldReturnFalse_WhenNotExists()
    {
        // Arrange
        var key = "non-existent";

        // Act
        var exists = _tokenCache.TryGet(key, out var result);

        // Assert
        exists.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void Clear_ShouldRemoveAllTokens()
    {
        // Arrange
        _tokenCache.Set("key1", "token1", TimeSpan.FromMinutes(5));
        _tokenCache.Set("key2", "token2", TimeSpan.FromMinutes(5));
        _tokenCache.Set("key3", "token3", TimeSpan.FromMinutes(5));

        // Act
        _tokenCache.Clear();

        // Assert
        _tokenCache.Get("key1").Should().BeNull();
        _tokenCache.Get("key2").Should().BeNull();
        _tokenCache.Get("key3").Should().BeNull();
    }

    [Fact]
    public void GetOrSet_ShouldReturnExisting_WhenCached()
    {
        // Arrange
        var key = "user-123";
        var existingToken = "existing-token";
        _tokenCache.Set(key, existingToken, TimeSpan.FromMinutes(5));
        var factoryCalled = false;

        // Act
        var result = _tokenCache.GetOrSet(key, () =>
        {
            factoryCalled = true;
            return "new-token";
        }, TimeSpan.FromMinutes(5));

        // Assert
        result.Should().Be(existingToken);
        factoryCalled.Should().BeFalse();
    }

    [Fact]
    public void GetOrSet_ShouldCallFactory_WhenNotCached()
    {
        // Arrange
        var key = "new-user";
        var newToken = "new-token";

        // Act
        var result = _tokenCache.GetOrSet(key, () => newToken, TimeSpan.FromMinutes(5));

        // Assert
        result.Should().Be(newToken);
        _tokenCache.Get(key).Should().Be(newToken);
    }
}

# Minisource C# SDK

Official C# SDK for Minisource microservices. Provides typed clients for service-to-service communication in .NET applications.

## Installation

```bash
# Via NuGet (when published)
dotnet add package Minisource.Sdk

# Via project reference
dotnet add reference ../csharp-sdk/src/Minisource.Sdk
```

## Available Clients

| Service | Client | Status |
|---------|--------|--------|
| Auth | `AuthClient` | ✅ Available |
| Notifier | `NotifierClient` | 🚧 Planned |
| Storage | `StorageClient` | 🚧 Planned |
| Comment | `CommentClient` | 🚧 Planned |
| Log | `LogClient` | 🚧 Planned |
| Scheduler | `SchedulerClient` | 🚧 Planned |

## Quick Start

### Auth Client

```csharp
using Minisource.Sdk.Auth;

// Configure services
services.AddMinisourceAuth(options =>
{
    options.BaseUrl = "http://auth:9001";
    options.ClientId = "my-service";
    options.ClientSecret = "secret";
});

// Inject and use
public class MyService
{
    private readonly IAuthClient _authClient;
    
    public MyService(IAuthClient authClient)
    {
        _authClient = authClient;
    }
    
    public async Task<User> GetCurrentUser(string token)
    {
        var claims = await _authClient.ValidateTokenAsync(token);
        return new User { Id = claims.UserId, Email = claims.Email };
    }
}
```

### Service Authentication

```csharp
// Get token for service-to-service calls
var token = await _authClient.GetServiceTokenAsync();

// Use with HttpClient
httpClient.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", token);
```

### Token Validation

```csharp
// Validate user token
var result = await _authClient.ValidateTokenAsync(userToken);

if (result.IsValid)
{
    var userId = result.Claims.UserId;
    var tenantId = result.Claims.TenantId;
    var roles = result.Claims.Roles;
}
```

## Configuration

### appsettings.json

```json
{
  "Minisource": {
    "Auth": {
      "BaseUrl": "http://auth:9001",
      "ClientId": "my-service",
      "ClientSecret": "your-secret",
      "Timeout": "00:00:30",
      "AutoRefresh": true
    },
    "Notifier": {
      "BaseUrl": "http://notifier:9002",
      "GrpcAddress": "notifier:9003"
    },
    "Storage": {
      "BaseUrl": "http://storage:5004"
    }
  }
}
```

### Dependency Injection

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Add all clients
    services.AddMinisourceSdk(Configuration);
    
    // Or add individually
    services.AddMinisourceAuth(Configuration);
    services.AddMinisourceNotifier(Configuration);
    services.AddMinisourceStorage(Configuration);
}
```

## Client Features

### Retry Policy

All clients include automatic retry with exponential backoff:

```csharp
services.AddMinisourceAuth(options =>
{
    options.MaxRetries = 3;
    options.RetryDelay = TimeSpan.FromMilliseconds(500);
});
```

### Circuit Breaker

Built-in circuit breaker for fault tolerance:

```csharp
services.AddMinisourceAuth(options =>
{
    options.CircuitBreaker = new CircuitBreakerOptions
    {
        FailureThreshold = 5,
        SamplingDuration = TimeSpan.FromSeconds(30),
        BreakDuration = TimeSpan.FromSeconds(60)
    };
});
```

### Logging

Clients integrate with Microsoft.Extensions.Logging:

```csharp
services.AddMinisourceAuth(options =>
{
    options.EnableRequestLogging = true;
    options.EnableResponseLogging = false; // Don't log sensitive data
});
```

## Models

### TokenClaims

```csharp
public class TokenClaims
{
    public string UserId { get; set; }
    public string TenantId { get; set; }
    public string Email { get; set; }
    public List<string> Roles { get; set; }
    public DateTime ExpiresAt { get; set; }
}
```

### AuthResponse

```csharp
public class AuthResponse
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public UserInfo User { get; set; }
}
```

## Error Handling

```csharp
try
{
    var result = await _authClient.ValidateTokenAsync(token);
}
catch (AuthenticationException ex)
{
    // Token invalid or expired
    _logger.LogWarning("Auth failed: {Message}", ex.Message);
}
catch (ServiceUnavailableException ex)
{
    // Auth service unreachable
    _logger.LogError("Auth service unavailable: {Message}", ex.Message);
}
```

## Testing

Mock clients are provided for testing:

```csharp
// In tests
services.AddSingleton<IAuthClient>(new MockAuthClient
{
    ValidateResult = new TokenValidationResult
    {
        IsValid = true,
        Claims = new TokenClaims { UserId = "test-user" }
    }
});
```

## Building

```bash
# Build
dotnet build

# Test
dotnet test

# Pack for NuGet
dotnet pack -c Release
```

## Project Structure

```
Minisource.Sdk/
├── Auth/
│   ├── AuthClient.cs
│   ├── AuthOptions.cs
│   ├── IAuthClient.cs
│   └── Models/
├── Notifier/        # Coming soon
├── Storage/         # Coming soon
├── Common/
│   ├── BaseClient.cs
│   └── HttpClientFactory.cs
└── Extensions/
    └── ServiceCollectionExtensions.cs
```

## Dependencies

- .NET 10
- Microsoft.Extensions.Http
- Microsoft.Extensions.Options
- Polly (for retry/circuit breaker)

## License

MIT

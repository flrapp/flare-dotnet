# Flare.HttpClient

Standalone HTTP client library for the [Flare](https://github.com/flrapp/flare-api) feature flag API. No dependency on OpenFeature or any other provider framework.

## Installation

```bash
dotnet add package Flare.HttpClient
```

## Quick Start

```csharp
services.AddFlareHttpClient(new FlareApiClientOptions
{
    BaseUrl = "https://flare.example.com",
    ApiKey = "your-api-key",
    Scope = "production"
});
```

Then inject `IFlareApiClient`:

```csharp
public class MyService
{
    private readonly IFlareApiClient _client;

    public MyService(IFlareApiClient client) => _client = client;

    public async Task<bool> IsFlagEnabled(string flagKey)
    {
        var context = new FlareEvaluationContext
        {
            Scope = "production",
            TargetingKey = "user-123"
        };

        var result = await _client.EvaluateAsync(flagKey, context);
        return result.Value;
    }

    public async Task<IReadOnlyList<FlagEvaluationResponse>> GetAllFlags()
    {
        var context = new FlareEvaluationContext { Scope = "production" };
        var result = await _client.EvaluateAllAsync(context);
        return result.Flags;
    }
}
```

## API

### `IFlareApiClient`

| Method | Endpoint | Description |
|--------|----------|-------------|
| `EvaluateAsync(flagKey, context)` | `POST /sdk/v1/flags/evaluate` | Evaluate a single flag |
| `EvaluateAllAsync(context)` | `POST /sdk/v1/flags/evaluate-all` | Evaluate all flags for a scope |

### `FlareApiClientOptions`

| Option | Type | Description |
|--------|------|-------------|
| `BaseUrl` | `string` | Flare API base URL (required) |
| `ApiKey` | `string` | API key for Bearer authentication (required) |
| `Scope` | `string` | Default scope/environment for flag evaluation |

### Error Handling

- `FlareApiException` — thrown for non-success API responses, includes `HttpStatusCode`
- `HttpRequestException` — thrown for network-level errors
- `JsonException` — thrown for response deserialization failures

## License

MIT License - see [LICENSE](LICENSE) for details.

# Flare.OpenFeature.Provider

[OpenFeature](https://openfeature.dev/) provider for [Flare](https://github.com/flrapp/flare-api) feature flag management system. Supports in-memory caching with background polling, provider lifecycle events, and automatic context conversion.

## Installation

```bash
dotnet add package Flare.OpenFeature.Provider
```

## Quick Start

```csharp
services.AddOpenFeature(builder =>
{
    builder.AddFlareProvider(new FlareApiClientOptions
    {
        BaseUrl = "https://flare.example.com",
        ApiKey = "your-api-key",
        Scope = "production"
    });
});
```

### With `IConfiguration` binding

```json
{
  "Flare": {
    "BaseUrl": "https://flare.example.com",
    "ApiKey": "your-api-key",
    "Scope": "production"
  }
}
```

```csharp
services.AddOpenFeature(builder =>
{
    builder.AddFlareProvider(configuration);
});
```

### With custom caching options

```csharp
services.AddOpenFeature(builder =>
{
    builder.AddFlareProvider(
        new FlareApiClientOptions
        {
            BaseUrl = "https://flare.example.com",
            ApiKey = "your-api-key",
            Scope = "production"
        },
        new FlareProviderOptions
        {
            PollingInterval = TimeSpan.FromSeconds(15),
            StaleThreshold = 5,
            CachingEnabled = true
        });
});
```

## Evaluating Flags

```csharp
public class MyService
{
    private readonly IFeatureClient _client;

    public MyService(IFeatureClient client) => _client = client;

    public async Task DoWork()
    {
        // boolean flag
        var isEnabled = await _client.GetBooleanValueAsync("my-flag", false);

        // string flag
        var theme = await _client.GetStringValueAsync("ui-theme", "default");

        // number flags
        var timeout = await _client.GetIntegerValueAsync("request-timeout", 30);
        var rate = await _client.GetDoubleValueAsync("sample-rate", 0.1);

        // json flag — returns OpenFeature Value (supports nested objects/arrays)
        var config = await _client.GetObjectValueAsync("feature-config", new Value());
    }
}
```

## Features

### Caching and Background Polling

When caching is enabled (default), the provider:

1. Fetches all flags on initialization and populates an in-memory cache
2. Resolves flags from cache for near-instant evaluations
3. Polls the Flare API in the background to keep the cache fresh
4. Emits OpenFeature events on flag changes, errors, and staleness

Cache reads are lock-free using volatile reference swap for minimal overhead on the hot path.

### Provider Events

The provider emits standard OpenFeature events:

| Event | When |
|-------|------|
| `ProviderConfigurationChanged` | Background poll detects flag value or variant changes |
| `ProviderError` | A poll attempt fails |
| `ProviderStale` | Consecutive poll failures exceed the stale threshold |

### Direct Evaluation Fallback

When caching is disabled, flags are evaluated directly against the Flare API per request. Errors are surfaced as OpenFeature `FeatureProviderException` subtypes (`GeneralException`, `ProviderNotReadyException`, `ParseErrorException`, etc.), which the SDK handles automatically.

## Configuration Options

### `FlareApiClientOptions`

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `BaseUrl` | `string` | Required | Flare API base URL |
| `ApiKey` | `string` | Required | API key for authentication |
| `Scope` | `string` | Required | Scope/environment (e.g., `"production"`) |

### `FlareProviderOptions`

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `PollingInterval` | `TimeSpan` | `30s` | Background poll interval |
| `StaleThreshold` | `int` | `3` | Consecutive failures before emitting `ProviderStale` |
| `CachingEnabled` | `bool` | `true` | Enable in-memory caching with background polling |

## Supported Value Types

All Flare flag types are supported:

| Flare type | OpenFeature method | .NET type |
|------------|--------------------|-----------|
| `boolean` | `GetBooleanValueAsync` | `bool` |
| `string` | `GetStringValueAsync` | `string` |
| `number` | `GetIntegerValueAsync` | `int` |
| `number` | `GetDoubleValueAsync` | `double` |
| `json` | `GetObjectValueAsync` | `Value` (OpenFeature) |

Requesting a flag with an incompatible type (e.g., calling `GetBooleanValueAsync` on a `json` flag) throws `TypeMismatchException`.

## License

MIT License - see [LICENSE](LICENSE) for details.

# Flare.Extensions.Configuration

Configuration provider for [Flare](https://github.com/flrapp/flare-api) feature flag management system. Enables loading feature flags as configuration values with support for auto-reload and multiple environments.

## Installation
```bash
dotnet add package Flare.Extensions.Configuration
```

## Quick Start
```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Flare configuration provider
builder.Configuration.AddFlareConfiguration();

// Add the background service that polls for flag updates
builder.Services.AddFlareBackgroundService(options =>
{
    options.ServerUrl = "https://flare.example.com";
    options.ApiKey = "your-api-key";
    options.ScopeAlias = "production";
    options.ReloadInterval = TimeSpan.FromMinutes(5);
});

var app = builder.Build();

// Access feature flags through IConfiguration
var isEnabled = app.Configuration.GetValue<bool>("FeatureFlags:new-feature");
```

## Configuration from appsettings.json

You can also configure the provider using a configuration section:

```json
{
  "Flare": {
    "ServerUrl": "https://flare.example.com",
    "ApiKey": "your-api-key",
    "ScopeAlias": "production",
    "ReloadInterval": "00:05:00",
    "FeatureFlagSection": "FeatureFlags"
  }
}
```

```csharp
builder.Configuration.AddFlareConfiguration();
builder.Services.AddFlareBackgroundService(builder.Configuration, "Flare");
```

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ServerUrl` | string | Required | Flare server URL |
| `ApiKey` | string | Required | Project API key |
| `ScopeAlias` | string | Required | Scope/environment (e.g., "production", "staging") |
| `ReloadInterval` | TimeSpan | `TimeSpan.Zero` | Interval for polling flag updates |
| `FeatureFlagSection` | string | `"FeatureFlags"` | Configuration section prefix for flags |

## How It Works

1. `AddFlareConfiguration()` registers a configuration provider that listens for flag updates
2. `AddFlareBackgroundService()` starts a hosted service that periodically fetches flags from the Flare API
3. Flags are exposed under the configured section (default: `FeatureFlags:{flag-key}`)
4. Configuration change tokens allow `IOptionsSnapshot<T>` and `IOptionsMonitor<T>` to react to updates

## Features

- ASP.NET Core integration - Works seamlessly with `IConfiguration`
- Auto-reload - Background service periodically refreshes feature flags
- Multi-environment - Support for dev, staging, production scopes via `ScopeAlias`
- Change notifications - Triggers `IOptionsMonitor<T>` callbacks on flag updates
- Logging support - Built-in logging for monitoring reload operations

## License

MIT License - see [LICENSE](LICENSE) for details.

## Related Projects

- [Flare Server](https://github.com/flrapp/flare-api) - Self-hosted feature flag management
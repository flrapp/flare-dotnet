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
builder.Configuration.AddFlare(options =>
{
    options.ServerUrl = "https://flare.example.com";
    options.ApiKey = "your-api-key";
    options.ProjectAlias = "my-project";
    options.ScopeAlias = "production";
});

var app = builder.Build();

// Access feature flags through IConfiguration
var isEnabled = app.Configuration.GetValue("FeatureFlags:new-feature");
```

## Configuration Options
```csharp
builder.Configuration.AddFlare(options =>
{
    options.ServerUrl = "https://flare.example.com";  // Required: Flare server URL
    options.ApiKey = "your-api-key";                  // Required: Project API key
    options.ProjectAlias = "my-project";              // Required: Project alias
    options.ScopeAlias = "production";                // Required: Scope (environment)
    options.ReloadInterval = TimeSpan.FromMinutes(5); // Optional: Auto-reload interval
    options.Optional = true;                          // Optional: Don't fail if server unavailable
});
```

## Features

- ✅ **ASP.NET Core integration** - Works seamlessly with `IConfiguration`
- ✅ **Auto-reload** - Periodically refresh feature flags without restart
- ✅ **Multi-environment** - Support for dev, staging, production scopes
- ✅ **Optional loading** - Graceful degradation if Flare server unavailable
- ✅ **Logging support** - Built-in logging for monitoring

## License

MIT License - see [LICENSE](LICENSE) for details.

## Related Projects

- [Flare Server](https://github.com/flrapp/flare-api) - Self-hosted feature flag management
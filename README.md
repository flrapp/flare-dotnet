# flare-dotnet

.NET libraries for [Flare](https://github.com/flrapp/flare-api) feature flag management system.

## Packages

| Package | Description | NuGet |
|---------|-------------|-------|
| [Flare.HttpClient](src/Flare.HttpClient) | Standalone HTTP client for the Flare API | [![NuGet](https://img.shields.io/nuget/v/Flare.HttpClient)](https://www.nuget.org/packages/Flare.HttpClient) |
| [Flare.OpenFeature.Provider](src/Flare.OpenFeature.Provider) | OpenFeature provider with caching and background polling | [![NuGet](https://img.shields.io/nuget/v/Flare.OpenFeature.Provider)](https://www.nuget.org/packages/Flare.OpenFeature.Provider) |
| [Flare.Extensions.Configuration](src/Flare.Extensions.Configuration) | ASP.NET Core configuration provider | [![NuGet](https://img.shields.io/nuget/v/Flare.Extensions.Configuration)](https://www.nuget.org/packages/Flare.Extensions.Configuration) |

## Quick Start

### OpenFeature Provider

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

### Configuration Provider

```csharp
builder.Configuration.AddFlareConfiguration();
builder.Services.AddFlareBackgroundService(options =>
{
    options.ServerUrl = "https://flare.example.com";
    options.ApiKey = "your-api-key";
    options.ScopeAlias = "production";
});
```

### Standalone HTTP Client

```csharp
services.AddFlareHttpClient(new FlareApiClientOptions
{
    BaseUrl = "https://flare.example.com",
    ApiKey = "your-api-key",
    Scope = "production"
});
```

## License

MIT License - see [LICENSE](LICENSE) for details.

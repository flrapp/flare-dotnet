---
name: flare-dotnet-patterns
description: Coding patterns extracted from flare-dotnet repository
version: 1.0.0
source: local-git-analysis
analyzed_commits: 14
---

# Flare .NET SDK Patterns

## Commit Conventions

This project uses **prefixed conventional commits**:
- `feature:` - New features and capabilities
- `fixes:` - Bug fixes
- `chore:` - Maintenance, renames, upgrades
- Unprefixed messages used occasionally for minor changes

Examples:
```
feature: add standalone api client and refactor Flare.OpenFeature.Provider
fixes: Flare.Configuration.Provider fatal exception fix
chore: rename projects
chore: Upgrade to .NET 10
```

## Code Architecture

```
src/
├── Flare.HttpClient/               # Shared HTTP client (netstandard2.0)
│   ├── IFlareApiClient.cs          # Interface for API calls
│   ├── FlareApiClient.cs           # Internal implementation
│   ├── FlareApiClientOptions.cs    # Options class
│   ├── FlareApiException.cs        # Custom exception
│   ├── FlareHttpClientExtensions.cs # DI registration
│   └── Models/                     # Request/response DTOs
├── Flare.OpenFeature.Provider/     # OpenFeature integration (netstandard2.0)
│   ├── FlareProvider.cs            # Main provider (sealed)
│   ├── FlareProviderOptions.cs     # Provider config
│   ├── FlagCache.cs                # Internal cache (internal sealed)
│   ├── EvaluationContextConverter.cs # Internal converter (internal static)
│   └── OpenFeatureBuilderExtensions.cs # DI extensions
├── Flare.Extensions.Configuration/ # ASP.NET config provider (net10.0)
│   ├── FlareBackgroundService.cs
│   ├── FlareConfigurationProvider.cs
│   └── ...
tests/
└── Flare.OpenFeature.Provider.Tests/ # xUnit + NSubstitute + FluentAssertions
```

## Key Patterns

### Project Naming & Structure
- Each NuGet package lives under `src/Flare.{Name}/`
- Test projects under `tests/Flare.{Name}.Tests/`
- Solution file: `.slnx` format (XML-based)
- Projects grouped into `/src/` and `/tests/` solution folders

### Target Framework Strategy
- Shared libraries: `netstandard2.0` for broad compatibility
- App-specific projects: `net10.0`
- `LangVersion` set to `latest` across all projects

### Dependency Injection Pattern
- Extension methods on framework builders (`OpenFeatureBuilder`, `IConfigurationBuilder`)
- Two overloads: explicit options object + configuration-binding variant
- Internal `*Core()` method for shared registration logic
- Options pattern (`IOptions<T>`) for all configuration

### Class Visibility
- Public API surface: `public sealed class` for providers, `public class` for options
- Internal implementation: `internal sealed class` for caches, `internal static class` for converters
- `InternalsVisibleTo` for test projects

### Error Handling
- Custom exception types inheriting from framework base (`FlareApiException : Exception`)
- Exception mapping in providers (e.g., `HttpRequestException` -> `ProviderNotReadyException`)
- Comprehensive catch blocks ordered by specificity
- Logging via `ILogger<T>` with structured parameters

### Thread Safety
- `volatile` for flag fields and dictionary references
- `SemaphoreSlim` for exclusive async locks
- `Interlocked` for atomic counter operations
- Snapshot reads on volatile references

### HTTP Client
- `IHttpClientFactory` via `Microsoft.Extensions.Http`
- Bearer token authentication
- 30-second timeout
- `System.Text.Json` with camelCase naming policy

## Workflows

### Adding a New NuGet Package
1. Create `src/Flare.{Name}/Flare.{Name}.csproj` targeting `netstandard2.0`
2. Add extension methods for DI registration
3. Add project to `Flare.Dotnet.slnx` under `/src/` folder
4. Add release workflow in `.github/workflows/release-flare-{name}.yml`
5. Add `README.md` for NuGet package page

### Adding Tests for a Package
1. Create `tests/Flare.{Name}.Tests/Flare.{Name}.Tests.csproj` targeting `net10.0`
2. Add `InternalsVisibleTo` to source project's `.csproj`
3. Add test project to `Flare.Dotnet.slnx` under `/tests/` folder
4. Use xUnit (`[Fact]`, `[Theory]`), NSubstitute for mocks, FluentAssertions for assertions
5. Add `GlobalUsings.cs` with `global using Xunit;`

### Refactoring / Renaming a Package
1. Rename project folder and `.csproj`
2. Update namespace in all `.cs` files
3. Update `Flare.Dotnet.slnx` references
4. Update dependent project references

## Testing Patterns

- **Framework**: xUnit 2.x
- **Mocking**: NSubstitute 5.x
- **Assertions**: FluentAssertions 8.x
- **Test runner**: Microsoft.NET.Test.Sdk 17.x
- **Naming**: `MethodName_Condition_ExpectedResult`
- **Organization**: Tests grouped by `#region` per concern area
- **Theory data**: `[InlineData]` for parameterized tests
- **No IDisposable on test classes** unless needed; cleanup via explicit `ShutdownAsync()` calls

## CI/CD

### Release Workflow Pattern
- Triggered by tag push: `release/flare-{name}/*.*.*`
- Manual trigger with version input
- Gate job with `nuget-publish` environment approval
- Reusable workflow from org `.github` repo: `nuget-publish.yml`

### Test Workflow
- Runs on push to `main` and PRs targeting `main`
- Restores, builds, tests with `.trx` output
- Uploads test results as artifacts

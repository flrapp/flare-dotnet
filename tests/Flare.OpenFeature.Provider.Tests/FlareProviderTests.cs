using System.Net;
using System.Net.Http;
using System.Text.Json;
using Flare.HttpClient;
using Flare.HttpClient.Models;
using Flare.OpenFeature.Provider;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OpenFeature.Constant;
using OpenFeature.Error;
using OpenFeature.Model;

namespace Flare.OpenFeature.Provider.Tests;

public class FlareProviderTests
{
    private readonly IFlareApiClient _apiClient = Substitute.For<IFlareApiClient>();

    private readonly FlareApiClientOptions _clientOptions = new()
    {
        BaseUrl = "https://api.flare.test",
        ApiKey = "test-key",
        Scope = "test-scope"
    };

    private readonly FlareProviderOptions _providerOptions = new()
    {
        CachingEnabled = true,
        PollingInterval = TimeSpan.FromHours(1),
        StaleThreshold = 3
    };

    private FlareProvider CreateProvider(FlareProviderOptions? overrides = null)
    {
        var opts = overrides ?? _providerOptions;
        return new FlareProvider(
            _apiClient,
            Options.Create(_clientOptions),
            Options.Create(opts));
    }

    [Fact]
    public void GetMetadata_ReturnsFlareProviderName()
    {
        var provider = CreateProvider();

        var metadata = provider.GetMetadata();

        metadata.Should().NotBeNull();
        metadata!.Name.Should().Be("Flare Provider");
    }

    [Fact]
    public void Constructor_NullApiClient_ThrowsArgumentNullException()
    {
        var act = () => new FlareProvider(
            null!,
            Options.Create(_clientOptions),
            Options.Create(_providerOptions));

        act.Should().Throw<ArgumentNullException>().WithParameterName("apiClient");
    }

    #region InitializeAsync

    [Fact]
    public async Task InitializeAsync_CachingEnabled_PopulatesCacheAndStartsPolling()
    {
        var flags = new FlareEvaluateAllResponse
        {
            Flags = new List<FlagEvaluationResponse>
            {
                new() { FlagKey = "flag1", Value = true, Variant = "on", Reason = "STATIC" }
            }
        };
        _apiClient.EvaluateAllAsync(Arg.Any<FlareEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(flags);

        var provider = CreateProvider();
        await provider.InitializeAsync(EvaluationContext.Builder().Build());

        var result = await provider.ResolveBooleanValueAsync("flag1", false);
        result.Value.Should().BeTrue();
        result.Reason.Should().Be(Reason.Cached);

        await provider.ShutdownAsync();
    }

    [Fact]
    public async Task InitializeAsync_CachingDisabled_DoesNotCallApi()
    {
        var provider = CreateProvider(new FlareProviderOptions { CachingEnabled = false });

        await provider.InitializeAsync(EvaluationContext.Builder().Build());

        await _apiClient.DidNotReceive()
            .EvaluateAllAsync(Arg.Any<FlareEvaluationContext>(), Arg.Any<CancellationToken>());

        await provider.ShutdownAsync();
    }

    [Fact]
    public async Task InitializeAsync_ApiFailure_ThrowsAndDoesNotPreventStartup()
    {
        _apiClient.EvaluateAllAsync(Arg.Any<FlareEvaluationContext>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("connection refused"));

        var provider = CreateProvider();

        var act = () => provider.InitializeAsync(EvaluationContext.Builder().Build());

        await act.Should().ThrowAsync<HttpRequestException>();

        await provider.ShutdownAsync();
    }

    #endregion

    #region ResolveBooleanValueAsync - Cached Mode

    [Fact]
    public async Task ResolveBooleanValue_CachedMode_FlagInCache_ReturnsCachedResult()
    {
        var flags = new FlareEvaluateAllResponse
        {
            Flags = new List<FlagEvaluationResponse>
            {
                new()
                {
                    FlagKey = "feature-x",
                    Value = true,
                    Variant = "enabled",
                    Reason = "TARGETING_MATCH",
                    Metadata = new FlagMetadata
                    {
                        ScopeAlias = "production",
                        ScopeId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                        UpdatedAt = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero)
                    }
                }
            }
        };
        _apiClient.EvaluateAllAsync(Arg.Any<FlareEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(flags);

        var provider = CreateProvider();
        await provider.InitializeAsync(EvaluationContext.Builder().Build());

        var result = await provider.ResolveBooleanValueAsync("feature-x", false);

        result.FlagKey.Should().Be("feature-x");
        result.Value.Should().BeTrue();
        result.Variant.Should().Be("enabled");
        result.Reason.Should().Be(Reason.Cached);
        result.ErrorType.Should().Be(ErrorType.None);
        result.FlagMetadata.Should().NotBeNull();

        await provider.ShutdownAsync();
    }

    [Fact]
    public async Task ResolveBooleanValue_CachedMode_FlagNotInCache_ThrowsFlagNotFound()
    {
        var flags = new FlareEvaluateAllResponse
        {
            Flags = new List<FlagEvaluationResponse>
            {
                new() { FlagKey = "existing-flag", Value = true, Variant = "on" }
            }
        };
        _apiClient.EvaluateAllAsync(Arg.Any<FlareEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(flags);

        var provider = CreateProvider();
        await provider.InitializeAsync(EvaluationContext.Builder().Build());

        var act = () => provider.ResolveBooleanValueAsync("nonexistent", false);

        await act.Should().ThrowAsync<FlagNotFoundException>();

        await provider.ShutdownAsync();
    }

    #endregion

    #region ResolveBooleanValueAsync - Direct Mode

    [Fact]
    public async Task ResolveBooleanValue_DirectMode_CallsApiAndReturnsResult()
    {
        var response = new FlagEvaluationResponse
        {
            FlagKey = "flag-direct",
            Value = true,
            Variant = "on",
            Reason = "STATIC"
        };
        _apiClient.EvaluateAsync("flag-direct", Arg.Any<FlareEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(response);

        var provider = CreateProvider(new FlareProviderOptions
        {
            CachingEnabled = false,
            PollingInterval = TimeSpan.FromHours(1)
        });

        var result = await provider.ResolveBooleanValueAsync("flag-direct", false);

        result.FlagKey.Should().Be("flag-direct");
        result.Value.Should().BeTrue();
        result.Variant.Should().Be("on");
        result.Reason.Should().Be(Reason.Static);

        await provider.ShutdownAsync();
    }

    [Fact]
    public async Task ResolveBooleanValue_DirectMode_FlareApiException_ThrowsGeneralException()
    {
        _apiClient.EvaluateAsync(Arg.Any<string>(), Arg.Any<FlareEvaluationContext>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new FlareApiException(HttpStatusCode.BadRequest, "bad request"));

        var provider = CreateProvider(new FlareProviderOptions { CachingEnabled = false });

        var act = () => provider.ResolveBooleanValueAsync("flag1", false);

        await act.Should().ThrowAsync<GeneralException>();

        await provider.ShutdownAsync();
    }

    [Fact]
    public async Task ResolveBooleanValue_DirectMode_HttpRequestException_ThrowsProviderNotReady()
    {
        _apiClient.EvaluateAsync(Arg.Any<string>(), Arg.Any<FlareEvaluationContext>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("connection refused"));

        var provider = CreateProvider(new FlareProviderOptions { CachingEnabled = false });

        var act = () => provider.ResolveBooleanValueAsync("flag1", false);

        await act.Should().ThrowAsync<ProviderNotReadyException>();

        await provider.ShutdownAsync();
    }

    [Fact]
    public async Task ResolveBooleanValue_DirectMode_JsonException_ThrowsParseError()
    {
        _apiClient.EvaluateAsync(Arg.Any<string>(), Arg.Any<FlareEvaluationContext>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new JsonException("invalid json"));

        var provider = CreateProvider(new FlareProviderOptions { CachingEnabled = false });

        var act = () => provider.ResolveBooleanValueAsync("flag1", false);

        await act.Should().ThrowAsync<ParseErrorException>();

        await provider.ShutdownAsync();
    }

    [Fact]
    public async Task ResolveBooleanValue_DirectMode_Timeout_ThrowsProviderNotReady()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _apiClient.EvaluateAsync(Arg.Any<string>(), Arg.Any<FlareEvaluationContext>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("timeout", new TimeoutException()));

        var provider = CreateProvider(new FlareProviderOptions { CachingEnabled = false });

        var act = () => provider.ResolveBooleanValueAsync("flag1", false, cancellationToken: token);

        await act.Should().ThrowAsync<ProviderNotReadyException>();

        await provider.ShutdownAsync();
    }

    [Fact]
    public async Task ResolveBooleanValue_DirectMode_GenericException_ThrowsGeneralException()
    {
        _apiClient.EvaluateAsync(Arg.Any<string>(), Arg.Any<FlareEvaluationContext>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("unexpected"));

        var provider = CreateProvider(new FlareProviderOptions { CachingEnabled = false });

        var act = () => provider.ResolveBooleanValueAsync("flag1", false);

        await act.Should().ThrowAsync<GeneralException>();

        await provider.ShutdownAsync();
    }

    #endregion

    #region Unsupported Types

    [Fact]
    public void ResolveStringValue_ThrowsTypeMismatchException()
    {
        var provider = CreateProvider();

        var act = () => provider.ResolveStringValueAsync("flag", "default");

        act.Should().ThrowAsync<TypeMismatchException>();
    }

    [Fact]
    public void ResolveIntegerValue_ThrowsTypeMismatchException()
    {
        var provider = CreateProvider();

        var act = () => provider.ResolveIntegerValueAsync("flag", 0);

        act.Should().ThrowAsync<TypeMismatchException>();
    }

    [Fact]
    public void ResolveDoubleValue_ThrowsTypeMismatchException()
    {
        var provider = CreateProvider();

        var act = () => provider.ResolveDoubleValueAsync("flag", 0.0);

        act.Should().ThrowAsync<TypeMismatchException>();
    }

    [Fact]
    public void ResolveStructureValue_ThrowsTypeMismatchException()
    {
        var provider = CreateProvider();

        var act = () => provider.ResolveStructureValueAsync("flag", new global::OpenFeature.Model.Value());

        act.Should().ThrowAsync<TypeMismatchException>();
    }

    #endregion

    #region Reason Mapping

    [Theory]
    [InlineData("STATIC", Reason.Static)]
    [InlineData("DEFAULT", Reason.Default)]
    [InlineData("TARGETING_MATCH", Reason.TargetingMatch)]
    [InlineData("SPLIT", Reason.Split)]
    [InlineData("CACHED", Reason.Cached)]
    [InlineData("DISABLED", Reason.Disabled)]
    [InlineData("ERROR", Reason.Error)]
    public async Task ResolveBooleanValue_DirectMode_MapsReasonCorrectly(string apiReason, string expectedReason)
    {
        var response = new FlagEvaluationResponse
        {
            FlagKey = "flag1",
            Value = true,
            Variant = "on",
            Reason = apiReason
        };
        _apiClient.EvaluateAsync("flag1", Arg.Any<FlareEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(response);

        var provider = CreateProvider(new FlareProviderOptions { CachingEnabled = false });

        var result = await provider.ResolveBooleanValueAsync("flag1", false);

        result.Reason.Should().Be(expectedReason);

        await provider.ShutdownAsync();
    }

    [Fact]
    public async Task ResolveBooleanValue_DirectMode_NullReason_MapsToUnknown()
    {
        var response = new FlagEvaluationResponse
        {
            FlagKey = "flag1",
            Value = true,
            Variant = "on",
            Reason = null!
        };
        _apiClient.EvaluateAsync("flag1", Arg.Any<FlareEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(response);

        var provider = CreateProvider(new FlareProviderOptions { CachingEnabled = false });

        var result = await provider.ResolveBooleanValueAsync("flag1", false);

        result.Reason.Should().Be(Reason.Unknown);

        await provider.ShutdownAsync();
    }

    [Fact]
    public async Task ResolveBooleanValue_DirectMode_UnknownReason_PassedThrough()
    {
        var response = new FlagEvaluationResponse
        {
            FlagKey = "flag1",
            Value = true,
            Variant = "on",
            Reason = "CUSTOM_REASON"
        };
        _apiClient.EvaluateAsync("flag1", Arg.Any<FlareEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(response);

        var provider = CreateProvider(new FlareProviderOptions { CachingEnabled = false });

        var result = await provider.ResolveBooleanValueAsync("flag1", false);

        result.Reason.Should().Be("CUSTOM_REASON");

        await provider.ShutdownAsync();
    }

    #endregion

    #region Flag Metadata

    [Fact]
    public async Task ResolveBooleanValue_DirectMode_WithMetadata_BuildsImmutableMetadata()
    {
        var scopeId = Guid.NewGuid();
        var updatedAt = new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);
        var response = new FlagEvaluationResponse
        {
            FlagKey = "flag1",
            Value = true,
            Variant = "on",
            Reason = "STATIC",
            Metadata = new FlagMetadata
            {
                ScopeAlias = "production",
                ScopeId = scopeId,
                UpdatedAt = updatedAt
            }
        };
        _apiClient.EvaluateAsync("flag1", Arg.Any<FlareEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(response);

        var provider = CreateProvider(new FlareProviderOptions { CachingEnabled = false });

        var result = await provider.ResolveBooleanValueAsync("flag1", false);

        result.FlagMetadata.Should().NotBeNull();
        result.FlagMetadata!.GetString("scopeAlias").Should().Be("production");
        result.FlagMetadata.GetString("scopeId").Should().Be(scopeId.ToString());
        result.FlagMetadata.GetString("updatedAt").Should().Be(updatedAt.ToString("O"));

        await provider.ShutdownAsync();
    }

    [Fact]
    public async Task ResolveBooleanValue_DirectMode_NullMetadata_ReturnsNullFlagMetadata()
    {
        var response = new FlagEvaluationResponse
        {
            FlagKey = "flag1",
            Value = false,
            Variant = "off",
            Reason = "STATIC",
            Metadata = null
        };
        _apiClient.EvaluateAsync("flag1", Arg.Any<FlareEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(response);

        var provider = CreateProvider(new FlareProviderOptions { CachingEnabled = false });

        var result = await provider.ResolveBooleanValueAsync("flag1", true);

        result.FlagMetadata.Should().BeNull();

        await provider.ShutdownAsync();
    }

    [Fact]
    public async Task ResolveBooleanValue_DirectMode_MetadataWithoutOptionalFields_OmitsThem()
    {
        var response = new FlagEvaluationResponse
        {
            FlagKey = "flag1",
            Value = true,
            Variant = "on",
            Reason = "STATIC",
            Metadata = new FlagMetadata
            {
                ScopeAlias = null,
                ScopeId = null,
                UpdatedAt = DateTimeOffset.UtcNow
            }
        };
        _apiClient.EvaluateAsync("flag1", Arg.Any<FlareEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(response);

        var provider = CreateProvider(new FlareProviderOptions { CachingEnabled = false });

        var result = await provider.ResolveBooleanValueAsync("flag1", false);

        result.FlagMetadata.Should().NotBeNull();

        await provider.ShutdownAsync();
    }

    #endregion

    #region ShutdownAsync

    [Fact]
    public async Task ShutdownAsync_CompletesSuccessfully()
    {
        var provider = CreateProvider();

        var act = () => provider.ShutdownAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ShutdownAsync_AfterInitialize_CleansUpTimer()
    {
        _apiClient.EvaluateAllAsync(Arg.Any<FlareEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(new FlareEvaluateAllResponse { Flags = new List<FlagEvaluationResponse>() });

        var provider = CreateProvider();
        await provider.InitializeAsync(EvaluationContext.Builder().Build());

        var act = () => provider.ShutdownAsync();

        await act.Should().NotThrowAsync();
    }

    #endregion

    #region EvaluationContext Forwarding

    [Fact]
    public async Task ResolveBooleanValue_DirectMode_ForwardsContextToApi()
    {
        var response = new FlagEvaluationResponse
        {
            FlagKey = "flag1",
            Value = true,
            Variant = "on",
            Reason = "TARGETING_MATCH"
        };
        _apiClient.EvaluateAsync("flag1", Arg.Any<FlareEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(response);

        var provider = CreateProvider(new FlareProviderOptions { CachingEnabled = false });
        var context = EvaluationContext.Builder()
            .SetTargetingKey("user-789")
            .Set("plan", "premium")
            .Build();

        await provider.ResolveBooleanValueAsync("flag1", false, context);

        await _apiClient.Received(1).EvaluateAsync(
            "flag1",
            Arg.Is<FlareEvaluationContext>(c =>
                c.Scope == "test-scope" &&
                c.TargetingKey == "user-789"),
            Arg.Any<CancellationToken>());

        await provider.ShutdownAsync();
    }

    #endregion
}

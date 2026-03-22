using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Flare.HttpClient;
using Flare.HttpClient.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenFeature;
using OpenFeature.Constant;
using OpenFeature.Error;
using OpenFeature.Model;

namespace Flare.OpenFeature.Provider;

public sealed class FlareProvider : FeatureProvider
{
    private static readonly Metadata ProviderMetadata = new("Flare Provider");

    private readonly IFlareApiClient _apiClient;
    private readonly FlareApiClientOptions _options;
    private readonly FlareProviderOptions _providerOptions;
    private readonly ILogger<FlareProvider> _logger;
    private readonly FlagCache _cache = new FlagCache();
    private readonly SemaphoreSlim _pollLock = new SemaphoreSlim(1, 1);

    private Timer? _pollTimer;
    private int _consecutiveFailures;
    private volatile bool _disposed;

    public FlareProvider(
        IFlareApiClient apiClient,
        IOptions<FlareApiClientOptions> options,
        IOptions<FlareProviderOptions> providerOptions,
        ILogger<FlareProvider>? logger = null)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _options = options.Value;
        _providerOptions = providerOptions.Value;
        _logger = logger ?? NullLogger<FlareProvider>.Instance;
    }

    public override Metadata? GetMetadata() => ProviderMetadata;

    public override async Task InitializeAsync(EvaluationContext context, CancellationToken cancellationToken = default)
    {
        if (!_providerOptions.CachingEnabled)
            return;

        try
        {
            var flareContext = EvaluationContextConverter.ToFlareContext(context, _options.Scope);
            var response = await _apiClient.EvaluateAllAsync(flareContext, cancellationToken).ConfigureAwait(false);
            _cache.Update(response.Flags, _options.Scope);
            _logger.LogInformation("Flare provider initialized with {FlagCount} flags cached", response.Flags.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to populate flag cache during initialization");
            StartPollTimer();
            throw;
        }

        StartPollTimer();
    }

    public override Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        _disposed = true;

        if (_pollTimer != null)
        {
            _pollTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _pollTimer.Dispose();
            _pollTimer = null;
        }

        _pollLock.Dispose();
        _logger.LogInformation("Flare provider shut down");

        return Task.CompletedTask;
    }

    public override async Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(
        string flagKey,
        bool defaultValue,
        EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        if (_providerOptions.CachingEnabled && !_cache.IsEmpty)
        {
            var cacheKey = FlagCache.BuildKey(_options.Scope, flagKey);
            if (_cache.TryGetFlag(cacheKey, out var cached))
            {
                return new ResolutionDetails<bool>(
                    flagKey: flagKey,
                    value: cached.Value,
                    variant: cached.Variant,
                    reason: Reason.Cached,
                    errorType: ErrorType.None,
                    errorMessage: null,
                    flagMetadata: BuildFlagMetadata(cached.Metadata)
                );
            }

            throw new FlagNotFoundException($"Flag '{flagKey}' not found in cache");
        }

        return await EvaluateDirectAsync(flagKey, defaultValue, context, cancellationToken).ConfigureAwait(false);
    }

    public override Task<ResolutionDetails<string>> ResolveStringValueAsync(
        string flagKey,
        string defaultValue,
        EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        throw new TypeMismatchException("Flare provider only supports boolean flag evaluation");
    }

    public override Task<ResolutionDetails<int>> ResolveIntegerValueAsync(
        string flagKey,
        int defaultValue,
        EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        throw new TypeMismatchException("Flare provider only supports boolean flag evaluation");
    }

    public override Task<ResolutionDetails<double>> ResolveDoubleValueAsync(
        string flagKey,
        double defaultValue,
        EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        throw new TypeMismatchException("Flare provider only supports boolean flag evaluation");
    }

    public override Task<ResolutionDetails<Value>> ResolveStructureValueAsync(
        string flagKey,
        Value defaultValue,
        EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        throw new TypeMismatchException("Flare provider only supports boolean flag evaluation");
    }

    private void StartPollTimer()
    {
        _pollTimer = new Timer(
            PollCallback,
            null,
            _providerOptions.PollingInterval,
            _providerOptions.PollingInterval);
    }

    private async void PollCallback(object? state)
    {
        if (_disposed)
            return;

        if (!_pollLock.Wait(0))
            return;

        try
        {
            await PollAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in poll callback");
        }
        finally
        {
            try { _pollLock.Release(); }
            catch (ObjectDisposedException) { }
        }
    }

    private async Task PollAsync()
    {
        try
        {
            var flareContext = new FlareEvaluationContext { Scope = _options.Scope };
            var response = await _apiClient.EvaluateAllAsync(flareContext, CancellationToken.None).ConfigureAwait(false);
            var changedKeys = _cache.Update(response.Flags, _options.Scope);

            Interlocked.Exchange(ref _consecutiveFailures, 0);

            if (changedKeys.Count > 0)
            {
                _logger.LogInformation("Poll detected {Count} flag change(s): {Keys}",
                    changedKeys.Count, string.Join(", ", changedKeys));

                EventChannel.Writer.TryWrite(new ProviderEventPayload
                {
                    Type = ProviderEventTypes.ProviderConfigurationChanged,
                    ProviderName = GetMetadata()?.Name,
                    Message = $"{changedKeys.Count} flag(s) changed",
                    FlagsChanged = changedKeys.ToList()
                });
            }
        }
        catch (Exception ex)
        {
            var failures = Interlocked.Increment(ref _consecutiveFailures);
            _logger.LogError(ex, "Poll failed ({ConsecutiveFailures} consecutive)", failures);

            if (failures >= _providerOptions.StaleThreshold)
            {
                EventChannel.Writer.TryWrite(new ProviderEventPayload
                {
                    Type = ProviderEventTypes.ProviderStale,
                    ProviderName = GetMetadata()?.Name,
                    Message = $"Poll failed {failures} consecutive times: {ex.Message}"
                });
            }
            else
            {
                EventChannel.Writer.TryWrite(new ProviderEventPayload
                {
                    Type = ProviderEventTypes.ProviderError,
                    ProviderName = GetMetadata()?.Name,
                    Message = $"Poll failed: {ex.Message}"
                });
            }
        }
    }

    private async Task<ResolutionDetails<bool>> EvaluateDirectAsync(
        string flagKey,
        bool defaultValue,
        EvaluationContext? context,
        CancellationToken cancellationToken)
    {
        try
        {
            var flareContext = EvaluationContextConverter.ToFlareContext(context, _options.Scope);
            var response = await _apiClient.EvaluateAsync(flagKey, flareContext, cancellationToken).ConfigureAwait(false);

            return new ResolutionDetails<bool>(
                flagKey: flagKey,
                value: response.Value,
                variant: response.Variant,
                reason: MapReason(response.Reason),
                errorType: ErrorType.None,
                errorMessage: null,
                flagMetadata: BuildFlagMetadata(response.Metadata)
            );
        }
        catch (FlareApiException ex)
        {
            _logger.LogError(ex, "API error evaluating flag {FlagKey}: {StatusCode}", flagKey, ex.StatusCode);
            throw new GeneralException($"Flare API error: {ex.Message}", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error evaluating flag {FlagKey}", flagKey);
            throw new ProviderNotReadyException($"HTTP error: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error for flag {FlagKey}", flagKey);
            throw new ParseErrorException($"Failed to parse response for flag '{flagKey}'", ex);
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            _logger.LogWarning("Flag evaluation cancelled for {FlagKey}", flagKey);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout evaluating flag {FlagKey}", flagKey);
            throw new ProviderNotReadyException("Request timeout", ex);
        }
        catch (FeatureProviderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error evaluating flag {FlagKey}", flagKey);
            throw new GeneralException($"Unexpected error evaluating flag '{flagKey}'", ex);
        }
    }

    private static string MapReason(string? reason)
    {
        if (string.IsNullOrEmpty(reason))
            return Reason.Unknown;

        return reason!.ToUpperInvariant() switch
        {
            "STATIC" => Reason.Static,
            "DEFAULT" => Reason.Default,
            "TARGETING_MATCH" => Reason.TargetingMatch,
            "SPLIT" => Reason.Split,
            "CACHED" => Reason.Cached,
            "DISABLED" => Reason.Disabled,
            "ERROR" => Reason.Error,
            _ => reason
        };
    }

    private static ImmutableMetadata? BuildFlagMetadata(FlagMetadata? metadata)
    {
        if (metadata == null)
            return null;

        var dict = new Dictionary<string, object>();

        if (metadata.ScopeAlias != null)
            dict["scopeAlias"] = metadata.ScopeAlias;

        if (metadata.ScopeId.HasValue)
            dict["scopeId"] = metadata.ScopeId.Value.ToString();

        dict["updatedAt"] = metadata.UpdatedAt.ToString("O");

        return new ImmutableMetadata(dict);
    }

}

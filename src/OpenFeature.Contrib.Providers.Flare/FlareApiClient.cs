using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenFeature.Contrib.Providers.Flare.Models;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.Flare;

public class FlareApiClient : IFlareApiClient
{
    private const string EvaluateEndpoint = "/sdk/v1/flags/evaluate";
    private const string EvaluateAllEndpoint = "/sdk/v1/flags/evaluate-all";

    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public FlareApiClient(
        HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }
    

    public async Task<IReadOnlyList<FlagEvaluationResponse>> EvaluateAllAsync(string scope,
        CancellationToken cancellationToken = default)
    {
        var request = new FlareEvaluateAllRequest
        {
            Context = new FlareEvaluationContext()
            {
                Scope = scope
            }
        };

        var json = JsonSerializer.Serialize(request, JsonOptions);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, EvaluateAllEndpoint);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);

        await EnsureSuccessAsync(httpResponse, cancellationToken).ConfigureAwait(false);

        var responseContent = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
        var response = JsonSerializer.Deserialize<FlareEvaluateAllResponse>(responseContent, JsonOptions);

        if (response?.Flags == null)
        {
            throw new JsonException("Failed to deserialize evaluate-all response");
        }

        return response.Flags;
    }

    public async Task<FlagEvaluationResponse> EvaluateAsync(
        string flagKey,
        EvaluationContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(flagKey))
        {
            throw new ArgumentException("Flag key cannot be null or empty.", nameof(flagKey));
        }

        var request = new FlareEvaluationRequest
        {
            FlagKey = flagKey,
            Context = BuildEvaluationContext(context)
        };

        var json = JsonSerializer.Serialize(request, JsonOptions);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, EvaluateEndpoint);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);

        await EnsureSuccessAsync(httpResponse, cancellationToken).ConfigureAwait(false);

        var responseContent = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
        var response = JsonSerializer.Deserialize<FlagEvaluationResponse>(responseContent, JsonOptions);

        if (response == null)
        {
            throw new JsonException("Failed to deserialize evaluate response");
        }

        return response;
    }

    private FlareEvaluationContext BuildEvaluationContext(EvaluationContext context)
    {
        var flareContext = new FlareEvaluationContext
        {
            Scope = "dev"
        };

        flareContext.TargetingKey = context.TargetingKey;

        var scopeValue = context.GetValue("scope");
        if (!string.IsNullOrEmpty(scopeValue.AsString))
        {
            flareContext.Scope = scopeValue.AsString;
        }
        else
        {
            throw new ArgumentException("Scope cannot be null or empty.", nameof(context));
        }

        return flareContext;
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var statusCode = response.StatusCode;
        string? responseBody = null;

        try
        {
            responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
        catch
        {
            // Ignore errors reading response body
        }

        var message = statusCode switch
        {
            HttpStatusCode.BadRequest => $"Bad request: {responseBody ?? "Invalid request format"}",
            HttpStatusCode.Unauthorized => "Unauthorized: Invalid or missing API key",
            HttpStatusCode.NotFound => $"Not found: {responseBody ?? "Resource not found"}",
            _ => $"API error ({(int)statusCode}): {responseBody ?? response.ReasonPhrase}"
        };

        throw new FlareApiException(statusCode, message);
    }
}

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenFeature.Contrib.Providers.Flare.Models;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.Flare;

public interface IFlareApiClient
{
    /// <summary>
    /// Evaluates all flags for the given context.
    /// Calls POST /sdk/v1/flags/evaluate-all
    /// </summary>
    /// <param name="context">The evaluation context containing scope and targeting key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all flag evaluation responses.</returns>
    /// <exception cref="System.Net.Http.HttpRequestException">Thrown for network errors.</exception>
    /// <exception cref="FlareApiException">Thrown for API errors (400, 401, 404).</exception>
    Task<IReadOnlyList<FlagEvaluationResponse>> EvaluateAllAsync(
        EvaluationContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates a single flag for the given context.
    /// Calls POST /sdk/v1/flags/evaluate
    /// </summary>
    /// <param name="flagKey">The key of the flag to evaluate.</param>
    /// <param name="context">The evaluation context containing scope and targeting key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The flag evaluation response.</returns>
    /// <exception cref="System.Net.Http.HttpRequestException">Thrown for network errors.</exception>
    /// <exception cref="FlareApiException">Thrown for API errors (400, 401, 404).</exception>
    Task<FlagEvaluationResponse> EvaluateAsync(
        string flagKey,
        EvaluationContext context,
        CancellationToken cancellationToken = default);
}

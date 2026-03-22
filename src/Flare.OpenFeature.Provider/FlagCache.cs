using System.Collections.Generic;
using System.Linq;
using Flare.HttpClient.Models;

namespace Flare.OpenFeature.Provider;

internal sealed class FlagCache
{
    private volatile IReadOnlyDictionary<string, FlagEvaluationResponse> _flags =
        new Dictionary<string, FlagEvaluationResponse>();

    public bool IsEmpty => _flags.Count == 0;

    public bool TryGetFlag(string cacheKey, out FlagEvaluationResponse response)
    {
        var snapshot = _flags;
        if (snapshot.TryGetValue(cacheKey, out var found))
        {
            response = found;
            return true;
        }

        response = null!;
        return false;
    }

    public IReadOnlyList<string> Update(IReadOnlyList<FlagEvaluationResponse> flags, string scope)
    {
        var newDict = new Dictionary<string, FlagEvaluationResponse>(flags.Count);
        foreach (var flag in flags)
        {
            var key = BuildKey(scope, flag.FlagKey);
            newDict[key] = flag;
        }

        var oldDict = _flags;
        var changedKeys = new List<string>();

        foreach (var kvp in newDict)
        {
            if (!oldDict.TryGetValue(kvp.Key, out var oldFlag) ||
                oldFlag.Value != kvp.Value.Value ||
                oldFlag.Variant != kvp.Value.Variant)
            {
                changedKeys.Add(kvp.Value.FlagKey);
            }
        }

        foreach (var kvp in oldDict)
        {
            if (!newDict.ContainsKey(kvp.Key))
            {
                changedKeys.Add(kvp.Value.FlagKey);
            }
        }

        _flags = newDict;

        return changedKeys;
    }

    public static string BuildKey(string scope, string flagKey) => $"{scope}:{flagKey}";
}

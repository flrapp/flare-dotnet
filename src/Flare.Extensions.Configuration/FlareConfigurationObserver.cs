using System;
using System.Collections.Generic;
using System.Linq;

namespace Flare.Extensions.Configuration;

public class FlareConfigurationObserver
{
    public static FlareConfigurationObserver Instance { get; } = new();
    private readonly List<Action<Dictionary<string, string?>>> _listeners = [];
    private Dictionary<string, string?> _data = new();
    
    public void AddListener(Action<Dictionary<string, string?>>listener)
    {
        _listeners.Add(listener);
        listener(_data);
    }

    public void NotifyListeners()
    {
        foreach (var listener in _listeners)
        {
            listener(_data);
        }
    }

    public void SetValue(string key, string value)
    {
        _data[key] = value;
        NotifyListeners();
    }

    public void SetAll(IReadOnlyDictionary<string, string?> newData)
    {
        _data = newData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        NotifyListeners();
    }
}
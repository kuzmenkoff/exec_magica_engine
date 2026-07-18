using System.Collections.Generic;
using UnityEngine;

/// <summary>Loads and caches distilled <see cref="NeuralNet"/> models from Resources (.bytes blobs).</summary>
public static class NeuralNetLoader
{
    private static readonly Dictionary<string, NeuralNet> cache = new Dictionary<string, NeuralNet>();

    /// <summary>Returns the net at Resources/<paramref name="resourcePath"/> (cached; null if missing).</summary>
    public static NeuralNet Load(string resourcePath)
    {
        if (string.IsNullOrEmpty(resourcePath)) return null;
        if (cache.TryGetValue(resourcePath, out NeuralNet net)) return net;

        TextAsset ta = Resources.Load<TextAsset>(resourcePath);
        if (ta == null) { Debug.LogError($"NeuralNetLoader: no model at Resources/{resourcePath}"); return null; }

        net = new NeuralNet(ta.bytes);
        cache[resourcePath] = net;
        return net;
    }
}

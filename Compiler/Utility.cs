using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Ronin;

internal static class Utility
{
    [ExcludeFromCodeCoverage]
    public static KeyValuePair<TKey, TValue> Entry<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key)
    {
        foreach (var entry in dictionary)
        {
            if (entry.Key.Equals(key)) return entry;
        }
        return default;
    }
}
using System.Reflection;

internal static class Utility
{
    public static ReadOnlyMemory<T> AsMemory<T>(this List<T> list)
    {
        return new(GetItems(list), 0, GetSize(list));

        static T[] GetItems(List<T> tokens) => typeof(List<T>).GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(tokens) as T[];
        static int GetSize(List<T> tokens) => (int)typeof(List<T>).GetField("_size", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(tokens);
    }

    public static KeyValuePair<TKey, TValue> Entry<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key)
    {
        foreach (var entry in dictionary)
        {
            if (entry.Key.Equals(key)) return entry;
        }
        return default;
    }
}
/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

namespace HelixToolkit.SharpDX.Core;

public static class CollectionExtensions {
    /// <summary>
    ///     Gets the internal array of a <see cref="List{T}" />.
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <param name="list">The respective list.</param>
    /// <returns>The internal array of the list.</returns>
    public static T[] GetInternalArray<T>(this List<T> list) {
        return [.. list];
    }

    public static T[] GetArrayByType<T>(this IList<T> list) {
        T[] array;
        if (list is T[] t)
            array = t;
        else if (list is FastList<T> f)
            array = f.Items;
        else
            array = [.. list];
        return array;
    }

    /// <summary>
    ///     Tries to get a value from a <see cref="IDictionary{K,V}" />.
    /// </summary>
    /// <typeparam name="K">The type of the key.</typeparam>
    /// <typeparam name="V">The type of the value.</typeparam>
    /// <param name="dict">The respective dictionary.</param>
    /// <param name="key">The respective key.</param>
    /// <returns>The value if exists, else <c>null</c>.</returns>
    public static V Get<K, V>(this IDictionary<K, V> dict, K key) {
        V val;
        if (dict.TryGetValue(key, out val)) return val;

        return default;
    }
}

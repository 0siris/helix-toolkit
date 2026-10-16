namespace HelixToolkit.UWP;

/// <summary>
///     Carries two ordered keys and their associated value independently of a dictionary entry.
/// </summary>
/// <typeparam name="K">The first key type.</typeparam>
/// <typeparam name="T">The second key type.</typeparam>
/// <typeparam name="V">The value type.</typeparam>
public sealed class DoubleKeyPairValue<K, T, V> {
    /// <summary>
    ///     Initializes a new instance of the <see cref="DoubleKeyPairValue{K,T,V}"/> class.
    /// </summary>
    /// <param name="key1">The first key.</param>
    /// <param name="key2">The second key.</param>
    /// <param name="value">The associated value.</param>
    public DoubleKeyPairValue(K key1, T key2, V value) {
        Key1 = key1;
        Key2 = key2;
        Value = value;
    }

    /// Gets or sets the first key carried by this instance.
    public K Key1 { get; set; }

    /// Gets or sets the second key carried by this instance.
    public T Key2 { get; set; }

    /// Gets or sets the value carried by this instance.
    public V Value { get; set; }

    public override string ToString() => Key1 + " - " + Key2 + " - " + Value;
}

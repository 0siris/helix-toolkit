using System.Runtime.CompilerServices;

namespace HelixToolkit.SharpDX.Core.Utilities;
/// <summary>
///     A simple curcular ring buffer implementation
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class SimpleRingBuffer<T> {
    private readonly T[] buffer;
    private readonly int bufferSize;
    private int first;
    private int last = -1;
    private int next;

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="size"></param>
    public SimpleRingBuffer(int size) {
        buffer = new T[size];
        bufferSize = size;
    }

    /// <summary>
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    ///     Get last added element
    /// </summary>
    public T? Last => IsEmpty() ? default : buffer[last];

    /// <summary>
    ///     Get first added element
    /// </summary>
    public T? First => IsEmpty() ? default : buffer[first];

    /// <summary>
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    public T this[int i] => buffer[(first + i) % bufferSize];

    /// <summary>
    /// </summary>
    /// <param name="item"></param>
    /// <returns>If buffer full, return false</returns>
    public void Add(T item) {
        if (IsFull()) RemoveFirst();
        buffer[next] = item;
        last = next;
        next = IncLast();
        ++Count;
    }

    /// <summary>
    ///     Remove the last element added into the buffer
    /// </summary>
    /// <returns></returns>
    public bool RemoveLast() {
        if (IsEmpty()) return false;

        next = DecLast();
        buffer[next] = default;
        last = next == 0 ? bufferSize - 1 : next - 1;
        --Count;
        return true;
    }

    /// <summary>
    ///     Remove the first element added into the buffer
    /// </summary>
    /// <returns></returns>
    public bool RemoveFirst() {
        if (IsEmpty()) return false;

        buffer[first] = default;
        first = IncFirst();
        --Count;
        return true;
    }

    /// <summary>
    ///     If buffer is full
    /// </summary>
    /// <returns></returns>
    public bool IsFull() => Count == bufferSize;

    /// <summary>
    ///     If buffer is empty
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsEmpty() => Count == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int IncLast() => (next + 1) % bufferSize;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int DecLast() {
        var prev = next - 1;
        return prev >= 0 ? prev : bufferSize - 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int IncFirst() => (first + 1) % bufferSize;

    /// <summary>
    ///     Reset
    /// </summary>
    public void Clear() {
        Array.Clear(buffer, 0, bufferSize);
        first = next = 0;
        last = -1;
        Count = 0;
    }
}

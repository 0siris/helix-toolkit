using System.Runtime.CompilerServices;

namespace HelixToolkit.SharpDX.Core.Model;
/// <summary>
///     Render order key
/// </summary>
public struct OrderKey : IComparable<OrderKey> {
    public uint Key { get; }

    public OrderKey(uint key) {
        Key = key;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OrderKey Create(ushort order, ushort materialId) =>
        //return new OrderKey(((uint)order << 16) | materialID);
        new(order);

    public int CompareTo(OrderKey other) => Key.CompareTo(other.Key);
}

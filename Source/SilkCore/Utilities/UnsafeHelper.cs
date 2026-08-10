using System.Runtime.CompilerServices;

namespace HelixToolkit.SharpDX.Core.Utilities;
public static class UnsafeHelper {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int SizeOf<T>() where T : unmanaged {
        unsafe {
            return sizeof(T);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int SizeOf<T>(T[] array) where T : unmanaged => SizeOf<T>() * array.Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int SizeOf<T>(ref T _) where T : unmanaged => SizeOf<T>();

    /// <summary>
    ///     Unsafe memory copy
    /// </summary>
    /// <param name="dst"></param>
    /// <param name="src"></param>
    /// <param name="sizeInBytes"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MemoryCopy(nint dst, nint src, int sizeInBytes) {
        if (dst == nint.Zero || src == nint.Zero) return;
        unsafe {
            System.Buffer.MemoryCopy((void*)src, (void*)dst, sizeInBytes, sizeInBytes);
        }
    }

    /// <summary>
    ///     Clears the memory.
    /// </summary>
    /// <param name="dest">The dest.</param>
    /// <param name="sizeInBytesToClear">The size in bytes to clear.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ClearMemory(nint dest, int sizeInBytesToClear) {
        if (dest == nint.Zero) return;
        unsafe {
            var pDest = (byte*)dest.ToPointer();
            for (var i = 0; i < sizeInBytesToClear; ++i) *pDest++ = 0;
        }
    }

    /// <summary>
    ///     Reads the specified T data from a memory location.
    /// </summary>
    /// <typeparam name="T">Type of a data to read.</typeparam>
    /// <param name="source">Memory location to read from.</param>
    /// <param name="data">The data write to.</param>
    /// <returns>source pointer + sizeof(T).</returns>
    public static nint ReadAndPosition<T>(nint source, ref T data) where T : unmanaged {
        if (source == nint.Zero) return nint.Zero;
        unsafe {
            data = *(T*)source;
            return source + SizeOf<T>();
        }
    }

    /// <summary>
    ///     Reads the specified T data from a memory location.
    /// </summary>
    /// <typeparam name="T">Type of a data to read.</typeparam>
    /// <param name="source">Memory location to read from.</param>
    /// <returns>The data read from the memory location.</returns>
    public static T Read<T>(nint source) where T : unmanaged {
        if (source == nint.Zero) return default;
        unsafe {
            return *(T*)source;
        }
    }

    /// <summary>
    ///     Reads the specified T data from a memory location.
    /// </summary>
    /// <typeparam name="T">Type of a data to read.</typeparam>
    /// <param name="source">Memory location to read from.</param>
    /// <param name="data">The data write to.</param>
    /// <returns>source pointer + sizeof(T).</returns>
    public static void Read<T>(nint source, ref T data) where T : unmanaged {
        if (source == nint.Zero) return;
        unsafe {
            data = *(T*)source;
        }
    }

    /// <summary>
    ///     Reads the specified T data from a memory location.
    /// </summary>
    /// <typeparam name="T">Type of a data to read.</typeparam>
    /// <param name="source">Memory location to read from.</param>
    /// <param name="data">The data write to.</param>
    /// <returns>source pointer + sizeof(T).</returns>
    public static void ReadOut<T>(nint source, out T data) where T : unmanaged {
        data = default;
        if (source == nint.Zero) return;
        unsafe {
            data = *(T*)source;
        }
    }

    /// <summary>
    ///     Reads the specified array T[] data from a memory location.
    /// </summary>
    /// <typeparam name="T">Type of a data to read.</typeparam>
    /// <param name="source">Memory location to read from.</param>
    /// <param name="data">The data write to.</param>
    /// <param name="offset">The offset in the array to write to.</param>
    /// <param name="count">The number of T element to read from the memory location.</param>
    /// <returns>source pointer + sizeof(T) * count.</returns>
    public static nint Read<T>(nint source, T[] data, int offset, int count) where T : unmanaged {
        if (source == nint.Zero) return nint.Zero;
        unsafe {
            var bytesToCopy = SizeOf<T>() * count;
            fixed (T* pData = &data[offset]) {
                MemoryCopy(new nint(pData), source, bytesToCopy);
            }

            return source + bytesToCopy;
        }
    }

    /// <summary>
    ///     Writes the specified T data to a memory location.
    /// </summary>
    /// <typeparam name="T">Type of a data to write.</typeparam>
    /// <param name="destination">Memory location to write to.</param>
    /// <param name="data">The data to write.</param>
    /// <returns>destination pointer + sizeof(T).</returns>
    public static nint Write<T>(nint destination, T data) where T : unmanaged 
        => Write(destination, ref data);

    /// <summary>
    ///     Writes the specified T data to a memory location.
    /// </summary>
    /// <typeparam name="T">Type of a data to write.</typeparam>
    /// <param name="destination">Memory location to write to.</param>
    /// <param name="data">The data to write.</param>
    /// <returns>destination pointer + sizeof(T).</returns>
    public static nint Write<T>(nint destination, ref T data) where T : unmanaged {
        if (destination == nint.Zero) return nint.Zero;
        unsafe {
            *(T*)destination = data;
            return destination + SizeOf<T>();
        }
    }

    /// <summary>
    ///     Writes the specified array T[] data to a memory location.
    /// </summary>
    /// <typeparam name="T">Type of a data to write.</typeparam>
    /// <param name="destination">Memory location to write to.</param>
    /// <param name="data">The array of T data to write.</param>
    /// <param name="offset">The offset in the array to read from.</param>
    /// <param name="count">The number of T element to write to the memory location.</param>
    /// <returns>destination pointer + sizeof(T) * count.</returns>
    public static nint Write<T>(nint destination, T[] data, int offset, int count) where T : unmanaged {
        if (destination == nint.Zero) return nint.Zero;
        unsafe {
            var bytesToWrite = count * SizeOf<T>();
            fixed (T* pData = &data[offset]) {
                MemoryCopy(destination, new nint((byte*)pData), bytesToWrite);
            }

            return destination + bytesToWrite;
        }
    }


    public static nint Write(nint destination, nint data, int offset, int count) {
        if (destination == nint.Zero) 
            return nint.Zero;

        MemoryCopy(destination, data + offset, count);
        return destination + count;
    }
}

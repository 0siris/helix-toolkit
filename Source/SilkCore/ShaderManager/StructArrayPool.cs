using System.Diagnostics;
using HelixToolkit.Logger;
using HelixToolkit.SharpDX.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace HelixToolkit.SharpDX.Core;

/// <summary>
///     A array buffer defined by its struct size.
///     <para>
///         To use it, caller must first call <see cref="GetId" /> to obtain an unique id in order to modify the buffer.
///         Caller must restrictly use this unique id to access the buffer.
///     </para>
///     Caller must call <see cref="ReleaseId(int)" /> to release the id once
///     caller is no longer needed to use this buffer. The released id will be reused by an new caller.
/// </summary>
public sealed unsafe class ArrayStorage : DisposeObject {
    private static LoggerLib.ILog Logger => LoggerLib.Logger.Current;
    public static int MinArraySize = 1024 * 4;
    public static int MaxArraySizeExpoentialIncrement = 1024 * 1024;
    private readonly FastList<byte> binaryArray = new(MinArraySize);
    private readonly IdHelper idHelper = new();
    private readonly ReaderWriterLockSlim rwLock = new();

    public ArrayStorage(int structSize) {
        StructSize = structSize;
    }

    public int StructSize { get; }

    public int GetId() {
        var id = idHelper.GetNextId();
        if (binaryArray.Count <= id * StructSize) {
            var newSize = id * 2 * StructSize;
            if (newSize > MaxArraySizeExpoentialIncrement) newSize = (id + 1) * StructSize;
            rwLock.EnterWriteLock();
            binaryArray.Resize(newSize, false);
            rwLock.ExitWriteLock();
            if (Logger.IsEnabled(LogLevel.Debug))
                Logger.Debug("Resize struct array to {Value0} * {Value1} = {Value2}", StructSize, id + 1, binaryArray.Count);
        }

        if (Logger.IsEnabled(LogLevel.Debug))
            Logger.Debug("Getting new id [{Value0}] on struct size [{Value1}].", id, StructSize);
        return id;
    }

    public void ReleaseId(int id) {
        if (Logger.IsEnabled(LogLevel.Debug)) Logger.Debug("Release id [{Value0}] on struct size [{Value1}].", id, StructSize);
        idHelper.ReleaseId(id);
        Clear(id);
    }

    public void Clear(int id) {
        if (id < 0) {
            Logger.Error("Invalid Id {Value0}", id);
            return;
        }

        var offsetInArray = GetOffSet(id);
        if (offsetInArray + StructSize > binaryArray.Count) {
            Debug.Assert(false);
            return;
        }

        rwLock.EnterReadLock();
        var array = binaryArray.GetInternalArray();
        Array.Clear(array, offsetInArray, StructSize);
        rwLock.ExitReadLock();
    }

    public bool Write(int id, int offset, nint data, int dataLength) {
        if (id < 0) {
            Logger.Error("Invalid Id {Value0}", id);
            return false;
        }

        var offsetInArray = GetOffSet(id) + offset;
        if (offsetInArray + dataLength > binaryArray.Count || offset + dataLength > StructSize) {
            Debug.Assert(false);
            return false;
        }

        rwLock.EnterReadLock();
        var array = binaryArray.GetInternalArray();
        fixed (byte* pArray = &array[offsetInArray]) {
            UnsafeHelper.Write(new nint(pArray), data, 0, dataLength);
        }

        rwLock.ExitReadLock();
        return true;
    }

    public bool Write<T>(int id, int offset, ref T value) where T : unmanaged {
        var size = UnsafeHelper.SizeOf<T>();
        fixed (T* pValue = &value) {
            return Write(id, offset, new nint(pValue), size);
        }
    }

    public bool Read(int id, nint dest) => Read(id, 0, dest, StructSize);

    public bool Read(int id, int offset, nint dest, int size) {
        if (id < 0) return false;
        var offsetInArray = GetOffSet(id) + offset;
        if (offsetInArray + size > binaryArray.Count) {
            Debug.Assert(false);
            return false;
        }

        var array = binaryArray.GetInternalArray();
        fixed (byte* pArray = &array[offsetInArray]) {
            UnsafeHelper.MemoryCopy(dest, new nint(pArray), size);
        }

        return true;
    }

    public bool Read<T>(int id, int offset, out T value) where T : unmanaged {
        if (id < 0) {
            value = default;
            return false;
        }

        var size = UnsafeHelper.SizeOf<T>();
        var offsetInArray = GetOffSet(id) + offset;
        if (offsetInArray + size > binaryArray.Count) {
            Debug.Assert(false);
            value = default;
            return false;
        }

        var array = binaryArray.GetInternalArray();
        fixed (byte* pArray = &array[offsetInArray]) {
            value = *(T*)pArray;
        }

        return true;
    }

    public int GetOffSet(int id) => id * StructSize;

    public byte[] GetArray() => binaryArray.GetInternalArray();

    protected override void OnDispose(bool disposeManagedResources) {
        rwLock.EnterWriteLock();
        binaryArray.Clear();
        rwLock.ExitWriteLock();
        base.OnDispose(disposeManagedResources);
    }
}

/// <summary>
///     Interface for struct array
/// </summary>
public interface IStructArrayPool : IDisposable {
    ArrayStorage Register(int structSize);
}

/// <summary>
///     A pool contains various of binary buffers defined by struct size.
/// </summary>
public sealed class StructArrayPool : DisposeObject, IStructArrayPool {
    private ArrayPoolStorage storage;

    public StructArrayPool() {
        storage = new ArrayPoolStorage();
    }

    public ArrayStorage Register(int structSize) => storage.TryCreateOrGet(structSize, structSize, out var result)
        ? result
        : throw new InvalidOperationException($"Unable to register storage for struct size {structSize}.");

    protected override void OnDispose(bool disposeManagedResources) {
        if (disposeManagedResources) RemoveAndDispose(ref storage);
        base.OnDispose(disposeManagedResources);
    }

    private sealed class ArrayPoolStorage : ReferenceCountedDictionaryPool<int, ArrayStorage, int> {
        private static LoggerLib.ILog Logger => LoggerLib.Logger.Current;

        public ArrayPoolStorage() : base(true) { }

        protected override bool CanCreate(ref int key, ref int argument) => argument > 0;

        protected override ArrayStorage OnCreate(ref int key, ref int argument) {
            Logger.Info("Creating new struct array with size {Value0}", argument);
            return new ArrayStorage(argument);
        }
    }
}

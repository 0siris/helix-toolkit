/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/
//#define DEBUGRESOURCE

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace HelixToolkit.SharpDX.Core.Utilities;

/// <summary>
///     Base implementation for reference counted dictionary.
///     <para></para>
///     Object with same key will be returned by the pool or create an new object if key is not dictionary.
///     And object reference count will be incremented by 1.
///     <para></para>
///     Set autoDispose = true in constructor if you want to automatically dispose the object once not being used from
///     outside.
/// </summary>
/// <typeparam name="TKey">The stable key identifying a cached resource.</typeparam>
/// <typeparam name="TValue">The reference-counted resource type.</typeparam>
/// <typeparam name="TArgument">The resource creation argument type.</typeparam>
public abstract class ReferenceCountedDictionaryPool<TKey, TValue, TArgument> : DisposeObject
    where TKey : notnull
    where TValue : DisposeObject {
    private readonly bool autoDispose;
    private readonly Dictionary<TKey, TValue> pool = [];

    /// <summary>
    /// </summary>
    /// <param name="autoDispose">Dispose object if no more exteranl references.</param>
    protected ReferenceCountedDictionaryPool(bool autoDispose) {
        this.autoDispose = autoDispose;
    }

    public int DictionaryCount => pool.Count;

    public int Count => pool.Count;

    protected IEnumerable<TValue> Items => pool.Values;

    /// <summary>
    ///     Try to create or get object from the pool. Reference is incremented before returning.
    /// </summary>
    /// <param name="key">The requested key, optionally normalized by the creation callbacks.</param>
    /// <param name="argument">The argument passed to the resource creation callbacks.</param>
    /// <param name="objOut">The acquired live resource, or <see langword="null"/> when acquisition fails.</param>
    /// <returns>Whether a live resource reference was acquired.</returns>
    /// <remarks>
    ///     Disposal removes only the same cached instance under the pool lock. A detached resource cannot evict a
    ///     newer resource stored under its former key.
    /// </remarks>
    public bool TryCreateOrGet(TKey key, TArgument argument, [NotNullWhen(true)] out TValue? objOut) {
        if (IsDisposed) {
            objOut = null;
            return false;
        }

        if (!CanCreate(ref key, ref argument)) {
            objOut = null;
            return false;
        }

        do {
            lock (pool) {
                if (!pool.TryGetValue(key, out var existing)) {
                    var created = OnCreate(ref key, ref argument);
                    if (created is null) {
                        objOut = null;
                        return false;
                    }

                    pool.Add(key, created);
                    objOut = created;
                    created.AddBackToPool = Item_AddBackToPool;
                    var capturedKey = key;
                    created.Disposed += (_, _) => {
                        lock (pool)
                            if (pool.TryGetValue(capturedKey, out var pooledValue)
                                && ReferenceEquals(pooledValue, created))
                                pool.Remove(capturedKey);
                    };
                } else {
                    objOut = existing;
                }

                if (objOut is not { } current)
                    return false;

                if (current.IncRef() <= 1 || current.IsDisposed) {
                    Task.Delay(1)
                        .Wait();
                    continue;
                }
            }

            break;
        } while (true);

        return true;
    }

    /// <summary>
    ///     Try to get object by key. Reference will be incremented before returning.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="objOut"></param>
    /// <returns></returns>
    public bool TryGet(TKey key, [NotNullWhen(true)] out TValue? objOut) {
        objOut = null;
        if (IsDisposed) {
#if DEBUG
            throw new InvalidOperationException("Pool has been disposed.");
#else
        return false;
#endif
        }

        lock (pool) {
            if (!pool.TryGetValue(key, out var existing))
                return false;

            if (existing.IncRef() <= 1 || existing.IsDisposed)
                return false;

            objOut = existing;
            return true;
        }
    }

    /// <summary>
    ///     Try detach from the pool. The object will be removed from the pool and reference is not incremented before
    ///     returning.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="objOut"></param>
    /// <returns></returns>
    public bool TryDetach(TKey key, [NotNullWhen(true)] out TValue? objOut) {
        objOut = null;
        if (IsDisposed) {
#if DEBUG
            throw new InvalidOperationException("Pool has been disposed.");
#else
        return false;
#endif
        }

        lock (pool) {
            if (!pool.Remove(key, out var detached))
                return false;

            detached.AddBackToPool = null;
            objOut = detached;
        }

        return !objOut.IsDisposed;
    }

    private void Item_AddBackToPool(DisposeObject e) {
        if (autoDispose)
            lock (pool) {
                if (e.RefCount > 1 || e.IsDisposed) return;
                Debug.Assert(e.RefCount == 1);
                e.AddBackToPool = null;
                e.Dispose();
            }
    }

    protected abstract bool CanCreate(ref TKey key, ref TArgument argument);

    protected abstract TValue? OnCreate(ref TKey key, ref TArgument argument);

    protected void Clear() {
        if (IsDisposed) throw new InvalidOperationException("Pool has been disposed.");
        TValue[] items;
        lock (pool) {
            items = [.. pool.Values];
            pool.Clear();
        }

        foreach (var item in items) {
            item.Dispose();
            Debug.Assert(item.IsDisposed);
        }
    }

    protected override void OnDispose(bool disposeManagedResources) {
        Clear();
        base.OnDispose(disposeManagedResources);
    }
}
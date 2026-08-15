/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace HelixToolkit.SharpDX.Core.Utilities;

public interface IDisposeObject: IDisposable
{
    /// <summary>
    ///     Occurs when this instance is starting to be disposed.
    /// </summary>
    event EventHandler<BoolEventArgs>? Disposing;

    /// <summary>
    ///     Occurs when this instance is fully disposed.
    /// </summary>
    event EventHandler<BoolEventArgs>? Disposed;

    int RefCount { get; }

    /// <summary>
    ///     Gets a value indicating whether this instance is disposed.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
    /// </value>
    bool IsDisposed { get; }

    /// <summary>
    ///     Increase reference counter
    /// </summary>
    /// <returns></returns>
    int IncRef();

    /// <summary>
    ///     Forces the dispose.
    /// </summary>
    void ForceDispose();

    /// <summary>
    ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    void Dispose(bool disposing);
}

/// <summary>
///     Base class to handle disposable.
/// </summary>
public abstract class DisposeObject : IDisposable, IDisposeObject {
    internal Action<DisposeObject>? AddBackToPool;

    /// <summary>
    ///     Occurs when this instance is starting to be disposed.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public event EventHandler<BoolEventArgs>? Disposing;

    /// <summary>
    ///     Occurs when this instance is fully disposed.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public event EventHandler<BoolEventArgs>? Disposed;

    /// <summary>
    ///     Disposes of object resources.
    /// </summary>
    /// <param name="disposeManagedResources">
    ///     If true, managed resources should be
    ///     disposed of in addition to unmanaged resources.
    /// </param>
    protected virtual void OnDispose(bool disposeManagedResources) { }

    /// <summary>
    ///     Dispose a disposable object and set the reference to null. Removes this object from this instance..
    /// </summary>
    /// <param name="objectToDispose">Object to dispose.</param>
    public static void RemoveAndDispose<T>(ref T? objectToDispose) where T : class, IDisposable {
        objectToDispose?.Dispose();
        objectToDispose = null;
    }

    /// <summary>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="backingField"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    protected static bool Set<T>(ref T backingField, T value) {
        if (EqualityComparer<T>.Default.Equals(backingField, value))
            return false;
        
        backingField = value;
        return true;
    }

    #region IDisposible

    public int RefCount => AtomicHelper.Read(ref refCounter);

    private int refCounter = 1;

    /// <summary>
    ///     Increase reference counter
    /// </summary>
    /// <returns></returns>
    public int IncRef() {
        // Increment only greater than 1
        AtomicHelper.IncrementIfGreaterThan(ref refCounter, 0);
        return AtomicHelper.Read(ref refCounter);
    }

    /// <summary>
    ///     Forces the dispose.
    /// </summary>
    public void ForceDispose() {
        // Set ref counter to 1 if greater than 1
        AtomicHelper.ExchangeIfGreaterThan(ref refCounter, 1, 1);
        Dispose();
    }

    /// <summary>
    ///     Gets a value indicating whether this instance is disposed.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
    /// </value>
    public bool IsDisposed { get; private set; }

    private int disposeCount;

    /// <summary>
    ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "False positive.")]
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
    public void Dispose()
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
    {
        Dispose(true);
    }

    /// <summary>
    ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
#pragma warning disable CA1063 // Implement IDisposable Correctly
    public void Dispose(bool disposing)
#pragma warning restore CA1063 // Implement IDisposable Correctly
    {
        // If already 0, return.
        if (!AtomicHelper.DecrementIfGreaterThan(ref refCounter, 0)) {
            Debug.Assert(RefCount == 0);
            return;
        }

        var currRef = RefCount;
        if (currRef == 0 && !IsDisposed) {
            if (Interlocked.Increment(ref disposeCount) != 1)
                return;
            
            AddBackToPool = null;
            Disposing?.Invoke(this, disposing ? BoolEventArgs.TrueArgs : BoolEventArgs.FalseArgs);
            Disposing = null;
            OnDispose(disposing);
            //GC.SuppressFinalize(this);

            IsDisposed = true;

            Disposed?.Invoke(this, disposing ? BoolEventArgs.TrueArgs : BoolEventArgs.FalseArgs);
            Disposed = null;
        } else if (currRef == 1) {
            AddBackToPool?.Invoke(this);
        }
    }

    #endregion
}

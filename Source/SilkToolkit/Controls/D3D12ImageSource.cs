using System;
using System.Windows;
using System.Windows.Interop;

namespace HelixToolkit.Wpf.SharpDX.Controls;

/// <summary>Publishes caller-owned, completed D3D9 surfaces through one dispatcher-owned WPF image.</summary>
/// <remarks>
/// This type never creates or releases native surfaces. The caller must keep every possibly bound allocation
/// alive until successful publication of its replacement or successful detachment, including Unlock.
/// Front-buffer loss does not authorize releasing a surface. Access NativeImage only for visual binding and
/// inspection; changing its back buffer directly would invalidate BoundSurface bookkeeping.
/// </remarks>
internal sealed class D3D12ImageSource : IDisposable {
    /// <summary>Bounds each ordinary image-lock attempt without waiting for desktop composition.</summary>
    private static readonly Duration LockTimeout = new(TimeSpan.Zero);

    /// <summary>Bounds explicit disposal retries without a dispatcher loop, sleep or unbounded wait.</summary>
    private const int DetachAttempts = 3;

    /// <summary>Rejects new publications after disposal begins, even if detachment needs another attempt.</summary>
    private bool stopping;

    /// <summary>Records successful detachment and subscription removal.</summary>
    private bool disposed;

    /// <summary>Retains one successful dispatcher-owned write lock across nonwaiting copy polls.</summary>
    internal bool IsWriteLocked { get; private set; }

    /// <summary>Replaces only the native image calls in headless transport tests.</summary>
    private readonly Func<bool>? tryLock;
    /// <summary>Replaces only native binding in headless transport tests.</summary>
    private readonly Action<nint>? setBackBuffer;
    /// <summary>Replaces only native dirtying in headless transport tests.</summary>
    private readonly Action<Int32Rect>? addDirtyRect;
    /// <summary>Replaces only native unlocking in headless transport tests.</summary>
    private readonly Action? unlock;

    /// <summary>Uses the existing presentation owner's bounded metadata observer, if attached.</summary>
    internal Action<string, int>? ObservePublication { get; set; }

    /// <summary>Records call boundaries without swallowing native failures.</summary>
    private void Observe(string phase, int status = 0) => ObservePublication?.Invoke(phase, status);

    /// <summary>Creates the retained image and its front-buffer notification subscription.</summary>
    internal D3D12ImageSource() {
        NativeImage.IsFrontBufferAvailableChanged += OnFrontBufferChanged;
    }

    /// <summary>Controls the existing image boundary without creating native surfaces in CPU tests.</summary>
    internal D3D12ImageSource(Func<bool> tryLock, Action<nint> setBackBuffer,
        Action<Int32Rect> addDirtyRect, Action unlock) : this() {
        this.tryLock = tryLock;
        this.setBackBuffer = setBackBuffer;
        this.addDirtyRect = addDirtyRect;
        this.unlock = unlock;
    }

    /// <summary>Acquires a write lease on this dispatcher; a normal false result is balanced immediately.</summary>
    /// <returns>Whether a native copy may write the bound output on this thread.</returns>
    internal bool TryBeginWrite() {
        NativeImage.VerifyAccess();
        ObjectDisposedException.ThrowIf(stopping, this);
        return AcquireWrite();
    }

    /// <summary>Balances native zero-timeout locks, including during retryable detachment.</summary>
    private bool AcquireWrite() {
        NativeImage.VerifyAccess();
        if (IsWriteLocked) throw new InvalidOperationException("The image already owns a write lock");
        Observe("lock.begin");
        bool acquired;
        try {
            acquired = tryLock?.Invoke() ?? NativeImage.TryLock(LockTimeout);
        } catch (Exception error) {
            Observe("lock.error", error.HResult);
            throw;
        }
        IsWriteLocked = true;
        try {
            Observe(acquired ? "lock.acquired" : "lock.miss");
        } finally {
            // Confirmed in the shipped .NET 11 RC3 IL: a normal false also increments the lock count.
            if (!acquired) EndWrite();
        }
        return acquired;
    }

    /// <summary>Releases the retained lease only after the owner has completed or retired its native copy.</summary>
    internal void EndWrite() {
        NativeImage.VerifyAccess();
        if (!IsWriteLocked) return;
        if (NativeImage.IsFrozen) throw new InvalidOperationException("The image must remain writable");
        Observe("unlock.begin");
        try {
            // WPF decrements before commit/Changed callbacks can throw; a retry must acquire a fresh lock.
            IsWriteLocked = false;
            if (unlock is not null) unlock();
            else NativeImage.Unlock();
            Observe("unlock.done");
        } catch (Exception error) {
            Observe("unlock.error", error.HResult);
            throw;
        }
    }

    /// <summary>Updates actual binding bookkeeping only after the native call succeeds.</summary>
    private void Bind(nint surface) {
        if (setBackBuffer is not null) setBackBuffer(surface);
        else NativeImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, surface, enableSoftwareFallback: true);
        BoundSurface = surface;
    }

    /// <summary>Gets the actual retained WPF image for Image.Source binding and read-only inspection.</summary>
    internal D3DImage NativeImage { get; } = new();

    /// <summary>Gets hardware front-buffer availability independently of software publication.</summary>
    internal bool IsFrontBufferAvailable => NativeImage.IsFrontBufferAvailable;

    /// <summary>Gets the actual last successful SetBackBuffer pointer, including after a later operation fails.</summary>
    /// <remarks>A zero value alone does not confirm successful Unlock; use the result of TryDetach.</remarks>
    internal nint BoundSurface { get; private set; }

    /// <summary>Notifies the owner of front-buffer loss or return without changing native ownership.</summary>
    internal event EventHandler? FrontBufferChanged;

    /// <summary>Binds changed output and dirties a completed surface, ending its retained write lease.</summary>
    /// <param name="surface">The nonzero borrowed IDirect3DSurface9 pointer, kept alive by the caller.</param>
    /// <param name="width">The nonzero surface width in physical pixels.</param>
    /// <param name="height">The nonzero surface height in physical pixels.</param>
    /// <returns>True only after binding, dirtying and Unlock succeed; false leaves a missed-lock binding unchanged.</returns>
    /// <exception cref="ObjectDisposedException">Disposal has started.</exception>
    /// <exception cref="ArgumentException">The surface pointer is zero.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A dimension cannot describe a WPF surface.</exception>
    /// <remarks>
    /// Native WPF errors propagate; inspect BoundSurface before recovery, because SetBackBuffer may have
    /// succeeded. The same completed pointer needs only DirtyRect and Unlock, including front-buffer recovery.
    /// </remarks>
    internal bool TryPublish(nint surface, uint width, uint height) {
        NativeImage.VerifyAccess();
        ObjectDisposedException.ThrowIf(stopping, this);
        if (surface == 0)
            throw new ArgumentException("A completed native surface is required.", nameof(surface));
        if (width == 0 || width > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (height == 0 || height > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(height));
        if (!IsWriteLocked && !TryBeginWrite()) return false;
        try {
            if (BoundSurface != surface) {
                Observe("backbuffer.begin");
                Bind(surface);
                Observe("backbuffer.done");
            }
            Observe("dirty.begin");
            var rectangle = new Int32Rect(0, 0, (int) width, (int) height);
            if (addDirtyRect is not null) addDirtyRect(rectangle);
            else NativeImage.AddDirtyRect(rectangle);
            Observe("dirty.done");
        } catch (Exception error) {
            Observe("image.error", error.HResult);
            throw;
        } finally {
            EndWrite();
        }

        return true;
    }

    /// <summary>Attempts to detach the borrowed surface under a zero-timeout lock, including during front-buffer loss.</summary>
    /// <returns>True only after successful detachment and Unlock; false retains the existing binding.</returns>
    /// <remarks>WPF errors propagate with actual binding bookkeeping retained for controlled recovery.</remarks>
    internal bool TryDetach() {
        NativeImage.VerifyAccess();
        if (disposed)
            return true;
        // An unload or direct canvas disposal must not unlock an in-flight native write.
        if (IsWriteLocked) return false;
        if (!AcquireWrite()) return false;
        try {
            Observe("detach.begin");
            Bind(0);
            Observe("detach.done");
        } catch (Exception error) {
            Observe("image.error", error.HResult);
            throw;
        } finally {
            EndWrite();
        }

        return true;
    }

    /// <summary>Stops publication and makes a fixed number of short detachment attempts before removing subscriptions.</summary>
    /// <exception cref="TimeoutException">Detachment failed; retain this image and native allocations and retry disposal.</exception>
    /// <remarks>
    /// WPF has no D3DImage.Dispose. This owner retains NativeImage even after successful disposal for inspection.
    /// On failure its image, actual binding and front-buffer subscription remain available for recovery.
    /// The caller alone disposes native allocations, and only after successful detachment.
    /// </remarks>
    public void Dispose() {
        for (var attempt = 0; attempt < DetachAttempts; attempt++)
            if (TryDispose()) return;
        throw new TimeoutException("WPF detachment failed; retain the image and native surfaces and retry disposal.");
    }

    /// <summary>Completes image disposal only after actual detachment and successful Unlock.</summary>
    /// <returns>False while a native write lease is retained or the zero-duration WPF detach lock misses.</returns>
    internal bool TryDispose() {
        NativeImage.VerifyAccess();
        if (disposed) return true;
        stopping = true;
        if (!TryDetach()) return false;
        NativeImage.IsFrontBufferAvailableChanged -= OnFrontBufferChanged;
        FrontBufferChanged = null;
        disposed = true;
        return true;
    }

    /// <summary>Forwards the original availability transition without detaching or freeing a native allocation.</summary>
    /// <param name="sender">The retained D3DImage.</param>
    /// <param name="args">The original WPF dependency-property transition.</param>
    private void OnFrontBufferChanged(object sender, DependencyPropertyChangedEventArgs args) {
        FrontBufferChanged?.Invoke(this, EventArgs.Empty);
    }
}

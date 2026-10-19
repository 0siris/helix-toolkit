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
internal sealed class D3D12ImageSource : IDisposable
{
    /// <summary>Bounds each ordinary image-lock attempt without waiting for desktop composition.</summary>
    private static readonly Duration LockTimeout = new(TimeSpan.Zero);

    /// <summary>Bounds explicit disposal retries without a dispatcher loop, sleep or unbounded wait.</summary>
    private const int DetachAttempts = 3;

    /// <summary>Rejects new publications after disposal begins, even if detachment needs another attempt.</summary>
    private bool stopping;

    /// <summary>Records successful detachment and subscription removal.</summary>
    private bool disposed;

    /// <summary>Creates the retained image and its front-buffer notification subscription.</summary>
    internal D3D12ImageSource()
    {
        NativeImage.IsFrontBufferAvailableChanged += OnFrontBufferChanged;
    }

    /// <summary>Gets the actual retained WPF image for Image.Source binding and read-only inspection.</summary>
    internal D3DImage NativeImage { get; } = new();

    /// <summary>Gets whether WPF currently permits publication to its front buffer.</summary>
    internal bool IsFrontBufferAvailable => NativeImage.IsFrontBufferAvailable;

    /// <summary>Gets the actual last successful SetBackBuffer pointer, including after a later operation fails.</summary>
    /// <remarks>A zero value alone does not confirm successful Unlock; use the result of TryDetach.</remarks>
    internal nint BoundSurface { get; private set; }

    /// <summary>Notifies the owner of front-buffer loss or return without changing native ownership.</summary>
    internal event EventHandler? FrontBufferChanged;

    /// <summary>Attempts to bind and dirty one completed immutable native surface without waiting.</summary>
    /// <param name="surface">The nonzero borrowed IDirect3DSurface9 pointer, kept alive by the caller.</param>
    /// <param name="width">The nonzero surface width in physical pixels.</param>
    /// <param name="height">The nonzero surface height in physical pixels.</param>
    /// <returns>True only after binding, dirtying and Unlock succeed; false leaves a missed-lock binding unchanged.</returns>
    /// <exception cref="ObjectDisposedException">Disposal has started.</exception>
    /// <exception cref="ArgumentException">The surface pointer is zero.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A dimension cannot describe a WPF surface.</exception>
    /// <remarks>
    /// Native WPF errors propagate; inspect BoundSurface before recovery, because SetBackBuffer may have
    /// succeeded. Rebinding the same completed pointer is intentional for front-buffer recovery.
    /// </remarks>
    internal bool TryPublish(nint surface, uint width, uint height)
    {
        NativeImage.VerifyAccess();
        ObjectDisposedException.ThrowIf(stopping, this);
        if (surface == 0)
            throw new ArgumentException("A completed native surface is required.", nameof(surface));
        if (width == 0 || width > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (height == 0 || height > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(height));
        if (!IsFrontBufferAvailable)
            return false;

        var locked = NativeImage.TryLock(LockTimeout);
        try
        {
            if (!locked || !IsFrontBufferAvailable)
                return false;
            NativeImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, surface, enableSoftwareFallback: true);
            BoundSurface = surface;
            NativeImage.AddDirtyRect(new Int32Rect(0, 0, (int)width, (int)height));
        }
        finally
        {
            // The repository's .NET 10 proof requires balancing even a normally returned false TryLock.
            NativeImage.Unlock();
        }

        return true;
    }

    /// <summary>Attempts to detach the borrowed surface under a zero-timeout lock, including during front-buffer loss.</summary>
    /// <returns>True only after successful detachment and Unlock; false retains the existing binding.</returns>
    /// <remarks>WPF errors propagate with actual binding bookkeeping retained for controlled recovery.</remarks>
    internal bool TryDetach()
    {
        NativeImage.VerifyAccess();
        if (disposed)
            return true;
        var locked = NativeImage.TryLock(LockTimeout);
        try
        {
            if (!locked)
                return false;
            NativeImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, 0, enableSoftwareFallback: true);
            BoundSurface = 0;
        }
        finally
        {
            NativeImage.Unlock();
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
    public void Dispose()
    {
        NativeImage.VerifyAccess();
        if (disposed)
            return;
        stopping = true;
        for (var attempt = 0; attempt < DetachAttempts; attempt++)
        {
            if (!TryDetach())
                continue;
            NativeImage.IsFrontBufferAvailableChanged -= OnFrontBufferChanged;
            FrontBufferChanged = null;
            disposed = true;
            return;
        }

        throw new TimeoutException("WPF detachment failed; retain the image and native surfaces and retry disposal.");
    }

    /// <summary>Forwards the original availability transition without detaching or freeing a native allocation.</summary>
    /// <param name="sender">The retained D3DImage.</param>
    /// <param name="args">The original WPF dependency-property transition.</param>
    private void OnFrontBufferChanged(object sender, DependencyPropertyChangedEventArgs args)
    {
        FrontBufferChanged?.Invoke(this, EventArgs.Empty);
    }
}

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace HelixToolkit.Wpf.SharpDX.Controls;

/// <summary>Provides a passive WPF image, physical pixel dimensions and lifecycle notifications to its owner.</summary>
/// <remarks>
/// This canvas owns no scene, renderer, device or native surface. Rendering forwards WPF's original event
/// arguments; the presentation owner decides whether any scene, completion or publication work is needed.
/// Input and layout remain in WPF DIPs independently of the image's pixel resolution.
/// </remarks>
internal sealed class DPFCanvas : Grid, IDisposable
{
    /// <summary>Retains the visual until its borrowed native back buffer is safely detached.</summary>
    private readonly Image image;

    /// <summary>Reuses the callback for coalesced lifecycle changes; no callback is posted on ordinary ticks.</summary>
    private readonly Action notifyPresentationChanged;

    /// <summary>Reuses the static-event callback across visibility and load transitions.</summary>
    private readonly EventHandler renderingCallback;

    /// <summary>Reuses the source-change callback across load transitions.</summary>
    private readonly SourceChangedEventHandler sourceChangedCallback;

    /// <summary>Stores at most one pending notification; unloading aborts it before notifying synchronously.</summary>
    private DispatcherOperation? notification;

    /// <summary>Records receipt of Loaded independently of native-window visibility.</summary>
    private bool loaded;

    /// <summary>Tracks the single static CompositionTarget.Rendering subscription.</summary>
    private bool renderingSubscribed;

    /// <summary>Tracks the single presentation-source subscription.</summary>
    private bool sourceSubscribed;

    /// <summary>Enables physical DPI resolution by default without changing DIP input.</summary>
    private bool enableDpiScale = true;

    /// <summary>Suppresses duplicate WPF timestamps, including TimeSpan.Zero after the first tick.</summary>
    private TimeSpan lastRenderingTime;

    /// <summary>Distinguishes the first timestamp from an actual zero-time rendering event.</summary>
    private bool hasRenderingTime;

    /// <summary>Prevents lifecycle reactivation once explicit disposal starts.</summary>
    private bool stopping;

    /// <summary>Records successful image detachment and final visual cleanup.</summary>
    private bool disposed;

    /// <summary>Creates one Stretch.Fill image and retained lifecycle callbacks.</summary>
    internal DPFCanvas()
    {
        notifyPresentationChanged = NotifyPresentationChanged;
        renderingCallback = OnRendering;
        sourceChangedCallback = OnSourceChanged;
        image = new Image { Source = ImageSource.NativeImage, Stretch = Stretch.Fill };
        Children.Add(image);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        IsVisibleChanged += OnVisibilityChanged;
        ImageSource.FrontBufferChanged += OnFrontBufferChanged;
    }

    /// <summary>Gets the retained image source whose native pointers remain owned by the presentation owner.</summary>
    internal D3D12ImageSource ImageSource { get; } = new();

    /// <summary>Gets or sets whether native pixels use monitor DPI rather than one pixel per DIP.</summary>
    internal bool EnableDpiScale
    {
        get => enableDpiScale;
        set
        {
            VerifyAccess();
            ObjectDisposedException.ThrowIf(stopping, this);
            if (enableDpiScale == value)
                return;
            enableDpiScale = value;
            RefreshPresentation();
            QueueNotification();
        }
    }

    /// <summary>Gets the horizontal physical DPI scale, or one when scaling is disabled.</summary>
    internal float EffectiveDpiScale { get; private set; } = 1;

    /// <summary>Gets the ceiling of the layout width multiplied by the effective horizontal DPI scale.</summary>
    internal uint PixelWidth { get; private set; }

    /// <summary>Gets the ceiling of the layout height multiplied by the effective vertical DPI scale.</summary>
    internal uint PixelHeight { get; private set; }

    /// <summary>Gets the actual WPF source HWND, borrowed only for native device creation, never presentation.</summary>
    internal nint NativeHandle { get; private set; }

    /// <summary>Gets whether a loaded, visible, nonempty canvas has a real WPF source and can service ticks.</summary>
    internal bool IsActive { get; private set; }

    /// <summary>Forwards each distinct WPF rendering timestamp using the original RenderingEventArgs instance.</summary>
    internal event EventHandler<RenderingEventArgs>? Rendering;

    /// <summary>Notifies the owner of coalesced presentation changes; unload and disposal notify synchronously.</summary>
    internal event EventHandler? PresentationChanged;

    /// <summary>Stops callbacks, requests native detachment and clears the visual only after successful image disposal.</summary>
    /// <exception cref="TimeoutException">The image remains bound; retain this canvas and native resources and retry.</exception>
    /// <remarks>No parent owner, bridge, application scene or manager is disposed by this method.</remarks>
    public void Dispose()
    {
        VerifyAccess();
        if (disposed)
            return;
        var firstAttempt = !stopping;
        stopping = true;
        loaded = false;
        IsActive = false;
        NativeHandle = 0;
        StopSubscriptions();
        CancelNotification();
        try
        {
            ImageSource.Dispose();
        }
        finally
        {
            // Even a failed detach must reach the native owner while its allocations remain retained.
            if (firstAttempt)
                PresentationChanged?.Invoke(this, EventArgs.Empty);
        }

        ImageSource.FrontBufferChanged -= OnFrontBufferChanged;
        Loaded -= OnLoaded;
        Unloaded -= OnUnloaded;
        IsVisibleChanged -= OnVisibilityChanged;
        image.Source = null;
        Children.Remove(image);
        Rendering = null;
        PresentationChanged = null;
        disposed = true;
    }

    /// <summary>Updates physical dimensions after layout without changing DIP coordinates.</summary>
    /// <param name="sizeInfo">The actual WPF render-size transition.</param>
    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        RefreshPresentation();
        QueueNotification();
    }

    /// <summary>Refreshes pixel dimensions after a monitor or root DPI change.</summary>
    /// <param name="oldDpi">The preceding monitor DPI.</param>
    /// <param name="newDpi">The current monitor DPI.</param>
    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        RefreshPresentation();
        QueueNotification();
    }

    /// <summary>Reactivates source tracking and schedules one notification after the owner's Loaded handler can attach.</summary>
    /// <param name="sender">The canvas receiving Loaded.</param>
    /// <param name="args">The original routed lifecycle event.</param>
    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        if (stopping || loaded)
            return;
        loaded = true;
        if (!sourceSubscribed)
        {
            PresentationSource.AddSourceChangedHandler(this, sourceChangedCallback);
            sourceSubscribed = true;
        }

        RefreshPresentation();
        QueueNotification();
    }

    /// <summary>Cancels pending callbacks and detaches before notifying the owner, retaining the image on failure.</summary>
    /// <param name="sender">The unloading canvas.</param>
    /// <param name="args">The original routed lifecycle event.</param>
    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        if (stopping || !loaded)
            return;
        loaded = false;
        IsActive = false;
        NativeHandle = 0;
        StopSubscriptions();
        CancelNotification();
        try
        {
            _ = ImageSource.TryDetach();
        }
        finally
        {
            PresentationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Starts or stops the single rendering subscription when WPF visibility changes.</summary>
    /// <param name="sender">The canvas whose visibility changed.</param>
    /// <param name="args">The original visibility transition.</param>
    private void OnVisibilityChanged(object sender, DependencyPropertyChangedEventArgs args)
    {
        RefreshPresentation();
        QueueNotification();
    }

    /// <summary>Refreshes the borrowed actual HWND after WPF source replacement.</summary>
    /// <param name="sender">The canvas whose presentation source changed.</param>
    /// <param name="args">The old and new presentation sources.</param>
    private void OnSourceChanged(object sender, SourceChangedEventArgs args)
    {
        RefreshPresentation();
        QueueNotification();
    }

    /// <summary>Coalesces front-buffer recovery with other presentation changes without requesting scene work itself.</summary>
    /// <param name="sender">The retained image source.</param>
    /// <param name="args">The original availability notification.</param>
    private void OnFrontBufferChanged(object? sender, EventArgs args) => QueueNotification();

    /// <summary>Reads current WPF geometry and source state and updates subscriptions only on actual transitions.</summary>
    private void RefreshPresentation()
    {
        if (stopping)
            return;
        var dpi = VisualTreeHelper.GetDpi(this);
        var scaleX = enableDpiScale ? (float)dpi.DpiScaleX : 1;
        var scaleY = enableDpiScale ? (float)dpi.DpiScaleY : 1;
        var width = ToPixelDimension(ActualWidth, scaleX);
        var height = ToPixelDimension(ActualHeight, scaleY);
        var handle = loaded && PresentationSource.FromVisual(this) is HwndSource source ? source.Handle : 0;
        var active = loaded && IsVisible && handle != 0 && width != 0 && height != 0;
        var changed = EffectiveDpiScale != scaleX || PixelWidth != width || PixelHeight != height ||
            NativeHandle != handle || IsActive != active;
        EffectiveDpiScale = scaleX;
        PixelWidth = width;
        PixelHeight = height;
        NativeHandle = handle;
        IsActive = active;
        if (active != renderingSubscribed)
        {
            if (active)
                CompositionTarget.Rendering += renderingCallback;
            else
                CompositionTarget.Rendering -= renderingCallback;
            renderingSubscribed = active;
        }

        if (changed)
            QueueNotification();
    }

    /// <summary>Converts finite WPF layout to a bounded native dimension without allocating.</summary>
    /// <param name="dips">The WPF dimension in device-independent units.</param>
    /// <param name="scale">The effective monitor scale.</param>
    /// <returns>Zero for empty layout, otherwise the ceiling clamped to WPF's signed dimension limit.</returns>
    private static uint ToPixelDimension(double dips, float scale) =>
        dips > 0 ? (uint)Math.Min(int.MaxValue, Math.Ceiling(dips * scale)) : 0;

    /// <summary>Posts one retained callback per group of actual presentation changes, never per rendering tick.</summary>
    private void QueueNotification()
    {
        if (!loaded || stopping || notification is not null || Dispatcher.HasShutdownStarted)
            return;
        notification = Dispatcher.BeginInvoke(DispatcherPriority.Render, notifyPresentationChanged);
    }

    /// <summary>Clears the pending marker before calling the owner so reentrant changes can schedule a later notification.</summary>
    private void NotifyPresentationChanged()
    {
        notification = null;
        if (loaded && !stopping)
            PresentationChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Aborts the sole queued lifecycle callback during unload or disposal.</summary>
    private void CancelNotification()
    {
        notification?.Abort();
        notification = null;
    }

    /// <summary>Removes static and source subscriptions once, preserving timestamp deduplication across reload.</summary>
    private void StopSubscriptions()
    {
        if (renderingSubscribed)
        {
            CompositionTarget.Rendering -= renderingCallback;
            renderingSubscribed = false;
        }

        if (sourceSubscribed)
        {
            PresentationSource.RemoveSourceChangedHandler(this, sourceChangedCallback);
            sourceSubscribed = false;
        }
    }

    /// <summary>Forwards the original WPF arguments once for each distinct timestamp without frame allocations.</summary>
    /// <param name="sender">The original WPF composition sender.</param>
    /// <param name="args">The actual RenderingEventArgs supplied by WPF.</param>
    private void OnRendering(object? sender, EventArgs args)
    {
        if (!IsActive || args is not RenderingEventArgs renderingArgs ||
            hasRenderingTime && renderingArgs.RenderingTime == lastRenderingTime)
            return;
        hasRenderingTime = true;
        lastRenderingTime = renderingArgs.RenderingTime;
        Rendering?.Invoke(this, renderingArgs);
    }
}

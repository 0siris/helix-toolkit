using System;
using System.Collections;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;

namespace MeshDesktop.UI.Controls;

/// <summary>
///     Keeps one WPF widget above a native viewport in a popup limited to that widget's measured bounds.
/// </summary>
/// <remarks>
///     Insert this placeholder in the widget's original layout slot after removing the widget from its old parent.
///     Its logical popup child retains inherited data, resources and routed commands. The GPU surface is unchanged.
///     The native region respects rectangular WPF layout clips, the original parent bounds and the owner client area.
/// </remarks>
public sealed class ViewportWidgetOverlay : FrameworkElement, IDisposable
{
    /// The window owning the native popup and its lifetime.
    private readonly Window owner;
    /// The independently measured widget surface.
    private readonly Border surface;
    /// The WPF-owned native window above the viewport.
    private readonly Popup popup;
    /// Observes dynamic presenter content even while the independent popup has never been opened.
    private readonly DependencyPropertyDescriptor? contentDescriptor;
    /// The last physical anchor rectangle used to position the popup.
    private Rect previousBounds = Rect.Empty;
    /// The last owner DPI used for popup placement.
    private DpiScale previousDpi;
    /// The owner source observed for native modal enable/disable transitions.
    private HwndSource? ownerSource;
    /// The popup source observed for transparent native hit testing.
    private HwndSource? popupSource;
    /// The last native clipping rectangle in popup window pixels.
    private Rect? previousRegion;
    /// Whether lifecycle subscriptions have been released.
    private bool disposed;

    /// <summary>Creates the measured placeholder for an unparented viewport widget.</summary>
    /// <param name="owner">The window whose native viewport contains the widget.</param>
    /// <param name="widget">The existing widget removed from its previous parent.</param>
    /// <exception cref="ArgumentException">The widget already has a logical or visual parent.</exception>
    public ViewportWidgetOverlay(Window owner, FrameworkElement widget)
    {
        owner.VerifyAccess();
        if (widget.Parent is not null || VisualTreeHelper.GetParent(widget) is not null)
            throw new ArgumentException("Remove the viewport widget from its parent before creating the overlay", nameof(widget));
        this.owner = owner;
        surface = new Border { Child = widget };
        popup = new Popup
        {
            PlacementTarget = this,
            Placement = PlacementMode.Relative,
            AllowsTransparency = true,
            StaysOpen = true,
            PopupAnimation = PopupAnimation.None,
            Child = surface
        };
        AddLogicalChild(popup);
        if (widget is ContentControl)
        {
            contentDescriptor = DependencyPropertyDescriptor.FromProperty(ContentControl.ContentProperty, widget.GetType());
            contentDescriptor?.AddValueChanged(widget, OnWidgetContentChanged);
        }
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        LayoutUpdated += OnLayoutUpdated;
        CompositionTarget.Rendering += OnLayoutUpdated;
        IsVisibleChanged += OnVisibilityChanged;
        surface.SizeChanged += OnSurfaceSizeChanged;
        widget.IsHitTestVisibleChanged += OnWidgetHitTestVisibilityChanged;
        popup.Opened += OnPopupOpened;
        popup.Closed += OnPopupClosed;
        owner.LocationChanged += OnOwnerChanged;
        owner.SizeChanged += OnOwnerSizeChanged;
        owner.StateChanged += OnOwnerChanged;
        owner.Activated += OnOwnerChanged;
        owner.IsVisibleChanged += OnVisibilityChanged;
        owner.SourceInitialized += OnOwnerSourceInitialized;
        owner.Closed += OnOwnerClosed;
    }

    /// Gets whether the widget's native popup is currently displayed.
    public bool IsOpen => popup.IsOpen;

    /// Gets the popup as the measured placeholder's logical child for WPF inheritance and command routing.
    protected override IEnumerator LogicalChildren => disposed
        ? Array.Empty<object>().GetEnumerator() : new object[] { popup }.GetEnumerator();

    /// <summary>Gets the full physical popup rectangle before native clipping, or an empty rectangle.</summary>
    public Rect ScreenBounds
    {
        get
        {
            if (!popup.IsOpen || PresentationSource.FromVisual(surface) is not HwndSource source
                || !GetWindowRect(source.Handle, out var rectangle))
                return Rect.Empty;
            return new Rect(rectangle.Left, rectangle.Top, rectangle.Right - rectangle.Left,
                rectangle.Bottom - rectangle.Top);
        }
    }

    /// Gets the physical desktop bounds allowed to paint and receive input by the popup's native region.
    public Rect VisibleScreenBounds
    {
        get
        {
            var bounds = ScreenBounds;
            if (bounds.IsEmpty || previousRegion is not { IsEmpty: false } region)
                return Rect.Empty;
            return new Rect(bounds.Left + region.Left, bounds.Top + region.Top, region.Width, region.Height);
        }
    }

    /// <summary>Closes the popup and removes all subscriptions without disposing the existing widget.</summary>
    public void Dispose()
    {
        VerifyAccess();
        if (disposed)
            return;
        disposed = true;
        Loaded -= OnLoaded;
        Unloaded -= OnUnloaded;
        LayoutUpdated -= OnLayoutUpdated;
        CompositionTarget.Rendering -= OnLayoutUpdated;
        IsVisibleChanged -= OnVisibilityChanged;
        surface.SizeChanged -= OnSurfaceSizeChanged;
        if (surface.Child is FrameworkElement widget)
        {
            widget.IsHitTestVisibleChanged -= OnWidgetHitTestVisibilityChanged;
            contentDescriptor?.RemoveValueChanged(widget, OnWidgetContentChanged);
        }
        popup.Opened -= OnPopupOpened;
        popup.Closed -= OnPopupClosed;
        owner.LocationChanged -= OnOwnerChanged;
        owner.SizeChanged -= OnOwnerSizeChanged;
        owner.StateChanged -= OnOwnerChanged;
        owner.Activated -= OnOwnerChanged;
        owner.IsVisibleChanged -= OnVisibilityChanged;
        owner.SourceInitialized -= OnOwnerSourceInitialized;
        owner.Closed -= OnOwnerClosed;
        if (ownerSource is { IsDisposed: false })
            ownerSource.RemoveHook(OnOwnerMessage);
        ownerSource = null;
        ReleasePopupSource();
        popup.IsOpen = false;
        popup.Child = null;
        surface.Child = null;
        popup.PlacementTarget = null;
        RemoveLogicalChild(popup);
    }

    /// <summary>Repositions the bounded native widget after layout, owner movement or a DPI change.</summary>
    /// <exception cref="System.ComponentModel.Win32Exception">Windows cannot create or apply the clipping region.</exception>
    public void RefreshPlacement()
    {
        VerifyAccess();
        if (disposed)
            return;
        if (!IsLoaded || !IsVisible || !owner.IsVisible || owner.WindowState == WindowState.Minimized
            || surface.Child?.Visibility != Visibility.Visible || ActualWidth <= 0 || ActualHeight <= 0
            || surface.Child is ContentControl { Content: null })
        {
            popup.IsOpen = false;
            previousBounds = Rect.Empty;
            return;
        }
        var origin = PointToScreen(new Point());
        var dpi = VisualTreeHelper.GetDpi(this);
        var bounds = new Rect(origin, new Size(ActualWidth * dpi.DpiScaleX, ActualHeight * dpi.DpiScaleY));
        var needsPlacement = bounds != previousBounds || !dpi.Equals(previousDpi);
        previousBounds = bounds;
        previousDpi = dpi;
        if (!popup.IsOpen)
            popup.IsOpen = true;
        else if (needsPlacement)
        {
            // WPF does not automatically reposition an open Popup when its owner's HWND moves.
            popup.HorizontalOffset = .01;
            popup.HorizontalOffset = 0;
        }
        ApplyNativeClip();
    }

    /// <summary>
    ///     Clips the independent HWND to the original layout without moving or reflowing its widget content.
    /// </summary>
    /// <remarks>
    ///     Windows owns a successfully assigned region. Pixels outside it cannot paint or intercept native input.
    ///     The rectangular viewport layout clips are converted inward to physical pixels to prevent edge overflow.
    /// </remarks>
    /// <exception cref="System.ComponentModel.Win32Exception">Windows cannot create or apply the clipping region.</exception>
    private void ApplyNativeClip()
    {
        if (popupSource is not { IsDisposed: false } source || !IsLoaded || !popup.IsOpen
            || !GetWindowRect(source.Handle, out var rectangle))
            return;
        var origin = PointToScreen(new Point());
        var left = (int)Math.Round(origin.X);
        var top = (int)Math.Round(origin.Y);
        if (rectangle.Left != left || rectangle.Top != top)
        {
            // Popup placement can otherwise shift oversized content to fit the monitor work area.
            SetWindowPos(source.Handle, IntPtr.Zero, left, top, 0, 0, 0x0015);
            if (!GetWindowRect(source.Handle, out rectangle))
                return;
        }
        var visible = new Rect(rectangle.Left, rectangle.Top, rectangle.Right - rectangle.Left,
            rectangle.Bottom - rectangle.Top);
        if (VisualTreeHelper.GetParent(this) is FrameworkElement parent)
            visible.Intersect(ToScreenBounds(parent, new Rect(parent.RenderSize)));
        for (Visual? visual = this; visual is not null; visual = VisualTreeHelper.GetParent(visual) as Visual)
        {
            // Explicit clips can change without layout; the retained visual clip updates later in rendering.
            if (visual is UIElement { Clip: { } explicitClip })
                visible.Intersect(ToScreenBounds(visual, explicitClip.Bounds));
            if (VisualTreeHelper.GetClip(visual) is { } clip)
                visible.Intersect(ToScreenBounds(visual, clip.Bounds));
        }
        var ownerHandle = new WindowInteropHelper(owner).Handle;
        if (GetClientRect(ownerHandle, out var client))
        {
            var clientOrigin = new NativePoint();
            if (ClientToScreen(ownerHandle, ref clientOrigin))
                visible.Intersect(new Rect(clientOrigin.X, clientOrigin.Y,
                    client.Right - client.Left, client.Bottom - client.Top));
        }
        var regionBounds = Rect.Empty;
        if (!visible.IsEmpty)
        {
            var regionLeft = (int)Math.Ceiling(visible.Left) - rectangle.Left;
            var regionTop = (int)Math.Ceiling(visible.Top) - rectangle.Top;
            var regionRight = (int)Math.Floor(visible.Right) - rectangle.Left;
            var regionBottom = (int)Math.Floor(visible.Bottom) - rectangle.Top;
            if (regionRight > regionLeft && regionBottom > regionTop)
                regionBounds = new Rect(regionLeft, regionTop, regionRight - regionLeft, regionBottom - regionTop);
        }
        if (regionBounds == previousRegion)
            return;
        var region = regionBounds.IsEmpty ? CreateRectRgn(0, 0, 0, 0)
            : CreateRectRgn((int)regionBounds.Left, (int)regionBounds.Top,
                (int)regionBounds.Right, (int)regionBounds.Bottom);
        if (region == IntPtr.Zero)
            throw new System.ComponentModel.Win32Exception();
        if (SetWindowRgn(source.Handle, region, true) == 0)
        {
            var error = Marshal.GetLastWin32Error();
            DeleteObject(region);
            throw new System.ComponentModel.Win32Exception(error);
        }
        previousRegion = regionBounds;
    }

    /// <summary>Transforms a rectangular WPF layout clip into physical desktop coordinates.</summary>
    /// <param name="visual">The visual whose local coordinates describe the clip.</param>
    /// <param name="bounds">The local rectangular clip.</param>
    /// <returns>The corresponding physical desktop rectangle, or an empty rectangle.</returns>
    private static Rect ToScreenBounds(Visual visual, Rect bounds)
    {
        if (bounds.IsEmpty)
            return Rect.Empty;
        return new Rect(visual.PointToScreen(bounds.TopLeft), visual.PointToScreen(bounds.BottomRight));
    }

    /// <summary>Measures only the actual widget rather than reserving a transparent viewport-sized window.</summary>
    /// <param name="availableSize">The space provided by the widget's original layout slot.</param>
    /// <returns>The constrained widget size used by the placeholder.</returns>
    protected override Size MeasureOverride(Size availableSize)
    {
        surface.MaxWidth = double.IsFinite(availableSize.Width) ? availableSize.Width : double.PositiveInfinity;
        surface.MaxHeight = double.IsFinite(availableSize.Height) ? availableSize.Height : double.PositiveInfinity;
        surface.Measure(availableSize);
        return surface.DesiredSize;
    }

    /// <summary>Sizes the native popup to its arranged layout slot before aligning its independent surface.</summary>
    /// <param name="finalSize">The arranged placeholder size.</param>
    /// <returns>The final placeholder size.</returns>
    protected override Size ArrangeOverride(Size finalSize)
    {
        // PopupRoot remeasures unconstrained content; retain the original slot rather than its natural width.
        popup.Width = finalSize.Width;
        popup.Height = finalSize.Height;
        surface.Arrange(new Rect(finalSize));
        RefreshPlacement();
        return finalSize;
    }

    /// <summary>Opens a widget after its placeholder has entered the owner visual tree.</summary>
    /// <param name="sender">The loaded placeholder.</param>
    /// <param name="args">The WPF load event.</param>
    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        AttachOwnerSource();
        RefreshPlacement();
    }

    /// <summary>Closes the native surface while allowing the same widget to be reattached.</summary>
    /// <param name="sender">The unloaded placeholder.</param>
    /// <param name="args">The WPF unload event.</param>
    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        popup.IsOpen = false;
        previousBounds = Rect.Empty;
    }

    /// <summary>Updates screen placement only when the widget's geometry or DPI changed.</summary>
    /// <param name="sender">The layout source.</param>
    /// <param name="args">The layout notification.</param>
    private void OnLayoutUpdated(object? sender, EventArgs args) => RefreshPlacement();

    /// <summary>Reflects owner state, activation and movement in the native surface.</summary>
    /// <param name="sender">The owner.</param>
    /// <param name="args">The owner notification.</param>
    private void OnOwnerChanged(object? sender, EventArgs args) => RefreshPlacement();

    /// <summary>Repositions after the owner is resized.</summary>
    /// <param name="sender">The owner.</param>
    /// <param name="args">The changed owner bounds.</param>
    private void OnOwnerSizeChanged(object sender, SizeChangedEventArgs args) => RefreshPlacement();

    /// <summary>Hides native surfaces when the owner or layout slot is hidden.</summary>
    /// <param name="sender">The owner or placeholder.</param>
    /// <param name="args">The changed visibility.</param>
    private void OnVisibilityChanged(object sender, DependencyPropertyChangedEventArgs args) => RefreshPlacement();

    /// <summary>Remeasures the placeholder after dynamic workflow content changes the widget size.</summary>
    /// <param name="sender">The independent WPF widget surface.</param>
    /// <param name="args">The new widget bounds.</param>
    private void OnSurfaceSizeChanged(object sender, SizeChangedEventArgs args) => InvalidateMeasure();

    /// <summary>
    ///     Remeasures the detached presenter and its surface before a previously empty popup can open.
    /// </summary>
    /// <param name="sender">The changed content presenter.</param>
    /// <param name="args">The content change notification.</param>
    private void OnWidgetContentChanged(object? sender, EventArgs args)
    {
        surface.Child?.InvalidateMeasure();
        surface.InvalidateMeasure();
        InvalidateMeasure();
    }

    /// <summary>Updates native click-through when an existing widget changes its input policy.</summary>
    /// <param name="sender">The existing widget.</param>
    /// <param name="args">The changed WPF hit-test availability.</param>
    private void OnWidgetHitTestVisibilityChanged(object sender, DependencyPropertyChangedEventArgs args)
        => ApplyInputPolicy();

    /// <summary>Releases native and managed ownership when the application window closes.</summary>
    /// <param name="sender">The closed owner.</param>
    /// <param name="args">The close notification.</param>
    private void OnOwnerClosed(object? sender, EventArgs args) => Dispose();

    /// <summary>Observes native modal state once the owning window has an HWND.</summary>
    /// <param name="sender">The initialized owner.</param>
    /// <param name="args">The initialization notification.</param>
    private void OnOwnerSourceInitialized(object? sender, EventArgs args) => AttachOwnerSource();

    /// <summary>Attaches one native enable-state hook to the live owner HWND.</summary>
    private void AttachOwnerSource()
    {
        if (ownerSource is not null)
            return;
        ownerSource = HwndSource.FromHwnd(new WindowInteropHelper(owner).Handle);
        ownerSource?.AddHook(OnOwnerMessage);
    }

    /// <summary>Disables independent popup input while a native modal dialog disables its owner.</summary>
    /// <param name="window">The owner HWND.</param>
    /// <param name="message">The native message identifier.</param>
    /// <param name="wParam">The owner's new native enabled state.</param>
    /// <param name="lParam">The unused message data.</param>
    /// <param name="handled">Whether normal owner message processing should stop.</param>
    /// <returns>Zero to leave normal WPF processing unchanged.</returns>
    private IntPtr OnOwnerMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == 0x000A && popup.IsOpen
            && PresentationSource.FromVisual(surface) is HwndSource source)
            EnableWindow(source.Handle, wParam != IntPtr.Zero);
        return IntPtr.Zero;
    }

    /// <summary>Assigns the real owner and removes WPF's global topmost popup behavior.</summary>
    /// <param name="sender">The opened popup.</param>
    /// <param name="args">The open notification.</param>
    private void OnPopupOpened(object? sender, EventArgs args)
    {
        if (PresentationSource.FromVisual(surface) is not HwndSource source)
            return;
        ReleasePopupSource();
        popupSource = source;
        popupSource.AddHook(OnPopupMessage);
        var ownerHandle = new WindowInteropHelper(owner).Handle;
        SetWindowLongPtr(source.Handle, -8, ownerHandle);
        SetWindowPos(source.Handle, owner.Topmost ? new IntPtr(-1) : new IntPtr(-2), 0, 0, 0, 0, 0x0013);
        EnableWindow(source.Handle, IsWindowEnabled(ownerHandle));
        ApplyInputPolicy();
        ApplyNativeClip();
    }

    /// <summary>Releases the old native source before WPF recreates a hidden popup.</summary>
    /// <param name="sender">The closed popup.</param>
    /// <param name="args">The close notification.</param>
    private void OnPopupClosed(object? sender, EventArgs args) => ReleasePopupSource();

    /// <summary>Detaches the hook from a popup HWND that is about to be hidden or destroyed.</summary>
    private void ReleasePopupSource()
    {
        if (popupSource is { IsDisposed: false })
            popupSource.RemoveHook(OnPopupMessage);
        popupSource = null;
        previousRegion = null;
    }

    /// <summary>Mirrors noninteractive WPF widgets onto the layered HWND's native transparent input style.</summary>
    private void ApplyInputPolicy()
    {
        if (popupSource is not { IsDisposed: false } source)
            return;
        var style = GetWindowLongPtr(source.Handle, -20).ToInt64();
        style = surface.Child?.IsHitTestVisible == true ? style & ~0x20L : style | 0x20L;
        SetWindowLongPtr(source.Handle, -20, new IntPtr(style));
    }

    /// <summary>Passes native pointer hit tests through a noninteractive log or display widget to the GPU HWND.</summary>
    /// <param name="window">The popup HWND.</param>
    /// <param name="message">The native hit-test message.</param>
    /// <param name="wParam">The unused hit-test parameter.</param>
    /// <param name="lParam">The physical cursor position.</param>
    /// <param name="handled">Whether the native hit test has been answered.</param>
    /// <returns>HTTRANSPARENT for display-only widgets, or zero for normal WPF processing.</returns>
    private IntPtr OnPopupMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == 0x0084 && surface.Child?.IsHitTestVisible != true)
        {
            handled = true;
            return new IntPtr(-1);
        }
        return IntPtr.Zero;
    }

    /// Describes a native physical window rectangle.
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRectangle
    {
        /// The left desktop pixel.
        public int Left;
        /// The top desktop pixel.
        public int Top;
        /// The exclusive right desktop pixel.
        public int Right;
        /// The exclusive bottom desktop pixel.
        public int Bottom;
    }

    /// Describes a native physical point used to locate the owner's client rectangle.
    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        /// The horizontal desktop pixel.
        public int X;
        /// The vertical desktop pixel.
        public int Y;
    }

    /// <summary>Reads the drawable owner client rectangle without window decorations.</summary>
    /// <param name="window">The owner HWND.</param>
    /// <param name="rectangle">The resulting client bounds.</param>
    /// <returns>Whether the client bounds were available.</returns>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetClientRect(IntPtr window, out NativeRectangle rectangle);

    /// <summary>Converts the owner client origin to desktop pixels.</summary>
    /// <param name="window">The owner HWND.</param>
    /// <param name="point">The client point replaced with its desktop coordinates.</param>
    /// <returns>Whether the conversion succeeded.</returns>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ClientToScreen(IntPtr window, ref NativePoint point);

    /// <summary>Creates the physical clipping region for the separate widget HWND.</summary>
    /// <param name="left">The inclusive left window pixel.</param>
    /// <param name="top">The inclusive top window pixel.</param>
    /// <param name="right">The exclusive right window pixel.</param>
    /// <param name="bottom">The exclusive bottom window pixel.</param>
    /// <returns>The newly owned region handle, or zero on failure.</returns>
    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateRectRgn(int left, int top, int right, int bottom);

    /// <summary>Transfers a clipping region to Windows for both painting and native input boundaries.</summary>
    /// <param name="window">The popup HWND.</param>
    /// <param name="region">The region transferred to Windows only on success.</param>
    /// <param name="redraw">Whether Windows should redraw the clipped window.</param>
    /// <returns>Nonzero on success, or zero on failure.</returns>
    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowRgn(IntPtr window, IntPtr region, [MarshalAs(UnmanagedType.Bool)] bool redraw);

    /// <summary>Releases a clipping region that Windows did not accept.</summary>
    /// <param name="value">The caller-owned native region.</param>
    /// <returns>Whether the native object was released.</returns>
    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr value);

    /// <summary>Reads the rectangle of the independently rendered popup HWND.</summary>
    /// <param name="window">The popup HWND.</param>
    /// <param name="rectangle">The resulting physical bounds.</param>
    /// <returns>Whether the window rectangle was available.</returns>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr window, out NativeRectangle rectangle);

    /// <summary>Assigns native ownership without changing popup activation or input handling.</summary>
    /// <param name="window">The popup HWND.</param>
    /// <param name="index">The native owner index.</param>
    /// <param name="value">The owning window HWND.</param>
    /// <returns>The former native owner.</returns>
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr window, int index, IntPtr value);

    /// <summary>Reads existing extended styles before applying native click-through.</summary>
    /// <param name="window">The popup HWND.</param>
    /// <param name="index">The extended-style index.</param>
    /// <returns>The current style bits.</returns>
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr window, int index);

    /// <summary>Reads native owner input availability, including modal-dialog disabling.</summary>
    /// <param name="window">The owner HWND.</param>
    /// <returns>Whether native input is enabled.</returns>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowEnabled(IntPtr window);

    /// <summary>Mirrors modal native input availability onto the separate widget HWND.</summary>
    /// <param name="window">The popup HWND.</param>
    /// <param name="enabled">Whether native input should be enabled.</param>
    /// <returns>Whether the native window was previously disabled.</returns>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnableWindow(IntPtr window, [MarshalAs(UnmanagedType.Bool)] bool enabled);

    /// <summary>Updates popup placement or z-order without resizing or activating either window.</summary>
    /// <param name="window">The popup HWND.</param>
    /// <param name="insertAfter">The required owner-compatible z-order.</param>
    /// <param name="x">The horizontal desktop position when movement is requested.</param>
    /// <param name="y">The vertical desktop position when movement is requested.</param>
    /// <param name="width">The unused width.</param>
    /// <param name="height">The unused height.</param>
    /// <param name="flags">The native flags controlling position, size, z-order and activation.</param>
    /// <returns>Whether Windows accepted the z-order update.</returns>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height,
        uint flags);
}

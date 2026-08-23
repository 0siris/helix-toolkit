/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace HelixToolkit.Wpf.SharpDX.Controls;

/// <summary>
///     Hosts the native child window used by a Direct3D 12 swap chain without Windows Forms.
/// </summary>
public sealed unsafe partial class HwndSwapChainHost : HwndHost {
    private nint nativeHandle;

    /// <summary>
    ///     Creates a host and attaches its native message bridge.
    /// </summary>
    public HwndSwapChainHost() {
        MessageHook += OnMessage;
    }

    /// <summary>
    ///     Gets the current horizontal DPI scale.
    /// </summary>
    public double DpiScale { get; private set; } = 1;

    /// <summary>
    ///     Gets whether the native child window currently exists.
    /// </summary>
    public bool IsHandleCreated => nativeHandle != 0;

    /// <summary>
    ///     Gets the native child-window handle, or zero before creation and after destruction.
    /// </summary>
    public nint NativeHandle => nativeHandle;

    /// <summary>
    ///     Occurs after the native child window has been created.
    /// </summary>
    public event EventHandler? HandleCreated;

    /// <summary>
    ///     Occurs after the native child window has been destroyed.
    /// </summary>
    public event EventHandler? HandleDestroyed;

    /// <summary>
    ///     Occurs when the effective DPI scale changes.
    /// </summary>
    public event EventHandler<double>? DpiScaleChanged;

    /// <summary>
    ///     Occurs for mouse and touch movement, button, contact, and wheel input over the child window.
    /// </summary>
    public event EventHandler<HwndPointerEventArgs>? PointerInput;

    /// <inheritdoc />
    protected override HandleRef BuildWindowCore(HandleRef hwndParent) {
        nativeHandle = NativeMethods.CreateWindowEx(0,
            "STATIC",
            null,
            NativeMethods.ChildWindowStyles,
            0,
            0,
            1,
            1,
            hwndParent.Handle,
            0,
            0,
            0);
        if (nativeHandle == 0)
            throw new Win32Exception(Marshal.GetLastPInvokeError(), "Could not create the swap-chain child window.");

        if (!NativeMethods.RegisterTouchWindow(nativeHandle, 0)) {
            var error = Marshal.GetLastPInvokeError();
            NativeMethods.DestroyWindow(nativeHandle);
            nativeHandle = 0;
            throw new Win32Exception(error, "Could not register touch input for the swap-chain child window.");
        }

        UpdateDpiScale(VisualTreeHelper.GetDpi(this).DpiScaleX);
        HandleCreated?.Invoke(this, EventArgs.Empty);
        return new HandleRef(this, nativeHandle);
    }

    /// <inheritdoc />
    protected override void DestroyWindowCore(HandleRef hwnd) {
        if (hwnd.Handle != 0) NativeMethods.UnregisterTouchWindow(hwnd.Handle);
        if (hwnd.Handle != 0 && !NativeMethods.DestroyWindow(hwnd.Handle))
            throw new Win32Exception(Marshal.GetLastPInvokeError(), "Could not destroy the swap-chain child window.");

        nativeHandle = 0;
        HandleDestroyed?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi) {
        base.OnDpiChanged(oldDpi, newDpi);
        UpdateDpiScale(newDpi.DpiScaleX);
    }

    /// <summary>
    ///     Updates and publishes the current DPI scale.
    /// </summary>
    /// <param name="value">The new scale.</param>
    private void UpdateDpiScale(double value) {
        if (DpiScale.Equals(value)) return;

        DpiScale = value;
        DpiScaleChanged?.Invoke(this, value);
    }

    /// <summary>
    ///     Translates native window messages into the renderer-neutral pointer event stream.
    /// </summary>
    /// <param name="window">The child window.</param>
    /// <param name="message">The native message identifier.</param>
    /// <param name="wParam">The native word parameter.</param>
    /// <param name="lParam">The native long parameter.</param>
    /// <param name="handled">Whether the message was handled.</param>
    /// <returns>Zero.</returns>
    private nint OnMessage(nint window, int message, nint wParam, nint lParam, ref bool handled) {
        handled = ProcessMessage(message, wParam, lParam);
        return 0;
    }

    /// <summary>
    ///     Processes one native input or DPI message.
    /// </summary>
    /// <param name="message">The native message identifier.</param>
    /// <param name="wParam">The native word parameter.</param>
    /// <param name="lParam">The native long parameter.</param>
    /// <returns><see langword="true" /> when native default handling must stop.</returns>
    internal bool ProcessMessage(int message, nint wParam, nint lParam) {
        switch (message) {
            case NativeMethods.WmDpiChanged:
                UpdateDpiScale((ushort) ((nuint) wParam & 0xffff) / 96.0);
                return false;
            case NativeMethods.WmMouseMove:
                RaiseMouse(HwndPointerAction.Move, HwndPointerButton.None, 0, lParam, false);
                return false;
            case NativeMethods.WmLeftButtonDown:
                RaiseMouse(HwndPointerAction.Pressed, HwndPointerButton.Left, 0, lParam, false);
                return false;
            case NativeMethods.WmLeftButtonUp:
                RaiseMouse(HwndPointerAction.Released, HwndPointerButton.Left, 0, lParam, false);
                return false;
            case NativeMethods.WmRightButtonDown:
                RaiseMouse(HwndPointerAction.Pressed, HwndPointerButton.Right, 0, lParam, false);
                return false;
            case NativeMethods.WmRightButtonUp:
                RaiseMouse(HwndPointerAction.Released, HwndPointerButton.Right, 0, lParam, false);
                return false;
            case NativeMethods.WmMiddleButtonDown:
                RaiseMouse(HwndPointerAction.Pressed, HwndPointerButton.Middle, 0, lParam, false);
                return false;
            case NativeMethods.WmMiddleButtonUp:
                RaiseMouse(HwndPointerAction.Released, HwndPointerButton.Middle, 0, lParam, false);
                return false;
            case NativeMethods.WmMouseWheel:
                RaiseMouse(HwndPointerAction.Wheel,
                    HwndPointerButton.None,
                    (short) (((nuint) wParam >> 16) & 0xffff),
                    lParam,
                    true);
                return false;
            case NativeMethods.WmTouch:
                ProcessTouch(wParam, lParam);
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    ///     Raises one decoded mouse event.
    /// </summary>
    /// <param name="action">The pointer action.</param>
    /// <param name="button">The changed button.</param>
    /// <param name="wheelDelta">The wheel delta.</param>
    /// <param name="lParam">The packed native position.</param>
    /// <param name="screenCoordinates">Whether the packed position is screen-relative.</param>
    private void RaiseMouse(
        HwndPointerAction action,
        HwndPointerButton button,
        int wheelDelta,
        nint lParam,
        bool screenCoordinates
    ) {
        var decodedPoint = DecodePoint(lParam);
        var point = new NativeMethods.NativePoint(decodedPoint.X, decodedPoint.Y);
        if (screenCoordinates && nativeHandle != 0) NativeMethods.ScreenToClient(nativeHandle, ref point);
        PointerInput?.Invoke(this,
            new HwndPointerEventArgs(HwndPointerDevice.Mouse,
                action,
                0,
                point.X / DpiScale,
                point.Y / DpiScale,
                button,
                wheelDelta));
    }

    /// <summary>
    ///     Reads, closes, and publishes all contacts in one native touch message.
    /// </summary>
    /// <param name="wParam">The contact count.</param>
    /// <param name="lParam">The touch input handle.</param>
    private void ProcessTouch(nint wParam, nint lParam) {
        var count = (uint) ((nuint) wParam & 0xffff);
        try {
            if (count == 0) return;

            var contacts = new NativeMethods.TouchInput[count];
            fixed (NativeMethods.TouchInput* contactPointer = contacts) {
                if (!NativeMethods.GetTouchInputInfo(lParam,
                        count,
                        contactPointer,
                        sizeof(NativeMethods.TouchInput)))
                    throw new Win32Exception(Marshal.GetLastPInvokeError(), "Could not read native touch input.");
            }

            foreach (var contact in contacts) {
                var point = new NativeMethods.NativePoint(contact.X / 100, contact.Y / 100);
                if (nativeHandle != 0) NativeMethods.ScreenToClient(nativeHandle, ref point);
                var action = (contact.Flags & NativeMethods.TouchEventDown) != 0
                    ? HwndPointerAction.Pressed
                    : (contact.Flags & NativeMethods.TouchEventUp) != 0
                        ? HwndPointerAction.Released
                        : HwndPointerAction.Move;
                PointerInput?.Invoke(this,
                    new HwndPointerEventArgs(HwndPointerDevice.Touch,
                        action,
                        contact.Id,
                        point.X / DpiScale,
                        point.Y / DpiScale,
                        HwndPointerButton.None,
                        0));
            }
        } finally {
            NativeMethods.CloseTouchInputHandle(lParam);
        }
    }

    /// <summary>
    ///     Decodes signed client coordinates from a native message parameter.
    /// </summary>
    /// <param name="value">The packed coordinate value.</param>
    /// <returns>The signed native point.</returns>
    internal static (int X, int Y) DecodePoint(nint value) => (
        (short) ((nuint) value & 0xffff),
        (short) (((nuint) value >> 16) & 0xffff));

    /// <summary>
    ///     Contains the minimal Win32 child-window operations required by <see cref="HwndHost" />.
    /// </summary>
    private static partial class NativeMethods {
        internal const int WmMouseMove = 0x0200;
        internal const int WmLeftButtonDown = 0x0201;
        internal const int WmLeftButtonUp = 0x0202;
        internal const int WmRightButtonDown = 0x0204;
        internal const int WmRightButtonUp = 0x0205;
        internal const int WmMiddleButtonDown = 0x0207;
        internal const int WmMiddleButtonUp = 0x0208;
        internal const int WmMouseWheel = 0x020A;
        internal const int WmTouch = 0x0240;
        internal const int WmDpiChanged = 0x02E0;
        internal const uint TouchEventDown = 0x0002;
        internal const uint TouchEventUp = 0x0004;

        /// <summary>
        ///     Window styles required for a clipped visible child window.
        /// </summary>
        internal const uint ChildWindowStyles = 0x40000000 | 0x10000000 | 0x02000000 | 0x04000000;

        /// <summary>
        ///     Creates the native child window.
        /// </summary>
        /// <returns>The created window handle, or zero on failure.</returns>
        [LibraryImport("user32.dll",
            EntryPoint = "CreateWindowExW",
            SetLastError = true,
            StringMarshalling = StringMarshalling.Utf16)]
        internal static partial nint CreateWindowEx(
            uint extendedStyle,
            string className,
            string? windowName,
            uint style,
            int x,
            int y,
            int width,
            int height,
            nint parent,
            nint menu,
            nint instance,
            nint parameter);

        /// <summary>
        ///     Destroys the native child window.
        /// </summary>
        /// <param name="window">The window handle.</param>
        /// <returns><see langword="true" /> on success.</returns>
        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool DestroyWindow(nint window);

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool RegisterTouchWindow(nint window, uint flags);

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool UnregisterTouchWindow(nint window);

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool GetTouchInputInfo(
            nint touchInput,
            uint inputCount,
            TouchInput* inputs,
            int inputSize);

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool CloseTouchInputHandle(nint touchInput);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool ScreenToClient(nint window, ref NativePoint point);

        [StructLayout(LayoutKind.Sequential)]
        internal readonly struct NativePoint(int x, int y) {
            internal readonly int X = x;
            internal readonly int Y = y;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal readonly struct TouchInput {
            internal readonly int X;
            internal readonly int Y;
            internal readonly nint Source;
            internal readonly uint Id;
            internal readonly uint Flags;
            internal readonly uint Mask;
            internal readonly uint Time;
            internal readonly nuint ExtraInfo;
            internal readonly uint ContactWidth;
            internal readonly uint ContactHeight;
        }
    }
}

/// <summary>
///     Identifies the native pointer device that produced an input event.
/// </summary>
public enum HwndPointerDevice {
    /// <summary>Mouse input.</summary>
    Mouse,

    /// <summary>Touch input.</summary>
    Touch
}

/// <summary>
///     Identifies the pointer action.
/// </summary>
public enum HwndPointerAction {
    /// <summary>The pointer moved.</summary>
    Move,

    /// <summary>A button or touch contact was pressed.</summary>
    Pressed,

    /// <summary>A button or touch contact was released.</summary>
    Released,

    /// <summary>The mouse wheel changed.</summary>
    Wheel
}

/// <summary>
///     Identifies the mouse button changed by a pointer event.
/// </summary>
public enum HwndPointerButton {
    /// <summary>No button changed.</summary>
    None,

    /// <summary>The left mouse button.</summary>
    Left,

    /// <summary>The right mouse button.</summary>
    Right,

    /// <summary>The middle mouse button.</summary>
    Middle
}

/// <summary>
///     Describes renderer-neutral pointer input in WPF device-independent coordinates.
/// </summary>
public sealed class HwndPointerEventArgs : EventArgs {
    /// <summary>
    ///     Creates pointer event data.
    /// </summary>
    /// <param name="device">The pointer device.</param>
    /// <param name="action">The pointer action.</param>
    /// <param name="pointerId">The contact identifier, or zero for mouse input.</param>
    /// <param name="x">The horizontal position in device-independent units.</param>
    /// <param name="y">The vertical position in device-independent units.</param>
    /// <param name="button">The changed mouse button.</param>
    /// <param name="wheelDelta">The mouse-wheel delta.</param>
    public HwndPointerEventArgs(
        HwndPointerDevice device,
        HwndPointerAction action,
        uint pointerId,
        double x,
        double y,
        HwndPointerButton button,
        int wheelDelta
    ) {
        Device = device;
        Action = action;
        PointerId = pointerId;
        X = x;
        Y = y;
        Button = button;
        WheelDelta = wheelDelta;
    }

    /// <summary>Gets the pointer device.</summary>
    public HwndPointerDevice Device { get; }

    /// <summary>Gets the pointer action.</summary>
    public HwndPointerAction Action { get; }

    /// <summary>Gets the touch contact identifier, or zero for mouse input.</summary>
    public uint PointerId { get; }

    /// <summary>Gets the horizontal position in device-independent units.</summary>
    public double X { get; }

    /// <summary>Gets the vertical position in device-independent units.</summary>
    public double Y { get; }

    /// <summary>Gets the changed mouse button.</summary>
    public HwndPointerButton Button { get; }

    /// <summary>Gets the mouse-wheel delta.</summary>
    public int WheelDelta { get; }
}

/// <summary>
///     Converts WPF device-independent dimensions into valid swap-chain pixel dimensions.
/// </summary>
internal static class D3D12PresentationSize {
    /// <summary>
    ///     Calculates a minimum-one-pixel physical size using ceiling so fractional pixels are not clipped.
    /// </summary>
    /// <param name="width">The width in device-independent units.</param>
    /// <param name="height">The height in device-independent units.</param>
    /// <param name="dpiScale">The physical-pixel scale.</param>
    /// <returns>The physical width and height.</returns>
    internal static (uint Width, uint Height) Calculate(double width, double height, double dpiScale) {
        if (!double.IsFinite(width) || width < 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (!double.IsFinite(height) || height < 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (!double.IsFinite(dpiScale) || dpiScale <= 0) throw new ArgumentOutOfRangeException(nameof(dpiScale));

        return (ToPixels(width, dpiScale), ToPixels(height, dpiScale));
    }

    /// <summary>
    ///     Converts one dimension to physical pixels.
    /// </summary>
    /// <param name="value">The device-independent dimension.</param>
    /// <param name="dpiScale">The physical-pixel scale.</param>
    /// <returns>The physical dimension.</returns>
    private static uint ToPixels(double value, double dpiScale) {
        var pixels = Math.Ceiling(value * dpiScale);
        if (pixels > uint.MaxValue) throw new ArgumentOutOfRangeException(nameof(value));
        return Math.Max(1, (uint) pixels);
    }
}

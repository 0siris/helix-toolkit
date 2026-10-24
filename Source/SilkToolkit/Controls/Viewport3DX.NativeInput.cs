using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace HelixToolkit.Wpf.SharpDX.Controls;

/// <summary>Adapts native child-window mouse input to the viewport's existing WPF input contracts.</summary>
public partial class Viewport3DX {
    /// The authoritative native position while WPF processes one mouse event and its promoted counterpart.
    private Point? nativeMousePosition;

    /// <summary>Gets the current native event position or the ordinary WPF mouse position.</summary>
    /// <param name="args">The original WPF event, when available.</param>
    /// <returns>The pointer position in viewport device-independent coordinates.</returns>
    /// <remarks>Copy this value during routed dispatch; native coordinates are restored when dispatch finishes.</remarks>
    public Point GetMouseInputPosition(MouseEventArgs? args = null) => nativeMousePosition
        ?? args?.GetPosition(this) ?? Mouse.GetPosition(this);

    /// <summary>
    ///     Reads a logical mouse button from WPF or the live native child's current physical input state.
    /// </summary>
    /// <param name="button">The logical mouse button to read.</param>
    /// <param name="args">The original WPF event whose device supplies the ordinary fallback, when available.</param>
    /// <returns>The current button state; native polling uses only the asynchronous high bit.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The button is not one of the five supported mouse buttons.</exception>
    /// <exception cref="InvalidOperationException">The caller does not own this viewport's dispatcher.</exception>
    /// <remarks>
    ///     Viewport disposal or an active WPF device retains the ordinary event-device or primary-device value.
    ///     An absent, detached, uninitialized, unloaded or invisible native child also retains that value.
    ///     Otherwise this reads current OS state, not an event snapshot, and respects swapped primary buttons.
    ///     A failed OS read yields Released. This getter grants no foreground, focus, capture or input ownership;
    ///     callers must retain their actual ownership guards.
    /// </remarks>
    public MouseButtonState GetMouseInputButtonState(MouseButton button, MouseEventArgs? args = null) {
        Dispatcher.VerifyAccess();
        var device = args?.MouseDevice ?? Mouse.PrimaryDevice;
        var (state, virtualKey) = button switch {
            MouseButton.Left => (device.LeftButton, 0x01),
            MouseButton.Right => (device.RightButton, 0x02),
            MouseButton.Middle => (device.MiddleButton, 0x04),
            MouseButton.XButton1 => (device.XButton1, 0x05),
            MouseButton.XButton2 => (device.XButton2, 0x06),
            _ => throw new ArgumentOutOfRangeException(nameof(button), button, "Unsupported mouse button")
        };
        if (IsDisposed || device.ActiveSource is not null || !IsLoaded || !IsVisible
            || d3d12Surface is not { IsInitialized: true, IsDisposed: false } surface
            || !ReferenceEquals(hostPresenter?.Content, surface)
            || surface.WindowHost is not { IsHandleCreated: true, IsLoaded: true, IsVisible: true }
            || !ReferenceEquals(surface.WindowHost.Parent, surface))
            return state;

        // GetAsyncKeyState reads physical buttons; WPF and native button messages use logical buttons.
        if ((button is MouseButton.Left or MouseButton.Right) && GetSystemMetrics(23) != 0)
            virtualKey = button == MouseButton.Left ? 0x02 : 0x01;
        return (GetAsyncKeyState(virtualKey) & 0x8000) != 0
            ? MouseButtonState.Pressed : MouseButtonState.Released;
    }

    /// <summary>Reads the current physical key state without changing WPF input or capture.</summary>
    /// <param name="virtualKey">The physical button's Win32 virtual-key code.</param>
    /// <returns>The asynchronous state bits; only the high bit establishes a currently held button.</returns>
    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern short GetAsyncKeyState(int virtualKey);

    /// <summary>Reads the system's current physical-to-logical primary button mapping.</summary>
    /// <param name="index">The system metric identifier; 23 denotes SM_SWAPBUTTON.</param>
    /// <returns>A nonzero value when the primary mouse buttons are swapped.</returns>
    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern int GetSystemMetrics(int index);

    /// <summary>Runs native mouse input through WPF so existing tools and camera commands share one route.</summary>
    /// <param name="pointer">The decoded child-window event in device-independent coordinates.</param>
    /// <exception cref="ArgumentOutOfRangeException">The native action or pressed button is unsupported.</exception>
    private void ProcessNativeMouseInput(HwndPointerEventArgs pointer) {
        var timestamp = Environment.TickCount;
        MouseEventArgs input;
        RoutedEvent promoted;
        switch (pointer.Action) {
            case HwndPointerAction.Pressed:
            case HwndPointerAction.Released:
                var button = pointer.Button switch {
                    HwndPointerButton.Left => MouseButton.Left,
                    HwndPointerButton.Right => MouseButton.Right,
                    HwndPointerButton.Middle => MouseButton.Middle,
                    _ => throw new ArgumentOutOfRangeException(nameof(pointer), pointer.Button,
                        "A native press or release requires a mouse button")
                };
                input = new MouseButtonEventArgs(Mouse.PrimaryDevice, timestamp, button) {
                    RoutedEvent = pointer.Action == HwndPointerAction.Pressed
                        ? Mouse.PreviewMouseDownEvent : Mouse.PreviewMouseUpEvent
                };
                promoted = pointer.Action == HwndPointerAction.Pressed ? Mouse.MouseDownEvent : Mouse.MouseUpEvent;
                break;
            case HwndPointerAction.Move:
                input = new MouseEventArgs(Mouse.PrimaryDevice, timestamp) { RoutedEvent = Mouse.PreviewMouseMoveEvent };
                promoted = Mouse.MouseMoveEvent;
                break;
            case HwndPointerAction.Wheel:
                input = new MouseWheelEventArgs(Mouse.PrimaryDevice, timestamp, pointer.WheelDelta) {
                    RoutedEvent = Mouse.PreviewMouseWheelEvent
                };
                promoted = Mouse.MouseWheelEvent;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(pointer), pointer.Action, "Unsupported native mouse action");
        }
        input.Source = this;
        var manager = InputManager.Current;
        PreProcessInputEventHandler retainSource = (_, args) => {
            var next = args.StagingItem.Input;
            if (next.Source is null && ReferenceEquals(next.Device, input.Device)
                && next.Timestamp == timestamp && next.RoutedEvent == promoted)
                next.Source = this;
        };
        var previousPosition = nativeMousePosition;
        nativeMousePosition = new Point(pointer.X, pointer.Y);
        manager.PreProcessInput += retainSource;
        try {
            manager.ProcessInput(input);
        } finally {
            manager.PreProcessInput -= retainSource;
            nativeMousePosition = previousPosition;
        }
    }
}

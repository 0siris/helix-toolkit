using System;
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
    internal Point GetMouseInputPosition(MouseEventArgs? args = null) => nativeMousePosition
        ?? args?.GetPosition(this) ?? Mouse.GetPosition(this);

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

using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;

namespace HelixToolkit.Wpf.SharpDX.Controls;
public class WinformHostExtend : WindowsFormsHost {
    public delegate void FormMouseMoveEventHandler(object sender, FormMouseMoveEventArgs e);

    public delegate void FormMouseWheelEventHandler(object sender, FormMouseWheelEventArgs e);

    public static readonly RoutedEvent FormMouseMoveEvent =
        EventManager.RegisterRoutedEvent("FormMouseMove",
                                         RoutingStrategy.Bubble,
                                         typeof(FormMouseMoveEventHandler),
                                         typeof(WinformHostExtend));

    public static readonly RoutedEvent FormMouseWheelEvent =
        EventManager.RegisterRoutedEvent("FormMouseWheel",
                                         RoutingStrategy.Bubble,
                                         typeof(FormMouseWheelEventHandler),
                                         typeof(WinformHostExtend));

    public WinformHostExtend() {
        ChildChanged += OnChildChanged;
    }

    protected UIElement ParentControl { get; set; }

    public double DpiScale {
        get;
        set {
            if (field == value) return;
            field = value;
            DpiScaleChanged?.Invoke(this, value);
        }
    } = 1;

    public event FormMouseMoveEventHandler FormMouseMove {
        add => AddHandler(FormMouseMoveEvent, value);
        remove => RemoveHandler(FormMouseMoveEvent, value);
    }

    public event FormMouseWheelEventHandler FormMouseWheel {
        add => AddHandler(FormMouseWheelEvent, value);
        remove => RemoveHandler(FormMouseWheelEvent, value);
    }

    public event EventHandler<double> DpiScaleChanged;

    private void OnChildChanged(object sender, ChildChangedEventArgs childChangedEventArgs) {
        var previousChild = childChangedEventArgs.PreviousChild as Control;
        if (previousChild != null) {
            previousChild.MouseDown -= OnMouseDown;
            previousChild.MouseWheel -= OnMouseWheel;
            previousChild.MouseMove -= OnMouseMove;
            previousChild.MouseUp -= OnMouseUp;
        }

        if (Child != null) {
            Child.MouseDown += OnMouseDown;
            Child.MouseWheel += OnMouseWheel;
            Child.MouseMove += OnMouseMove;
            Child.MouseUp += OnMouseUp;
        }
    }

    private void OnMouseMove(object sender, MouseEventArgs e) {
        RaiseEvent(new FormMouseMoveEventArgs(FormMouseMoveEvent,
                                              new Point(e.Location.X / DpiScale, e.Location.Y / DpiScale),
                                              e.X,
                                              e.Y,
                                              e.Delta) { Source = this });
    }

    private void OnMouseWheel(object sender, MouseEventArgs e) {
        RaiseEvent(new FormMouseWheelEventArgs(FormMouseWheelEvent,
                                               Mouse.PrimaryDevice,
                                               Environment.TickCount,
                                               e.Delta) {
            Source = this
        });
    }

    private void OnMouseDown(object sender, MouseEventArgs mouseEventArgs) {
        var wpfButton = ConvertToWpf(mouseEventArgs.Button);
        if (!wpfButton.HasValue)
            return;
        Capture();
        RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, wpfButton.Value) {
            RoutedEvent = Mouse.PreviewMouseDownEvent,
            Source = this
        });

        RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, wpfButton.Value) {
            RoutedEvent = Mouse.MouseDownEvent,
            Source = this
        });
    }

    private void OnMouseUp(object sender, MouseEventArgs mouseEventArgs) {
        var wpfButton = ConvertToWpf(mouseEventArgs.Button);
        if (!wpfButton.HasValue)
            return;
        Capture();
        RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, wpfButton.Value) {
            RoutedEvent = Mouse.MouseUpEvent,
            Source = this
        });
    }

    /// <summary>
    ///     Has to do this, otherwise the mouse point is wrong in mouse event.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Capture() {
        CaptureMouse();
        ReleaseMouseCapture();
    }

    private MouseButton? ConvertToWpf(MouseButtons winformButton) {
        switch (winformButton) {
            case MouseButtons.Left:
                return MouseButton.Left;
            case MouseButtons.None:
                return null;
            case MouseButtons.Right:
                return MouseButton.Right;
            case MouseButtons.Middle:
                return MouseButton.Middle;
            case MouseButtons.XButton1:
                return MouseButton.XButton1;
            case MouseButtons.XButton2:
                return MouseButton.XButton2;
            default:
                return null;
        }
    }

    /// <summary>
    /// </summary>
    /// <seealso cref="System.Windows.RoutedEventArgs" />
    public sealed class FormMouseMoveEventArgs : RoutedEventArgs {
        /// <summary>
        ///     Initializes a new instance of the <see cref="FormMouseMoveEventArgs" /> class.
        /// </summary>
        /// <param name="routedEvent">The routed event.</param>
        /// <param name="p">The p.</param>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="delta">The delta.</param>
        public FormMouseMoveEventArgs(RoutedEvent routedEvent, Point p, int x, int y, int delta)
            : base(routedEvent) {
            Location = p;
            X = x;
            Y = y;
            Delta = delta;
        }

        //
        // Summary:
        //     Gets a signed count of the number of detents the mouse wheel has rotated, multiplied
        //     by the WHEEL_DELTA constant. A detent is one notch of the mouse wheel.
        //
        // Returns:
        //     A signed count of the number of detents the mouse wheel has rotated, multiplied
        //     by the WHEEL_DELTA constant.
        public int Delta { get; private set; }

        //
        // Summary:
        //     Gets the location of the mouse during the generating mouse event.
        //
        // Returns:
        //     A System.Drawing.Point that contains the x- and y- mouse coordinates, in pixels,
        //     relative to the upper-left corner of the form.
        public Point Location { get; private set; }

        //
        // Summary:
        //     Gets the x-coordinate of the mouse during the generating mouse event.
        //
        // Returns:
        //     The x-coordinate of the mouse, in pixels.
        public int X { get; private set; }

        //
        // Summary:
        //     Gets the y-coordinate of the mouse during the generating mouse event.
        //
        // Returns:
        //     The y-coordinate of the mouse, in pixels.
        public int Y { get; private set; }
    }

    /// <summary>
    /// </summary>
    /// <seealso cref="System.Windows.Input.MouseWheelEventArgs" />
    public sealed class FormMouseWheelEventArgs : RoutedEventArgs {
        public readonly int Delta;
        public readonly MouseDevice Mouse;
        public readonly int Timestamp;

        /// <summary>
        ///     Initializes a new instance of the <see cref="FormMouseWheelEventArgs" /> class.
        /// </summary>
        /// <param name="routedEvent"></param>
        /// <param name="mouse">The mouse device associated with this event.</param>
        /// <param name="timestamp">The time when the input occurred.</param>
        /// <param name="delta">The amount the wheel has changed.</param>
        public FormMouseWheelEventArgs(RoutedEvent routedEvent, MouseDevice mouse, int timestamp, int delta) :
            base(routedEvent) {
            Delta = delta;
            Timestamp = timestamp;
            Mouse = mouse;
        }

        public static implicit operator MouseWheelEventArgs(FormMouseWheelEventArgs args) {
            return new MouseWheelEventArgs(args.Mouse, args.Timestamp, args.Delta) { RoutedEvent = MouseWheelEvent };
        }
    }
}

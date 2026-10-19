using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Xunit;

namespace SilkToolkit.Tests;

/// <summary>Provides the serial, explicitly selected Phase 4 visible acceptance over actual productive D3D12 output.</summary>
/// <param name="output">Receives native counters, desktop pixels and WPF snapshot measurements.</param>
[Collection(WpfCollection.Name)]
public sealed class P4VisibleCompositionTests(ITestOutputHelper output) {
    /// <summary>Shows one production-template viewport and verifies real controls, ZIndex, clip, brush, blur and adorners.</summary>
    /// <returns>The explicit hardware acceptance, with bounded waits and unconditional window cleanup.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    [Trait("Category", "Wpf")]
    public Task ProductionViewportComposesWithControlsBrushEffectsAndAdorners() => StaThread.RunAsync(async () => {
        var harness = new P4WpfHarness(hiddenSource: false);
        var root = harness.Root;
        root.Width = 380;
        root.Height = 200;
        root.Background = Brushes.Magenta;
        var viewport = harness.Viewport;
        viewport.HorizontalAlignment = HorizontalAlignment.Left;
        viewport.VerticalAlignment = VerticalAlignment.Top;
        viewport.Margin = new Thickness(20, 20, 0, 0);
        var decorator = new AdornerDecorator { Child = root };
        var window = new Window {
            Title = "Phase 4 productive WPF composition acceptance",
            Content = decorator, SizeToContent = SizeToContent.WidthAndHeight,
            WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.NoResize,
            Left = 100, Top = 100, Topmost = true, ShowInTaskbar = false,
            Background = Brushes.Magenta
        };
        var button = new Button {
            Width = 64, Height = 32, Margin = new Thickness(60, 50, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
            Template = ButtonTemplate()
        };
        Panel.SetZIndex(button, 10);
        root.Children.Add(button);
        var clickCount = 0;
        button.Click += (_, _) => clickCount++;
        AdornerLayer? layer = null;
        SolidPatchAdorner? adorner = null;
        var savedCursor = new NativePoint();
        var restoreCursor = GetCursorPos(ref savedCursor);
        try {
            window.Show();
            window.Activate();
            window.UpdateLayout();
            await harness.WarmAsync();
            Assert.False(viewport.EnableSwapChainRendering);
            Assert.Null(viewport.D3D12Surface);
            Assert.True(harness.Owner.IsInitialized);
            Assert.NotEqual(nint.Zero, harness.Canvas.ImageSource.BoundSurface);
            Assert.True(harness.Owner.CompletedFrames > 0);
            Assert.True(harness.Owner.CopiedFrames > 0);
            Assert.True(harness.Owner.PublishedFrames > 0);
            var initial = P4WpfHarness.Snapshot(harness.Canvas, 160, 120);
            P4WpfHarness.AssertPixel(initial, 160, 80, 60, Colors.Red);
            P4WpfHarness.AssertPixel(initial, 160, 8, 8, Colors.Black);
            output.WriteLine($"Production GPU image: {harness.Counters}; surface=0x{harness.Canvas.ImageSource.BoundSurface:X}.");

            // Read the actual desktop as well as WPF's D3DImage software snapshot; neither feeds presentation.
            var buttonPoint = new Point(92, 66);
            await AssertDesktopAsync(window, root, buttonPoint, Colors.Lime, "WPF Button above GPU");
            AssertSnapshot(decorator, buttonPoint, Colors.Lime, "WPF Button above GPU");
            var screen = root.PointToScreen(buttonPoint);
            var nativePoint = new NativePoint { X = (int) Math.Round(screen.X), Y = (int) Math.Round(screen.Y) };
            var handle = new WindowInteropHelper(window).Handle;
            Assert.Equal(handle, WindowFromPoint(nativePoint));
            Assert.True(SetCursorPos(nativePoint.X, nativePoint.Y));
            Assert.True(ScreenToClient(handle, ref nativePoint));
            var packed = (nint) ((nativePoint.X & 0xffff) | ((nativePoint.Y & 0xffff) << 16));
            _ = SendMessage(handle, 0x0200, 0, packed);
            _ = SendMessage(handle, 0x0201, 1, packed);
            _ = SendMessage(handle, 0x0202, 0, packed);
            await WaitAsync(() => clickCount == 1, "native WPF Button click");
            Assert.Equal(1, clickCount);
            Assert.True(button.IsLoaded);
            output.WriteLine($"Real WM_MOUSE click count={clickCount}; WindowFromPoint=0x{handle:X}; no Popup or child HWND.");

            Panel.SetZIndex(button, -1);
            window.UpdateLayout();
            await AssertDesktopAsync(window, root, buttonPoint, Colors.Red, "GPU above lowered Button");
            AssertSnapshot(decorator, buttonPoint, Colors.Red, "GPU above lowered Button");
            Panel.SetZIndex(button, 10);
            window.UpdateLayout();
            await AssertDesktopAsync(window, root, buttonPoint, Colors.Lime, "restored Button ZIndex");
            button.Visibility = Visibility.Collapsed;

            var clippedPoint = new Point(150, 110);
            AssertSnapshot(decorator, clippedPoint, Colors.Black, "GPU before clipping");
            viewport.Clip = new RectangleGeometry(new Rect(0, 0, 80, 120));
            window.UpdateLayout();
            AssertSnapshot(decorator, clippedPoint, Colors.Magenta, "parent revealed by GPU clipping");
            await AssertDesktopAsync(window, root, clippedPoint, Colors.Magenta, "parent revealed by GPU clipping");
            viewport.Clip = null;

            var brushPreview = new Border {
                Width = 160, Height = 120, Margin = new Thickness(200, 20, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
                Background = new VisualBrush(viewport) { Stretch = Stretch.Fill }
            };
            root.Children.Add(brushPreview);
            window.UpdateLayout();
            AssertSnapshot(decorator, new Point(280, 80), Colors.Red, "actual VisualBrush GPU mesh");
            AssertSnapshot(decorator, new Point(208, 28), Colors.Black, "actual VisualBrush GPU background");
            await AssertDesktopAsync(window, root, new Point(280, 80), Colors.Red, "actual VisualBrush GPU mesh");

            var sharp = P4WpfHarness.Snapshot(harness.Canvas, 160, 120);
            var boundary = Enumerable.Range(1, 79).First(x => sharp[(60 * 160 + x) * 4 + 2] > 200);
            var sampleX = boundary - 3;
            Assert.True(sampleX > 0);
            var sharpRed = sharp[(60 * 160 + sampleX) * 4 + 2];
            Assert.InRange(sharpRed, 0, 8);
            viewport.Effect = new BlurEffect { Radius = 8, KernelType = KernelType.Gaussian };
            window.UpdateLayout();
            var blurred = P4WpfHarness.Snapshot(decorator, 380, 200);
            var blurredRed = blurred[((60 + 20) * 380 + sampleX + 20) * 4 + 2];
            Assert.InRange(blurredRed, 9, 220);
            output.WriteLine($"Blur of actual GPU mesh edge at DIP ({sampleX},60): sharp R={sharpRed}, blurred R={blurredRed}.");
            viewport.Effect = null;

            layer = Assert.IsType<AdornerLayer>(AdornerLayer.GetAdornerLayer(viewport));
            adorner = new SolidPatchAdorner(viewport);
            layer.Add(adorner);
            window.UpdateLayout();
            Assert.Contains(adorner, layer.GetAdorners(viewport)!);
            AssertSnapshot(decorator, new Point(60, 60), Colors.Cyan, "actual AdornerLayer over GPU");
            await AssertDesktopAsync(window, root, new Point(60, 60), Colors.Cyan, "actual AdornerLayer over GPU");
            layer.Remove(adorner);
            adorner = null;
            window.UpdateLayout();
            Assert.True(layer.GetAdorners(viewport) is not { Length: > 0 });
            harness.AssertIdle();
            Assert.Equal(1, clickCount);
            output.WriteLine($"Visible acceptance completed; {harness.Counters}; control clicks={clickCount}.");
        } finally {
            if (layer is not null && adorner is not null) layer.Remove(adorner);
            viewport.Effect = null;
            if (restoreCursor) _ = SetCursorPos(savedCursor.X, savedCursor.Y);
            try {
                await harness.DisposeAsync();
            } finally {
                window.Content = null;
                window.Close();
            }
        }
    });

    /// <summary>Creates a solid button face so visual assertions do not depend on theme, hover or glyph rasterization.</summary>
    /// <returns>A normal WPF Button template without a Popup.</returns>
    private static ControlTemplate ButtonTemplate() {
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.BackgroundProperty, Brushes.Lime);
        return new ControlTemplate(typeof(Button)) { VisualTree = border };
    }

    /// <summary>Asserts and reports one actual composed WPF pixel.</summary>
    /// <param name="visual">The visual including its actual AdornerLayer.</param>
    /// <param name="point">The DIP coordinate at 96-DPI diagnostic resolution.</param>
    /// <param name="expected">The expected opaque composed color.</param>
    /// <param name="operation">The recorded acceptance step.</param>
    private void AssertSnapshot(Visual visual, Point point, Color expected, string operation) {
        var pixels = P4WpfHarness.Snapshot(visual, 380, 200);
        var offset = ((int) point.Y * 380 + (int) point.X) * 4;
        output.WriteLine($"{operation}: WPF BGRA=({pixels[offset]},{pixels[offset + 1]}," +
            $"{pixels[offset + 2]},{pixels[offset + 3]}) at {point}.");
        P4WpfHarness.AssertPixel(pixels, 380, (int) point.X, (int) point.Y, expected);
    }

    /// <summary>Waits for actual desktop composition and rejects obscured or unexpected child-window measurements.</summary>
    /// <param name="window">The visible acceptance Window.</param>
    /// <param name="root">The visual whose DIP coordinates identify the pixel.</param>
    /// <param name="point">The local sample coordinate.</param>
    /// <param name="expected">The expected desktop color.</param>
    /// <param name="operation">The recorded acceptance step.</param>
    /// <returns>The bounded compositor wait.</returns>
    private async Task AssertDesktopAsync(Window window, FrameworkElement root, Point point, Color expected, string operation) {
        uint pixel = uint.MaxValue;
        await WaitAsync(() => {
            var screen = root.PointToScreen(point);
            var nativePoint = new NativePoint { X = (int) Math.Round(screen.X), Y = (int) Math.Round(screen.Y) };
            Assert.Equal(new WindowInteropHelper(window).Handle, WindowFromPoint(nativePoint));
            var dc = GetDC(0);
            Assert.NotEqual(nint.Zero, dc);
            try {
                pixel = GetPixel(dc, nativePoint.X, nativePoint.Y);
            } finally {
                _ = ReleaseDC(0, dc);
            }
            return pixel != uint.MaxValue && Math.Abs((int) (pixel & 255) - expected.R) <= 24 &&
                Math.Abs((int) ((pixel >> 8) & 255) - expected.G) <= 24 &&
                Math.Abs((int) ((pixel >> 16) & 255) - expected.B) <= 24;
        }, operation);
        output.WriteLine($"{operation}: desktop RGB=({pixel & 255},{(pixel >> 8) & 255}," +
            $"{(pixel >> 16) & 255}) at DIP {point}.");
    }

    /// <summary>Bounds test-only input/compositor waits without blocking the dispatcher.</summary>
    /// <param name="completed">The observed condition.</param>
    /// <param name="operation">The operation included in timeout diagnostics.</param>
    /// <returns>The bounded asynchronous wait.</returns>
    /// <exception cref="TimeoutException">The requested observation does not occur within three seconds.</exception>
    private static async Task WaitAsync(Func<bool> completed, string operation) {
        var start = Stopwatch.GetTimestamp();
        while (!completed()) {
            if (Stopwatch.GetElapsedTime(start) >= TimeSpan.FromSeconds(3))
                throw new TimeoutException($"{operation} did not complete within three seconds.");
            await Task.Delay(20);
        }
    }

    /// <summary>Draws a real WPF adorner, leaving the underlying D3DImage and presentation pixels untouched.</summary>
    /// <param name="element">The genuinely GPU-backed public viewport.</param>
    private sealed class SolidPatchAdorner(UIElement element) : Adorner(element) {
        /// <inheritdoc />
        protected override void OnRender(DrawingContext drawingContext) =>
            drawingContext.DrawRectangle(Brushes.Cyan, null, new Rect(30, 30, 24, 24));
    }

    /// <summary>Stores one physical screen or client coordinate for native mouse and desktop observations.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint {
        /// <summary>The horizontal physical pixel coordinate.</summary>
        internal int X;
        /// <summary>The vertical physical pixel coordinate.</summary>
        internal int Y;
    }

    /// <summary>Reads the cursor location so the visible acceptance can restore it.</summary>
    /// <param name="point">Receives the original physical screen coordinate.</param>
    /// <returns>Whether the coordinate was read.</returns>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(ref NativePoint point);

    /// <summary>Moves the actual cursor for native Button input delivery.</summary>
    /// <param name="x">The physical screen column.</param>
    /// <param name="y">The physical screen row.</param>
    /// <returns>Whether the cursor was moved.</returns>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetCursorPos(int x, int y);

    /// <summary>Converts a physical screen point into the actual acceptance HWND client coordinate system.</summary>
    /// <param name="window">The real acceptance HWND.</param>
    /// <param name="point">The screen coordinate, replaced with the client coordinate.</param>
    /// <returns>Whether the conversion succeeded.</returns>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ScreenToClient(nint window, ref NativePoint point);

    /// <summary>Sends ordinary native mouse messages to the public WPF Window without raising a synthetic Click.</summary>
    /// <param name="window">The actual WPF Window HWND.</param>
    /// <param name="message">The native mouse message.</param>
    /// <param name="wParam">The native button flags.</param>
    /// <param name="lParam">The packed physical client coordinates.</param>
    /// <returns>The native message result.</returns>
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint SendMessage(nint window, uint message, nint wParam, nint lParam);

    /// <summary>Acquires the desktop DC only for reading already composed pixels.</summary>
    /// <param name="window">Zero selects the desktop.</param>
    /// <returns>The borrowed diagnostic DC.</returns>
    [DllImport("user32.dll")]
    private static extern nint GetDC(nint window);

    /// <summary>Releases the borrowed desktop diagnostic DC.</summary>
    /// <param name="window">The zero HWND used to acquire the DC.</param>
    /// <param name="dc">The borrowed DC.</param>
    /// <returns>The native release result.</returns>
    [DllImport("user32.dll")]
    private static extern int ReleaseDC(nint window, nint dc);

    /// <summary>Reads a desktop pixel without supplying CPU data to the GPU presentation.</summary>
    /// <param name="dc">The diagnostic desktop DC.</param>
    /// <param name="x">The physical screen column.</param>
    /// <param name="y">The physical screen row.</param>
    /// <returns>The desktop COLORREF or uint.MaxValue on failure.</returns>
    [DllImport("gdi32.dll")]
    private static extern uint GetPixel(nint dc, int x, int y);

    /// <summary>Rejects occluded measurements and any native child HWND bypassing WPF composition.</summary>
    /// <param name="point">The physical sample coordinate.</param>
    /// <returns>The native window at that coordinate.</returns>
    [DllImport("user32.dll")]
    private static extern nint WindowFromPoint(NativePoint point);
}

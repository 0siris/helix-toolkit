using HelixToolkit.Wpf.SharpDX;
using System.Windows;
using System.Windows.Input;
using HelixToolkit.Wpf.SharpDX.Controls;
using Xunit;

namespace SilkToolkit.Tests;

/// <summary>Verifies native viewport input retains the standard WPF callbacks used by existing desktop tools.</summary>
[Collection(WpfCollection.Name)]
public sealed class NativePointerRoutedEventsTests {
    /// <summary>Preserves decoded touch DIP positions and primary-contact ownership independently of render scaling.</summary>
    /// <param name="enableDpiScale">Whether render resolution follows the viewport DPI.</param>
    /// <returns>The managed native input-route verification.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task NativeTouchKeepsDipCoordinatesAndSinglePrimaryContact(bool enableDpiScale) => StaThread.RunAsync(() => {
            using var viewport = new Viewport3DX { EnableSwapChainRendering = true, EnableDpiScale = enableDpiScale };
            viewport.RenderHost.DpiScale = 2;
            var downs = new List<Point>();
            var moves = new List<Point>();
            var ups = new List<Point>();
            var mouseDowns = 0;
            viewport.MouseDown3D += (_, args) => downs.Add(((MouseDown3DEventArgs) args).Position);
            viewport.MouseMove3D += (_, args) => moves.Add(((MouseMove3DEventArgs) args).Position);
            viewport.MouseUp3D += (_, args) => ups.Add(((MouseUp3DEventArgs) args).Position);
            viewport.MouseDown += (_, _) => mouseDowns++;
            viewport.ProcessD3D12Pointer(
                new HwndPointerEventArgs(
                    HwndPointerDevice.Touch,
                    HwndPointerAction.Pressed, 7, 12.5, 14.25, HwndPointerButton.None, 0
                ), ModifierKeys.None
            );
            viewport.ProcessD3D12Pointer(
                new HwndPointerEventArgs(
                    HwndPointerDevice.Touch,
                    HwndPointerAction.Pressed, 8, 80, 90, HwndPointerButton.None, 0
                ), ModifierKeys.None
            );
            viewport.ProcessD3D12Pointer(
                new HwndPointerEventArgs(
                    HwndPointerDevice.Touch,
                    HwndPointerAction.Move, 8, 81, 91, HwndPointerButton.None, 0
                ), ModifierKeys.None
            );
            viewport.ProcessD3D12Pointer(
                new HwndPointerEventArgs(
                    HwndPointerDevice.Touch,
                    HwndPointerAction.Released, 8, 81, 91, HwndPointerButton.None, 0
                ), ModifierKeys.None
            );
            viewport.ProcessD3D12Pointer(
                new HwndPointerEventArgs(
                    HwndPointerDevice.Touch,
                    HwndPointerAction.Move, 7, -2.5, 18.75, HwndPointerButton.None, 0
                ), ModifierKeys.None
            );
            viewport.ProcessD3D12Pointer(
                new HwndPointerEventArgs(
                    HwndPointerDevice.Touch,
                    HwndPointerAction.Released, 7, -2.5, 18.75, HwndPointerButton.None, 0
                ), ModifierKeys.None
            );
            viewport.ProcessD3D12Pointer(
                new HwndPointerEventArgs(
                    HwndPointerDevice.Touch,
                    HwndPointerAction.Pressed, 8, 20.5, 24.25, HwndPointerButton.None, 0
                ), ModifierKeys.None
            );
            viewport.ProcessD3D12Pointer(
                new HwndPointerEventArgs(
                    HwndPointerDevice.Touch,
                    HwndPointerAction.Released, 8, 20.5, 24.25, HwndPointerButton.None, 0
                ), ModifierKeys.None
            );

            Assert.Equal([new Point(12.5, 14.25), new Point(20.5, 24.25)], downs);
            Assert.Equal([new Point(-2.5, 18.75)], moves);
            Assert.Equal([new Point(-2.5, 18.75), new Point(20.5, 24.25)], ups);
            Assert.Equal(0, mouseDowns);
            Assert.Equal(
                enableDpiScale
                    ? 2f
                    : 1f, viewport.RenderHost.DpiScale
            );
        }
    );

    /// <summary>Delivers one typed preview/bubble pair and one original-coordinate 3D event for a native click.</summary>
    /// <returns>The completion of the owning dispatcher assertions.</returns>
    [Fact]
    public Task NativeClickMoveReleaseRetainsWpfCallbacksTest() => StaThread.RunAsync(() => {
            using var viewport = new Viewport3DX { EnableSwapChainRendering = true };
            var events = new List<string>();
            MouseDown3DEventArgs? hit = null;
            viewport.PreviewMouseDown += (_, _) => events.Add("preview");
            viewport.MouseDown += (_, _) => events.Add("down");
            viewport.MouseLeftButtonDown += (_, _) => events.Add("left");
            viewport.MouseMove += (_, _) => events.Add("move");
            viewport.MouseUp += (_, _) => events.Add("up");
            viewport.MouseDown3D += (_, args) => hit = (MouseDown3DEventArgs) args;

            viewport.ProcessD3D12Pointer(
                new HwndPointerEventArgs(
                    HwndPointerDevice.Mouse,
                    HwndPointerAction.Pressed, 0, 12, 14, HwndPointerButton.Left, 0
                ), ModifierKeys.None
            );
            viewport.ProcessD3D12Pointer(
                new HwndPointerEventArgs(
                    HwndPointerDevice.Mouse,
                    HwndPointerAction.Move, 0, 16, 18, HwndPointerButton.None, 0
                ), ModifierKeys.None
            );
            viewport.ProcessD3D12Pointer(
                new HwndPointerEventArgs(
                    HwndPointerDevice.Mouse,
                    HwndPointerAction.Released, 0, 16, 18, HwndPointerButton.Left, 0
                ), ModifierKeys.None
            );

            Assert.Equal(["preview", "left", "down", "move", "up"], events);
            Assert.NotNull(hit);
            Assert.Equal(new Point(12, 14), hit.Position);
            var button = Assert.IsType<MouseButtonEventArgs>(hit.OriginalInputEventArgs);
            Assert.Equal(MouseButton.Left, button.ChangedButton);
        }
    );
}
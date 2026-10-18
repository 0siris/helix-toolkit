using HelixToolkit.Wpf.SharpDX;
using System.Windows;
using System.Windows.Input;
using HelixToolkit.Wpf.SharpDX.Controls;
using Xunit;

namespace SilkToolkit.Tests;

/// <summary>Verifies native viewport input retains the standard WPF callbacks used by existing desktop tools.</summary>
[Collection(WpfCollection.Name)]
public sealed class NativePointerRoutedEventsTests {
    /// <summary>Delivers one typed preview/bubble pair and one original-coordinate 3D event for a native click.</summary>
    /// <returns>The completion of the owning dispatcher assertions.</returns>
    [Fact]
    public Task NativeClickMoveReleaseRetainsWpfCallbacksTest() => StaThread.RunAsync(() => {
        using var viewport = new Viewport3DX();
        var events = new List<string>();
        MouseDown3DEventArgs? hit = null;
        viewport.PreviewMouseDown += (_, _) => events.Add("preview");
        viewport.MouseDown += (_, _) => events.Add("down");
        viewport.MouseLeftButtonDown += (_, _) => events.Add("left");
        viewport.MouseMove += (_, _) => events.Add("move");
        viewport.MouseUp += (_, _) => events.Add("up");
        viewport.MouseDown3D += (_, args) => hit = (MouseDown3DEventArgs) args;

        viewport.ProcessD3D12Pointer(new HwndPointerEventArgs(HwndPointerDevice.Mouse,
            HwndPointerAction.Pressed, 0, 12, 14, HwndPointerButton.Left, 0), ModifierKeys.None);
        viewport.ProcessD3D12Pointer(new HwndPointerEventArgs(HwndPointerDevice.Mouse,
            HwndPointerAction.Move, 0, 16, 18, HwndPointerButton.None, 0), ModifierKeys.None);
        viewport.ProcessD3D12Pointer(new HwndPointerEventArgs(HwndPointerDevice.Mouse,
            HwndPointerAction.Released, 0, 16, 18, HwndPointerButton.Left, 0), ModifierKeys.None);

        Assert.Equal(["preview", "left", "down", "move", "up"], events);
        Assert.NotNull(hit);
        Assert.Equal(new Point(12, 14), hit.Position);
        var button = Assert.IsType<MouseButtonEventArgs>(hit.OriginalInputEventArgs);
        Assert.Equal(MouseButton.Left, button.ChangedButton);
    });
}

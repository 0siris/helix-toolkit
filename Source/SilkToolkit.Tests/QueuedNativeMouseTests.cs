using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.Wpf.SharpDX.Controls;
using Xunit;
using MeshGeometry3D = HelixToolkit.SharpDX.Core.MeshGeometry3D;
using Vector3 = Silk.NET.Maths.Vector3D<float>;

namespace SilkToolkit.Tests;

/// Verifies queued native right-drag delivery through the actual captured HWND beneath a never-visible owner.
[Collection(WpfCollection.Name)]
public sealed class QueuedNativeMouseTests {
    /// <summary>
    ///     Rotates before release over geometry and empty background while retaining mesh state and dispatcher liveness.
    /// </summary>
    /// <param name="hitTriangle">Whether the press must hit the prepared triangle.</param>
    /// <returns>The hidden dispatcher and native-message check.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Warp")]
    [Trait("Category", "Wpf")]
    public Task RightDragMovesCameraBeforeOutsideRelease(bool hitTriangle) => StaThread.RunAsync(async () => {
        using var source = new HwndSource(new HwndSourceParameters(nameof(QueuedNativeMouseTests)) {
            Width = 160, Height = 120, WindowStyle = unchecked((int)0x80000000)
        });
        using var viewport = new Viewport3DX {
            EnableSwapChainRendering = true,
            Width = 160, Height = 120, D3D12DriverType = SilkDriverType.Warp,
            ShowViewCube = false, ShowCoordinateSystem = false,
            Camera = new HelixToolkit.Wpf.SharpDX.OrthographicCamera {
                Position = new Point3D(0, 0, 5), LookDirection = new Vector3D(0, 0, -5),
                UpDirection = new Vector3D(0, 1, 0), Width = 4
            }
        };
        var theme = new ResourceDictionary {
            Source = new Uri("/SilkToolkit;component/Themes/Generic.xaml", UriKind.Relative)
        };
        viewport.Resources.MergedDictionaries.Add(theme);
        viewport.Style = Assert.IsType<Style>(theme[typeof(Viewport3DX)]);
        var geometry = new MeshGeometry3D {
            Positions = new Vector3Collection([new(-0.7f, 0.7f, 0), new(0.7f, 0.7f, 0), new(0, -0.7f, 0)]),
            Normals = new Vector3Collection([Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ]),
            Indices = new IntCollection([0, 2, 1])
        };
        var tooth = new MeshGeometryModel3D {
            Geometry = geometry, Material = HelixToolkit.Wpf.SharpDX.DiffuseMaterials.Red
        };
        var gumGeometry = new MeshGeometry3D {
            Positions = new Vector3Collection([new(-1, -1, -1), new(1, -1, -1), new(0, 1, -1)]),
            Indices = new IntCollection([0, 1, 2])
        };
        var gum = new MeshGeometryModel3D {
            Geometry = gumGeometry, IsHitTestVisible = false,
            Material = HelixToolkit.Wpf.SharpDX.DiffuseMaterials.Red
        };
        viewport.Items.Add(tooth);
        viewport.Items.Add(gum);
        source.RootVisual = viewport;
        try {
            _ = viewport.ApplyTemplate();
            viewport.UpdateLayout();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            var surface = Assert.IsType<D3D12PresentationSurface>(viewport.D3D12Surface);
            var draws = viewport.RenderD3D12Frame(TimeSpan.FromSeconds(1));
            Assert.True(draws > 0);
            Assert.False(IsWindowVisible(source.Handle));
            Assert.False(viewport.EnableAutoOctreeUpdate);
            Assert.NotNull(geometry.Octree);
            Assert.Null(gumGeometry.Octree);
            var tree = geometry.Octree;
            _ = viewport.RenderD3D12Frame(TimeSpan.FromSeconds(2));
            Assert.Same(tree, geometry.Octree);
            var node = Assert.IsType<MeshNode>(tooth.SceneNode);
            var matrix = node.ModelMatrix;
            var transform = tooth.Transform;
            var positions = geometry.Positions!.ToArray();
            var indices = geometry.Indices!.ToArray();
            var downHit = false;
            var downCount = 0;
            var moves = 0;
            var ups = 0;
            viewport.MouseDown3D += (_, args) => {
                downCount++;
                downHit = ((MouseDown3DEventArgs)args).HitTestResult is not null;
            };
            viewport.MouseMove3D += (_, _) => moves++;
            viewport.MouseUp3D += (_, _) => ups++;
            var start = hitTriangle ? new Point(80, 60) : new Point(12, 12);
            var look = viewport.Camera.LookDirection;
            var host = surface.WindowHost;
            var pressed = PostMessage(host.NativeHandle, 0x0204, 0x0002,
                PackPoint(start.X * host.DpiScale, start.Y * host.DpiScale));
            Assert.True(pressed);
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            var capture = GetCapture();
            Assert.NotEqual(0, capture);
            Assert.Equal(1, downCount);
            Assert.Equal(hitTriangle, downHit);
            var heartbeat = 0;
            for (var step = 1; step <= 4; step++) {
                PostAtCapture(host, capture, 0x0200, 0x0002,
                    new Point(start.X + step * 8, start.Y + step * 3));
                await Dispatcher.CurrentDispatcher.InvokeAsync(() => heartbeat++, DispatcherPriority.ApplicationIdle);
            }

            Assert.Equal(0, ups);
            Assert.True(moves >= 4);
            Assert.Equal(4, heartbeat);
            Assert.NotEqual(look, viewport.Camera.LookDirection);
            Assert.Equal(capture, GetCapture());
            Assert.False(IsWindowVisible(source.Handle));
            Assert.Same(tree, geometry.Octree);
            Assert.Same(geometry, tooth.Geometry);
            Assert.Equal(positions, geometry.Positions);
            Assert.Equal(indices, geometry.Indices);
            Assert.Same(transform, tooth.Transform);
            Assert.Equal(matrix, node.ModelMatrix);
            Assert.Null(gumGeometry.Octree);

            PostAtCapture(host, capture, 0x0205, 0, new Point(-20, -20));
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.Equal(1, ups);
            Assert.Equal(0, GetCapture());
        } finally {
            var capture = GetCapture();
            if (capture != 0) {
                _ = PostMessage(capture, 0x0205, 0, PackPoint(-20, -20));
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            }
            source.RootVisual = null;
        }
    });

    /// <summary>
    ///     Queues a point in the real capture owner's client coordinates, including positions outside the child.
    /// </summary>
    /// <param name="host">The original presentation child.</param>
    /// <param name="capture">The HWND returned by the actual native capture API.</param>
    /// <param name="message">The move or release message.</param>
    /// <param name="buttons">The held native mouse buttons.</param>
    /// <param name="point">The presentation-relative DIP point.</param>
    private static void PostAtCapture(HwndSwapChainHost host, nint capture, uint message, nint buttons, Point point) {
        var screen = host.PointToScreen(point);
        var client = new NativePoint { X = (int)screen.X, Y = (int)screen.Y };
        Assert.True(ScreenToClient(capture, ref client));
        var posted = PostMessage(capture, message, buttons, PackPoint(client.X, client.Y));
        Assert.True(posted);
    }

    /// <summary>Packs signed native client coordinates.</summary>
    /// <param name="x">The horizontal physical coordinate.</param>
    /// <param name="y">The vertical physical coordinate.</param>
    /// <returns>The LPARAM-compatible point.</returns>
    private static nint PackPoint(double x, double y) => (nint)((uint)(ushort)(short)x | ((uint)(ushort)(short)y << 16));

    /// The native POINT representation used when the capture belongs to the WPF owner.
    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint {
        /// The horizontal physical coordinate.
        internal int X;
        /// The vertical physical coordinate.
        internal int Y;
    }

    /// <summary>Returns the actual thread mouse capture owner.</summary>
    /// <returns>The capture HWND, or zero.</returns>
    [DllImport("user32.dll")]
    private static extern nint GetCapture();

    /// <summary>Checks effective native visibility without showing a window.</summary>
    /// <param name="window">The hidden owner HWND.</param>
    /// <returns>Whether the window is visible.</returns>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(nint window);

    /// <summary>Converts a physical screen point to the actual capture owner's client coordinates.</summary>
    /// <param name="window">The capture HWND.</param>
    /// <param name="point">The screen point replaced by client coordinates.</param>
    /// <returns>Whether the conversion succeeded.</returns>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ScreenToClient(nint window, ref NativePoint point);

    /// <summary>Queues a native mouse message for ordinary dispatcher delivery.</summary>
    /// <param name="window">The actual receiving HWND.</param>
    /// <param name="message">The native mouse message.</param>
    /// <param name="buttons">The current mouse buttons.</param>
    /// <param name="point">The receiving HWND's packed client coordinates.</param>
    /// <returns>Whether the message was queued.</returns>
    [DllImport("user32.dll", EntryPoint = "PostMessageW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(nint window, uint message, nint buttons, nint point);
}

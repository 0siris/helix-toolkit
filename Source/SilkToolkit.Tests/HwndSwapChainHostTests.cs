using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Camera;
using HelixToolkit.SharpDX.Core.Model.Collection;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.Wpf.SharpDX.Controls;
using HelixToolkit.Wpf.SharpDX.Element3D;
using HelixToolkit.Wpf.SharpDX.Model.Elements3D.AbstractElements3D;
using HelixToolkit.SharpDX.Core.Native;
using Xunit;
using Point = System.Windows.Point;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace SilkToolkit.Tests;

/// <summary>
///     Verifies the pure-WPF child-window foundation for Direct3D 12 presentation.
/// </summary>
[Collection(WpfCollection.Name)]
public sealed class HwndSwapChainHostTests {
    /// <summary>
    ///     Verifies DIP dimensions are rounded up and never produce a zero-sized swap chain.
    /// </summary>
    [Theory]
    [InlineData(0, 0, 1, 1, 1)]
    [InlineData(10, 20, 1, 10, 20)]
    [InlineData(10.1, 20.1, 1.5, 16, 31)]
    public void PresentationSizeRoundsToValidPhysicalPixels(
        double width,
        double height,
        double scale,
        uint expectedWidth,
        uint expectedHeight
    ) {
        Assert.Equal((expectedWidth, expectedHeight), D3D12PresentationSize.Calculate(width, height, scale));
    }

    /// <summary>
    ///     Verifies invalid layout and DPI values fail before reaching DXGI.
    /// </summary>
    [Fact]
    public void PresentationSizeRejectsInvalidValues() {
        Assert.Throws<ArgumentOutOfRangeException>(() => D3D12PresentationSize.Calculate(-1, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => D3D12PresentationSize.Calculate(1, double.NaN, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => D3D12PresentationSize.Calculate(1, 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => D3D12PresentationSize.Calculate(double.MaxValue, 1, 1));
    }

    /// <summary>
    ///     Verifies packed native coordinates preserve signed client positions.
    /// </summary>
    [Theory]
    [InlineData(12, 34)]
    [InlineData(-12, -34)]
    [InlineData(short.MinValue, short.MaxValue)]
    public void PointerCoordinatesDecodeSignedValues(short x, short y) {
        var packed = PackPoint(x, y);

        Assert.Equal(((int) x, (int) y), HwndSwapChainHost.DecodePoint(packed));
    }

    /// <summary>
    ///     Verifies mouse and DPI messages are forwarded through one renderer-neutral event contract.
    /// </summary>
    [Fact]
    public Task HostForwardsMouseAndDpiMessages() {
        return StaThread.RunAsync(() => {
            using var host = new HwndSwapChainHost();
            var events = new List<HwndPointerEventArgs>();
            var dpiChanges = new List<double>();
            host.PointerInput += (_, eventArgs) => events.Add(eventArgs);
            host.DpiScaleChanged += (_, value) => dpiChanges.Add(value);

            Assert.False(host.ProcessMessage(0x0200, 0, PackPoint(12, -34)));
            Assert.False(host.ProcessMessage(0x0201, 0, PackPoint(12, -34)));
            Assert.False(host.ProcessMessage(0x0202, 0, PackPoint(12, -34)));
            Assert.False(host.ProcessMessage(0x020A, (nint) (120 << 16), PackPoint(12, -34)));
            Assert.False(host.ProcessMessage(0x02E0, 144, 0));

            Assert.Collection(events,
                eventArgs => AssertPointer(eventArgs, HwndPointerAction.Move, HwndPointerButton.None, 0),
                eventArgs => AssertPointer(eventArgs, HwndPointerAction.Pressed, HwndPointerButton.Left, 0),
                eventArgs => AssertPointer(eventArgs, HwndPointerAction.Released, HwndPointerButton.Left, 0),
                eventArgs => AssertPointer(eventArgs, HwndPointerAction.Wheel, HwndPointerButton.None, 120));
            Assert.All(events, eventArgs => {
                Assert.Equal(HwndPointerDevice.Mouse, eventArgs.Device);
                Assert.Equal(12, eventArgs.X);
                Assert.Equal(-34, eventArgs.Y);
            });
            Assert.Equal([1.5], dpiChanges);
            Assert.Equal(1.5, host.DpiScale);
        });
    }

    /// <summary>
    ///     Verifies native presses reuse the viewport's existing configurable camera gesture handlers.
    /// </summary>
    [Fact]
    public Task NativePointerResolvesExistingCameraGestureBindings() {
        return StaThread.RunAsync(() => {
            using var viewport = new Viewport3DX();

            Assert.Same(viewport.CameraController.RotateHandler,
                viewport.ResolveD3D12PointerGesture(HwndPointerButton.Right, ModifierKeys.None));
            Assert.Same(viewport.CameraController.ZoomHandler,
                viewport.ResolveD3D12PointerGesture(HwndPointerButton.Right, ModifierKeys.Control));
            Assert.Same(viewport.CameraController.PanHandler,
                viewport.ResolveD3D12PointerGesture(HwndPointerButton.Right, ModifierKeys.Shift));
            Assert.Same(viewport.CameraController.ChangeFieldOfViewHandler,
                viewport.ResolveD3D12PointerGesture(HwndPointerButton.Right, ModifierKeys.Alt));
            Assert.Same(viewport.CameraController.ZoomRectangleHandler,
                viewport.ResolveD3D12PointerGesture(HwndPointerButton.Right,
                    ModifierKeys.Control | ModifierKeys.Shift));
            Assert.Null(viewport.ResolveD3D12PointerGesture(HwndPointerButton.Left, ModifierKeys.None));
        });
    }

    /// <summary>
    ///     Verifies WPF creates and destroys the native child handle on an STA dispatcher.
    /// </summary>
    [Fact]
    [Trait("Category", "Wpf")]
    public Task CreatesAndDestroysChildWindow() {
        return StaThread.RunAsync(() => {
            using var source = new HwndSource(new HwndSourceParameters(nameof(HwndSwapChainHostTests)) {
                Width = 32,
                Height = 24,
                WindowStyle = unchecked((int) 0x80000000)
            });
            var host = new HwndSwapChainHost();
            var created = 0;
            var destroyed = 0;
            host.HandleCreated += (_, _) => created++;
            host.HandleDestroyed += (_, _) => destroyed++;

            source.RootVisual = host;
            host.UpdateLayout();

            Assert.True(host.IsHandleCreated);
            Assert.NotEqual(nint.Zero, host.NativeHandle);
            Assert.Equal(1, created);

            source.RootVisual = null;
            host.Dispose();

            Assert.False(host.IsHandleCreated);
            Assert.Equal(nint.Zero, host.NativeHandle);
            Assert.Equal(1, destroyed);
        });
    }

    /// <summary>
    ///     Verifies resize requests retain only the newest size and schedule one dispatcher operation.
    /// </summary>
    [Fact]
    public void ResizeQueueCoalescesToNewestSize() {
        var queue = new D3D12ResizeQueue();

        Assert.True(queue.Request(10, 20));
        Assert.False(queue.Request(30, 40));
        Assert.False(queue.Request(50, 60));
        Assert.True(queue.TryTake(out var size));
        Assert.Equal((50U, 60U), size);
        Assert.False(queue.TryTake(out _));
        Assert.True(queue.Request(70, 80));
    }

    /// <summary>
    ///     Verifies the resize queue rejects dimensions that DXGI cannot accept.
    /// </summary>
    [Fact]
    public void ResizeQueueRejectsZeroDimensions() {
        var queue = new D3D12ResizeQueue();

        Assert.Throws<ArgumentOutOfRangeException>(() => queue.Request(0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => queue.Request(1, 0));
        Assert.False(queue.TryTake(out _));
    }

    /// <summary>
    ///     Verifies viewport camera state maps to the complete physical-frame transform contract.
    /// </summary>
    [Fact]
    public void ViewportTransformsUseCameraAndPhysicalSurfaceDimensions() {
        var camera = new PerspectiveCameraCore {
            Position = new Vector3(1, 2, 3),
            LookDirection = new Vector3(0, 0, -3),
            UpDirection = Vector3.UnitY,
            NearPlaneDistance = 0.5f,
            FarPlaneDistance = 500
        };

        var transforms = D3D12PresentationSurface.CreateViewportTransforms(camera,
            80,
            40,
            1.5f,
            12.25f,
            out var frustum);

        Assert.Equal(new Vector4(80, 40, 1f / 80, 1f / 40), transforms.Viewport);
        Assert.Equal(transforms.Viewport, transforms.Resolution);
        Assert.Equal(camera.Position, transforms.EyePos);
        Assert.Equal(2, transforms.Frustum.Y);
        Assert.Equal(0.5f, transforms.Frustum.Z);
        Assert.Equal(500, transforms.Frustum.W);
        Assert.Equal(1.5f, transforms.DpiScale);
        Assert.Equal(12.25f, transforms.TimeStamp);
        Assert.True(transforms.IsPerspective);
        Assert.False(frustum.IsOrthographic);
        Assert.Equal(camera.CreateViewMatrix() * camera.CreateProjectionMatrix(2), transforms.ViewProjection);
    }

    /// <summary>
    ///     Verifies the WPF surface repeatedly attaches, renders, resizes, detaches, and disposes on WARP.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    [Trait("Category", "Wpf")]
    public Task WpfSurfaceAttachesRendersResizesAndDetaches() {
        return StaThread.RunAsync(() => {
            for (var iteration = 0; iteration < 3; iteration++) {
                using var source = new HwndSource(new HwndSourceParameters(nameof(HwndSwapChainHostTests)) {
                    Width = 32,
                    Height = 24,
                    WindowStyle = unchecked((int) 0x80000000)
                });
                using var surface = new D3D12PresentationSurface(SilkDriverType.Warp) {
                    Width = 32,
                    Height = 24
                };

                source.RootVisual = surface;
                surface.UpdateLayout();
                Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.ApplicationIdle, () => { });

                Assert.True(surface.IsInitialized);
                var initialSize = D3D12PresentationSize.Calculate(32, 24, surface.WindowHost.DpiScale);
                Assert.Equal(initialSize.Width, surface.PixelWidth);
                Assert.Equal(initialSize.Height, surface.PixelHeight);
                using var node = new MeshNode {
                    Material = new DiffuseMaterialCore(),
                    Geometry = new MeshGeometry3D {
                        Positions = new Vector3Collection([
                            new Vector3(-0.8f, 0.8f, 0),
                            new Vector3(0.8f, 0.8f, 0),
                            new Vector3(-0.8f, -0.8f, 0),
                            new Vector3(0.8f, -0.8f, 0)
                        ]),
                        Colors = new Color4Collection([
                            new Vector4(1, 0, 0, 1),
                            new Vector4(1, 0, 0, 1),
                            new Vector4(1, 0, 0, 1),
                            new Vector4(1, 0, 0, 1)
                        ]),
                        Indices = new IntCollection([0, 1, 2, 2, 1, 3])
                    }
                };
                var camera = new PerspectiveCameraCore {
                    Position = new Vector3(0, 0, 2),
                    LookDirection = new Vector3(0, 0, -2),
                    UpDirection = Vector3.UnitY,
                    NearPlaneDistance = 0.1f,
                    FarPlaneDistance = 100
                };
                Assert.Equal(1,
                    surface.RenderViewportOnce([0.25f, 0.5f, 0.75f, 1], [node], camera, true));
                Assert.Equal(1UL, surface.PresentedFrames);

                surface.Width = 80;
                surface.Height = 60;
                surface.UpdateLayout();
                Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.ApplicationIdle, () => { });

                var resized = D3D12PresentationSize.Calculate(80, 60, surface.WindowHost.DpiScale);
                Assert.Equal(resized.Width, surface.PixelWidth);
                Assert.Equal(resized.Height, surface.PixelHeight);
                surface.RenderOnce([0, 0, 0, 1]);
                Assert.Equal(2UL, surface.PresentedFrames);

                source.RootVisual = null;
                surface.Dispose();
                Assert.False(surface.IsInitialized);
                Assert.True(surface.IsDisposed);
            }
        });
    }

    /// <summary>
    ///     Verifies the existing viewport swap-chain option hosts and renders through Direct3D 12 on WARP.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    [Trait("Category", "Wpf")]
    public Task ViewportSwapChainModeUsesDx12SurfaceAndCurrentCamera() {
        return StaThread.RunAsync(() => {
            using var source = new HwndSource(new HwndSourceParameters(nameof(HwndSwapChainHostTests)) {
                Width = 48,
                Height = 32,
                WindowStyle = unchecked((int) 0x80000000)
            });
            using var viewport = new Viewport3DX {
                Width = 48,
                Height = 32,
                EnableSwapChainRendering = true,
                D3D12DriverType = SilkDriverType.Warp
            };
            var theme = new ResourceDictionary {
                Source = new Uri("/SilkToolkit;component/Themes/Generic.xaml", UriKind.Relative)
            };
            viewport.Resources.MergedDictionaries.Add(theme);
            viewport.Style = Assert.IsType<Style>(theme[typeof(Viewport3DX)]);
            viewport.Items.Add(new MeshGeometryModel3D {
                Material = HelixToolkit.Wpf.SharpDX.Material.DiffuseMaterials.Red,
                Geometry = new MeshGeometry3D {
                    Positions = new Vector3Collection([
                        new Vector3(-0.5f, 0.5f, 0),
                        new Vector3(0.5f, 0.5f, 0),
                        new Vector3(0, -0.5f, 0)
                    ]),
                    Indices = new IntCollection([0, 1, 2])
                }
            });

            source.RootVisual = viewport;
            _ = viewport.ApplyTemplate();
            viewport.UpdateLayout();
            Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.ApplicationIdle, () => { });

            var surface = Assert.IsType<D3D12PresentationSurface>(viewport.D3D12Surface);
            Assert.True(surface.IsInitialized);
            Assert.Null(viewport.RenderHost);
            Assert.Single(viewport.Renderables.OfType<MeshNode>());
            Assert.Equal(1, viewport.RenderD3D12Frame(TimeSpan.FromSeconds(1)));
            Assert.True(surface.PresentedFrames >= 1);

            var originalPosition = viewport.Camera.Position;
            surface.WindowHost.ProcessMessage(0x020A, (nint) (120 << 16), PackPoint(12, 12));
            Assert.Equal(1, viewport.RenderD3D12Frame(TimeSpan.FromSeconds(2)));
            Assert.NotEqual(originalPosition, viewport.Camera.Position);

            var pointerPositions = new List<Point>();
            viewport.MouseDown3D += (_, eventArgs) =>
                pointerPositions.Add(((MouseDown3DEventArgs) eventArgs).Position);
            viewport.MouseMove3D += (_, eventArgs) =>
                pointerPositions.Add(((MouseMove3DEventArgs) eventArgs).Position);
            viewport.MouseUp3D += (_, eventArgs) =>
                pointerPositions.Add(((MouseUp3DEventArgs) eventArgs).Position);
            var originalLookDirection = viewport.Camera.LookDirection;
            surface.WindowHost.ProcessMessage(0x0204, 0, PackPoint(12, 12));
            surface.WindowHost.ProcessMessage(0x0200, 0, PackPoint(30, 18));
            surface.WindowHost.ProcessMessage(0x0205, 0, PackPoint(30, 18));

            Assert.Equal(3, pointerPositions.Count);
            Assert.Equal(new Point(12 / surface.WindowHost.DpiScale, 12 / surface.WindowHost.DpiScale),
                pointerPositions[0]);
            Assert.NotEqual(originalLookDirection, viewport.Camera.LookDirection);

            viewport.ProcessD3D12Pointer(new HwndPointerEventArgs(HwndPointerDevice.Touch,
                HwndPointerAction.Pressed,
                7,
                8,
                8,
                HwndPointerButton.None,
                0), ModifierKeys.None);
            viewport.ProcessD3D12Pointer(new HwndPointerEventArgs(HwndPointerDevice.Touch,
                HwndPointerAction.Pressed,
                8,
                10,
                10,
                HwndPointerButton.None,
                0), ModifierKeys.None);
            viewport.ProcessD3D12Pointer(new HwndPointerEventArgs(HwndPointerDevice.Touch,
                HwndPointerAction.Move,
                7,
                9,
                9,
                HwndPointerButton.None,
                0), ModifierKeys.None);
            viewport.ProcessD3D12Pointer(new HwndPointerEventArgs(HwndPointerDevice.Touch,
                HwndPointerAction.Released,
                7,
                9,
                9,
                HwndPointerButton.None,
                0), ModifierKeys.None);
            Assert.Equal(6, pointerPositions.Count);

            source.RootVisual = null;
        });
    }

    /// <summary>
    ///     Packs signed coordinates into the LPARAM layout used by mouse messages.
    /// </summary>
    /// <param name="x">The horizontal coordinate.</param>
    /// <param name="y">The vertical coordinate.</param>
    /// <returns>The packed value.</returns>
    private static nint PackPoint(short x, short y) =>
        (nint) ((uint) (ushort) x | ((uint) (ushort) y << 16));

    /// <summary>
    ///     Verifies the shared pointer fields for one expected mouse event.
    /// </summary>
    /// <param name="eventArgs">The actual event.</param>
    /// <param name="action">The expected action.</param>
    /// <param name="button">The expected button.</param>
    /// <param name="wheelDelta">The expected wheel delta.</param>
    private static void AssertPointer(
        HwndPointerEventArgs eventArgs,
        HwndPointerAction action,
        HwndPointerButton button,
        int wheelDelta
    ) {
        Assert.Equal(action, eventArgs.Action);
        Assert.Equal(button, eventArgs.Button);
        Assert.Equal(wheelDelta, eventArgs.WheelDelta);
    }
}

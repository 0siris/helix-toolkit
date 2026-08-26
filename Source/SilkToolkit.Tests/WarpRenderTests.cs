using System.Windows.Interop;
using System.Windows.Threading;
using HelixToolkit.SharpDX.Core.Model.Camera;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.Wpf.SharpDX.Controls;
using Xunit;
using Vector3 = Silk.NET.Maths.Vector3D<float>;

namespace SilkToolkit.Tests;

/// <summary>
///     Verifies the Direct3D 12 WARP presentation path.
/// </summary>
[Collection(WpfCollection.Name)]
public sealed class WarpRenderTests {
    /// <summary>
    ///     Verifies a clear-only frame can be rendered and captured as tightly packed BGRA pixels.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public Task ClearsAndCapturesSwapChainTarget() => StaThread.RunAsync(() => {
        using var window = CreateWindow();
        using var surface = CreateSurface(window, 64, 32);

        Assert.Equal(0, surface.RenderViewportOnce([0.25f, 0.5f, 0.75f, 1], [], CreateCamera(), false,
            captureFrame: true));

        var frame = Assert.IsType<D3D12CapturedFrame>(surface.LastCapturedFrame);
        Assert.Equal(surface.PixelWidth, frame.Width);
        Assert.Equal(surface.PixelHeight, frame.Height);
        Assert.Equal(checked((int) (frame.Width * frame.Height * 4)), frame.Pixels.Length);
        Assert.InRange(frame.Pixels[0], 188, 194);
        Assert.InRange(frame.Pixels[1], 124, 130);
        Assert.InRange(frame.Pixels[2], 60, 66);
        Assert.Equal(255, frame.Pixels[3]);
    });

    /// <summary>
    ///     Verifies the WARP swap chain presents before and after a WPF resize.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    [Trait("Category", "Wpf")]
    public Task ResizesPresentsAndCapturesHwndSwapChain() => StaThread.RunAsync(() => {
        using var window = CreateWindow();
        using var surface = CreateSurface(window, 32, 24);

        surface.RenderViewportOnce([0, 0, 0, 1], [], CreateCamera(), false);
        surface.Width = 80;
        surface.Height = 60;
        surface.UpdateLayout();
        Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.ApplicationIdle, () => { });
        surface.RenderViewportOnce([0, 0, 0, 1], [], CreateCamera(), false, captureFrame: true);

        var frame = Assert.IsType<D3D12CapturedFrame>(surface.LastCapturedFrame);
        Assert.Equal(surface.PixelWidth, frame.Width);
        Assert.Equal(surface.PixelHeight, frame.Height);
        Assert.Equal(2UL, surface.PresentedFrames);
    });

    /// <summary>
    ///     Verifies the WARP surface owns a valid Direct3D 12 depth target.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    [Trait("Category", "Wpf")]
    public Task InitializesHwndSwapChainWithDepthStencilBuffer() => StaThread.RunAsync(() => {
        using var window = CreateWindow();
        using var surface = CreateSurface(window, 32, 24);

        Assert.True(surface.IsInitialized);
        Assert.Equal(D3D12PresentationSize.Calculate(32, 24, surface.WindowHost.DpiScale),
            (surface.PixelWidth, surface.PixelHeight));
        surface.RenderViewportOnce([0, 0, 0, 1], [], CreateCamera(), false);
        Assert.Equal(1UL, surface.PresentedFrames);
    });

    /// <summary>
    ///     Creates the hidden WPF owner used by a WARP presentation surface.
    /// </summary>
    private static HwndSource CreateWindow() => new(new HwndSourceParameters(nameof(WarpRenderTests)) {
        Width = 80,
        Height = 60,
        WindowStyle = unchecked((int) 0x80000000)
    });

    /// <summary>
    ///     Creates and initializes a WARP presentation surface at the requested size.
    /// </summary>
    private static D3D12PresentationSurface CreateSurface(HwndSource window, double width, double height) {
        var surface = new D3D12PresentationSurface(SilkDriverType.Warp) { Width = width, Height = height };
        window.RootVisual = surface;
        surface.UpdateLayout();
        Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.ApplicationIdle, () => { });
        return surface;
    }

    /// <summary>
    ///     Creates a deterministic camera for clear-only presentation tests.
    /// </summary>
    private static PerspectiveCameraCore CreateCamera() => new() {
        Position = new Vector3(0, 0, 2),
        LookDirection = new Vector3(0, 0, -2),
        UpDirection = Vector3.UnitY,
        NearPlaneDistance = 0.1f,
        FarPlaneDistance = 100
    };
}

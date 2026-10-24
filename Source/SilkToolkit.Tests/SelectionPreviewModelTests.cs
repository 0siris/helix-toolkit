using HelixToolkit.SharpDX.Core.Core2D;
using HelixToolkit.SharpDX.Core.Core2D.Models;
using HelixToolkit.SharpDX.Core.Model.Scene2D;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.Wpf.SharpDX.Controls;
using HelixToolkit.Wpf.SharpDX.Model.Elements2D;
using SilkToolkit.Tests;
using Xunit;
using Vector2 = Silk.NET.Maths.Vector2D<float>;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace HelixToolkit.Wpf.SharpDX.Model.Elements2D;

/// <summary>Checks the actual thin path model and metrics getter without opening a native window.</summary>
[Collection(WpfCollection.Name)]
public sealed class SelectionPreviewModelTests {
    /// <summary>Verifies attachment preserves the exact native core data without WPF byte-color conversion.</summary>
    /// <returns>The existing STA test dispatcher completion.</returns>
    [Fact]
    [Trait("Category", "Wpf")]
    public Task ActualPathModelRetainsOneCoreAndExactNativeBrushTest() => StaThread.RunAsync(() => {
        using var model = new PathModel2D();
        using var context = new D2DDeviceContext();
        var color = new Vector4(BitConverter.Int32BitsToSingle(0x3f547ae1),
            BitConverter.Int32BitsToSingle(0x3d8f5c29), BitConverter.Int32BitsToSingle(0x3d8f5c29), 1);
        var core = model.PathCore;
        var brush = new SolidColorBrush(context, color);
        var figure = new Figure(new Vector2(4, 4), false, false);
        figure.AddSegment(new LineSegment(new Vector2(20, 4)), true, false);
        core.Figures = [figure];
        core.StrokeBrush = brush;
        core.StrokeWidth = 3;
        model.SceneNode.Attach(1.5f);
        var hit = model.SceneNode.HitTest(new Vector2(8, 4), out var result);
        var actualBrush = core.StrokeBrush as SolidColorBrush;

        Assert.IsType<PathNode2D>(model.SceneNode);
        Assert.Same(core, model.PathCore);
        Assert.IsType<PathRenderCore2D>(model.SceneNode.RenderCore);
        Assert.Same(figure, core.Figures![0]);
        Assert.Same(brush, actualBrush);
        Assert.Equal(color, actualBrush!.Color);
        Assert.Equal(3f, core.StrokeWidth);
        Assert.False(hit);
        Assert.Null(result);
        model.Dispose();
        Assert.True(core.IsDisposed);
        Assert.True(brush.IsDisposed);
    });

    /// <summary>Verifies direct-core resource ownership is released even without host attachment.</summary>
    /// <returns>The existing STA test dispatcher completion.</returns>
    [Fact]
    [Trait("Category", "Wpf")]
    public Task NeverAttachedPathModelDisposesItsOwnedBrushTest() => StaThread.RunAsync(() => {
        using var model = new PathModel2D();
        using var context = new D2DDeviceContext();
        var core = model.PathCore;
        var brush = new SolidColorBrush(context, Vector4.One);
        core.StrokeBrush = brush;
        model.Dispose();

        Assert.True(core.IsDisposed);
        Assert.True(brush.IsDisposed);
    });

    /// <summary>Verifies metrics reads neither initialize a surface nor substitute configured DPI for actual metrics.</summary>
    /// <returns>The existing STA test dispatcher completion.</returns>
    [Fact]
    [Trait("Category", "Wpf")]
    public Task UninitializedMetricsAreReadOnlyAndReturnDefaultsTest() => StaThread.RunAsync(() => {
        using var viewport = new Viewport3DX { DpiScale = 2 };
        var camera = viewport.Camera;
        var beforeReady = viewport.IsRenderReady;
        var found = viewport.TryGetPresentationMetrics(out var width, out var height, out var dpi);
        var again = viewport.TryGetPresentationMetrics(out var secondWidth, out var secondHeight, out var secondDpi);

        Assert.False(found);
        Assert.False(again);
        Assert.Equal(0u, width);
        Assert.Equal(0u, height);
        Assert.Equal(0f, dpi);
        Assert.Equal(width, secondWidth);
        Assert.Equal(height, secondHeight);
        Assert.Equal(dpi, secondDpi);
        Assert.Same(camera, viewport.Camera);
        Assert.Null(viewport.D3D12Surface);
        Assert.False(beforeReady);
        Assert.False(viewport.IsRenderReady);
        Assert.Equal(2, viewport.DpiScale);
    });

    /// <summary>Verifies metrics enforce actual dispatcher ownership before accessing presentation state.</summary>
    /// <returns>The existing STA test dispatcher completion.</returns>
    [Fact]
    [Trait("Category", "Wpf")]
    public Task MetricsRejectAnotherThreadWithoutInitializingSurfaceTest() => StaThread.RunAsync(async () => {
        using var viewport = new Viewport3DX();
        var failure = await Task.Run(() => Record.Exception(() =>
            viewport.TryGetPresentationMetrics(out _, out _, out _)));

        Assert.IsType<InvalidOperationException>(failure);
        Assert.Null(viewport.D3D12Surface);
    });
}
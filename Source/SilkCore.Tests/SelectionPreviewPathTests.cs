using HelixToolkit.SharpDX.Core.Core2D;
using HelixToolkit.SharpDX.Core.Core2D.Models;
using HelixToolkit.SharpDX.Core.Model.Scene2D;
using HelixToolkit.SharpDX.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Vector2 = Silk.NET.Maths.Vector2D<float>;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace HelixToolkit.SharpDX.Core.Native;

/// <summary>Exercises real native Figure geometry and the existing Scene2D sprite consumer without a window.</summary>
/// <remarks>All cases are authored source drafts; they require root's serialized native or WARP execution.</remarks>
public sealed class SelectionPreviewPathTests {
    /// <summary>Verifies native widening connects a bevel corner without filling or closing the open strip.</summary>
    [Fact]
    [Trait("Category", "NativeGeometry")]
    public void NativeOpenFigureProducesConnectedCornerAndPreservesNegativeSpaceTest() {
        using var factory = new D2DFactory();
        using var style = CreateStyle(factory);
        using var core = new PathRenderCore2D { Figures = OpenFigure(new(4, 4), new(20, 4), new(20, 20)) };
        var geometry = core.EnsureGeometry(factory)!;
        var points = D3D12PathTessellator.TessellateStroke(geometry, 3, style);
        var cornerCovered = Covers(points, new Vector2(20.5f, 3.2f));
        var fillCovered = Covers(points, new Vector2(15, 10));
        var implicitCloseCovered = Covers(points, new Vector2(12, 12));
        var endpointDotCovered = Covers(points, new Vector2(2.75f, 4));

        Assert.NotEmpty(points);
        Assert.Equal(0, points.Length % 3);
        Assert.All(points, point => Assert.True(float.IsFinite(point.X) && float.IsFinite(point.Y)));
        Assert.True(cornerCovered);
        Assert.False(fillCovered);
        Assert.False(implicitCloseCovered);
        Assert.False(endpointDotCovered);
    }

    /// <summary>Verifies empty, single and duplicate-only vectors produce no area and real duplicates stay safe.</summary>
    /// <param name="kind">The selected source vector, including a populated duplicate-interior case.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [Trait("Category", "NativeGeometry")]
    public void NativeDegenerateFiguresDoNotCreateEndpointGlyphsTest(int kind) {
        using var factory = new D2DFactory();
        using var style = CreateStyle(factory);
        var figures = kind switch {
            0 => new List<Figure>(),
            1 => OpenFigure(new Vector2(4, 4)),
            2 => OpenFigure(new(4, 4), new(4, 4)),
            _ => OpenFigure(new(4, 4), new(4, 4), new(20, 4))
        };
        using var core = new PathRenderCore2D { Figures = figures };
        var geometry = core.EnsureGeometry(factory);
        Vector2[] points = geometry is null ? [] : D3D12PathTessellator.TessellateStroke(geometry, 3, style);

        if (kind < 3) Assert.Empty(points);
        else {
            Assert.NotEmpty(points);
            var midpointCovered = Covers(points, new Vector2(12, 4));
            Assert.True(midpointCovered);
        }
        Assert.All(points, point => Assert.True(float.IsFinite(point.X) && float.IsFinite(point.Y)));
        Assert.Same(figures, core.Figures);
    }

    /// <summary>Verifies reversing and self-crossing native open strokes remain finite and do not fill regions.</summary>
    /// <param name="crossing">Whether the source crosses itself rather than reversing an existing segment.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "NativeGeometry")]
    public void NativeDifficultOpenVectorsRemainStrokesTest(bool crossing) {
        using var factory = new D2DFactory();
        using var style = CreateStyle(factory);
        var figures = crossing
            ? OpenFigure(new(4, 4), new(20, 20), new(4, 20), new(20, 4))
            : OpenFigure(new(4, 4), new(20, 4), new(4, 4));
        using var core = new PathRenderCore2D { Figures = figures };
        var geometry = core.EnsureGeometry(factory)!;
        var points = D3D12PathTessellator.TessellateStroke(geometry, 3, style);
        var negative = Covers(points, crossing ? new Vector2(12, 16) : new Vector2(12, 10));

        Assert.NotEmpty(points);
        Assert.All(points, point => Assert.True(float.IsFinite(point.X) && float.IsFinite(point.Y)));
        Assert.False(negative);
        Assert.Same(figures, core.Figures);
    }

    /// <summary>Verifies same-count replacement, empty replacement and failed native Close cannot reuse old paths.</summary>
    [Fact]
    [Trait("Category", "NativeGeometry")]
    public void NativeCacheReplacementAndFailureDiscardPreviousGeometryTest() {
        using var factory = new D2DFactory();
        using var core = new PathRenderCore2D { Figures = OpenFigure(new(4, 4), new(20, 4)) };
        var first = core.EnsureGeometry(factory)!;
        core.Figures = OpenFigure(new(4, 12), new(20, 12));
        var replacement = core.EnsureGeometry(factory)!;
        var invalid = new Figure(new Vector2(4, 8), false, false);
        invalid.AddSegment(new InvalidFigureSegment(), true, false);
        core.Figures = [invalid];
        var failure = Record.Exception(() => core.EnsureGeometry(factory));
        core.Figures = OpenFigure(new(4, 20), new(20, 20));
        var recovered = core.EnsureGeometry(factory)!;
        core.Figures = null;
        var empty = core.EnsureGeometry(factory);

        Assert.True(first.IsDisposed);
        Assert.NotSame(first, replacement);
        Assert.NotNull(failure);
        Assert.True(replacement.IsDisposed);
        Assert.True(recovered.IsDisposed);
        Assert.Null(empty);
    }

    /// <summary>Verifies native geometry is released on detach, factory change and never-attached core disposal.</summary>
    [Fact]
    [Trait("Category", "NativeGeometry")]
    public void NativeCacheFollowsActualCoreAndFactoryLifetimeTest() {
        using var firstFactory = new D2DFactory();
        using var secondFactory = new D2DFactory();
        var core = new PathRenderCore2D { Figures = OpenFigure(new(4, 4), new(20, 4)) };
        try {
            core.Attach();
            var attached = core.EnsureGeometry(firstFactory)!;
            core.Detach();
            core.Attach();
            var reattached = core.EnsureGeometry(firstFactory)!;
            var otherFactory = core.EnsureGeometry(secondFactory)!;
            core.Dispose();

            Assert.True(attached.IsDisposed);
            Assert.True(reattached.IsDisposed);
            Assert.True(otherFactory.IsDisposed);
            Assert.NotSame(attached, reattached);
        } finally {
            core.Dispose();
        }
        var unattached = new PathRenderCore2D { Figures = OpenFigure(new(1, 1), new(5, 1)) };
        var unattachedPath = unattached.EnsureGeometry(firstFactory)!;
        unattached.Dispose();
        Assert.True(unattachedPath.IsDisposed);
    }

    /// <summary>Verifies Figures replacement marks native geometry dirty before synchronous invalidation observers run.</summary>
    [Fact]
    [Trait("Category", "NativeGeometry")]
    public void SameCountReplacementIsCurrentDuringInvalidationTest() {
        using var factory = new D2DFactory();
        using var style = CreateStyle(factory);
        using var core = new PathRenderCore2D { Figures = OpenFigure(new(4, 4), new(20, 4)) };
        _ = core.EnsureGeometry(factory);
        Vector2[] observed = [];
        core.InvalidateRender += (_, _) => {
            var geometry = core.EnsureGeometry(factory)!;
            observed = D3D12PathTessellator.TessellateStroke(geometry, 3, style);
        };
        core.Figures = OpenFigure(new(4, 12), new(20, 12));
        var oldCovered = Covers(observed, new Vector2(12, 4));
        var newCovered = Covers(observed, new Vector2(12, 12));

        Assert.False(oldCovered);
        Assert.True(newCovered);
    }

    /// <summary>Verifies prepared native triangles preserve exact color, connected geometry and physical full width.</summary>
    /// <param name="dpi">The host DPI supplied to the actual existing Scene2D renderer.</param>
    [Theory]
    [InlineData(1f)]
    [InlineData(1.25f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    [Trait("Category", "Warp")]
    public void WarpPreparesExactPhysicalPathIndependentOfTextDpiTest(float dpi) {
        using var fixture = new SpriteFixture();
        using var node = new PathNode2D();
        var core = (PathRenderCore2D) node.RenderCore;
        Configure(core, fixture.PathFactory, new Vector2(4, 4), new Vector2(20, 4));
        fixture.Renderer.BeginFrame();
        var count = fixture.Renderer.Prepare([node], 32, 32, dpi);
        var draw = fixture.Renderer.Draws.Single();
        var minimum = draw.Vertices.Min(vertex => vertex.Position.Y);
        var maximum = draw.Vertices.Max(vertex => vertex.Position.Y);
        var colors = draw.Vertices.Select(vertex => vertex.Color).ToArray();

        Assert.Equal(1, count);
        Assert.Equal(2.5f, minimum);
        Assert.Equal(5.5f, maximum);
        Assert.All(colors, color => {
            Assert.Equal(0x3f547ae1, BitConverter.SingleToInt32Bits(color.X));
            Assert.Equal(0x3d8f5c29, BitConverter.SingleToInt32Bits(color.Y));
            Assert.Equal(0x3d8f5c29, BitConverter.SingleToInt32Bits(color.Z));
            Assert.Equal(1f, color.W);
        });
    }

    /// <summary>Verifies the real renderer applies the parent transform once and inherits clip and sibling order.</summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpPreparesPathTransformClipAndSiblingOrderTest() {
        using var fixture = new SpriteFixture();
        using var root = new PanelNode2D { ClipToBound = true, RenderTransformOrigin = Vector2.Zero };
        using var path = new PathNode2D { RenderTransformOrigin = Vector2.Zero };
        using var next = new PathNode2D { RenderTransformOrigin = Vector2.Zero };
        Configure((PathRenderCore2D) path.RenderCore, fixture.PathFactory, new Vector2(4, 4), new Vector2(12, 4));
        Configure((PathRenderCore2D) next.RenderCore, fixture.PathFactory, new Vector2(4, 10), new Vector2(12, 10));
        root.RenderCore.LayoutClippingBound = new RectangleF(0, 0, 20, 16);
        root.AddChildNode(path);
        root.AddChildNode(next);
        fixture.Renderer.Prepare([root], 64, 64, 1);
        var original = fixture.Renderer.Draws[0].Vertices.Select(vertex => vertex.Position).ToArray();
        root.ModelMatrix = new Matrix3X2(2, 0, 0, 0.5f, 0, 0)
                           * Matrix3X2.Rotation(0.2f) * Matrix3X2.Translation(7, 9);
        var count = fixture.Renderer.Prepare([root], 64, 64, 1);
        var expected = original.Select(point => Matrix3X2.TransformPoint(root.TotalModelMatrix, point)).ToArray();
        var actual = fixture.Renderer.Draws[0].Vertices.Select(vertex => vertex.Position).ToArray();
        var clip = D3D12Scene2DRenderer.Intersect(new RectangleF(0, 0, 64, 64),
            D3D12Scene2DRenderer.TransformBounds(new RectangleF(0, 0, 20, 16), root.TotalModelMatrix));
        var expectedClip = new RectangleF(MathF.Floor(clip.Left), MathF.Floor(clip.Top),
            MathF.Ceiling(clip.Right) - MathF.Floor(clip.Left), MathF.Ceiling(clip.Bottom) - MathF.Floor(clip.Top));

        Assert.Equal(2, count);
        Assert.Same(path, fixture.Renderer.Draws[0].Node);
        Assert.Same(next, fixture.Renderer.Draws[1].Node);
        Assert.Equal(expected, actual);
        Assert.Equal(expectedClip, fixture.Renderer.Draws[0].Clip);
        root.RenderCore.LayoutClippingBound = new RectangleF(0, 0, 0, 0);
        var empty = fixture.Renderer.Prepare([root], 64, 64, 1);
        Assert.Equal(0, empty);
    }

    /// <summary>Verifies real Sprite2D readback keeps the open path visible and its negative space untouched.</summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpRendersOpenPathAndResizedUpdatedGeometryTest() {
        using var fixture = new SpriteFixture();
        using var node = new PathNode2D();
        var core = (PathRenderCore2D) node.RenderCore;
        Configure(core, fixture.PathFactory, new Vector2(4, 4), new Vector2(20, 4), new Vector2(20, 20));
        foreach (var size in new uint[] { 32, 48 }) {
            fixture.Renderer.BeginFrame();
            fixture.Renderer.Prepare([node], size, size, 1.5f);
            var pixels = fixture.Render(size, size);
            var stroke = Pixel(pixels, size, 12, 4);
            var implicitClose = Pixel(pixels, size, 12, 12);
            var dot = Pixel(pixels, size, 2, 4);
            Assert.InRange(stroke[0], (byte) 211, (byte) 213);
            Assert.InRange(stroke[1], (byte) 17, (byte) 19);
            Assert.Equal(new byte[] { 0, 0, 255, 255 }, implicitClose);
            Assert.Equal(new byte[] { 0, 0, 255, 255 }, dot);
        }
        core.Figures = OpenFigure(new(4, 12), new(20, 12), new(20, 24));
        fixture.Renderer.BeginFrame();
        fixture.Renderer.Prepare([node], 32, 32, 1);
        var changed = fixture.Render(32, 32);
        var oldPixel = Pixel(changed, 32, 12, 4);
        var newPixel = Pixel(changed, 32, 12, 12);
        Assert.Equal(new byte[] { 0, 0, 255, 255 }, oldPixel);
        Assert.InRange(newPixel[0], (byte) 211, (byte) 213);
        core.Figures = [];
        fixture.Renderer.BeginFrame();
        var empty = fixture.Renderer.Prepare([node], 32, 32, 1);
        Assert.Equal(0, empty);
    }

    /// <summary>Verifies actual scissor readback and later sibling draws affect only their admitted pixels.</summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpRendersPathWithInheritedScissorAndOrderedSiblingTest() {
        using var fixture = new SpriteFixture();
        using var root = new PanelNode2D { ClipToBound = true };
        using var first = new PathNode2D();
        using var last = new PathNode2D();
        Configure((PathRenderCore2D) first.RenderCore, fixture.PathFactory, new Vector2(4, 4), new Vector2(24, 4));
        Configure((PathRenderCore2D) last.RenderCore, fixture.PathFactory, new Vector2(8, 4), new Vector2(12, 4));
        using var context = new D2DDeviceContext();
        ((PathRenderCore2D) last.RenderCore).StrokeBrush = new SolidColorBrush(context, new Vector4(0, 1, 0, 1));
        root.RenderCore.LayoutClippingBound = new RectangleF(0, 0, 16, 16);
        root.AddChildNode(first);
        root.AddChildNode(last);
        fixture.Renderer.BeginFrame();
        fixture.Renderer.Prepare([root], 32, 32, 1);
        var pixels = fixture.Render(32, 32);
        var firstOnly = Pixel(pixels, 32, 6, 4);
        var overlapped = Pixel(pixels, 32, 10, 4);
        var clipped = Pixel(pixels, 32, 20, 4);

        Assert.InRange(firstOnly[0], (byte) 211, (byte) 213);
        Assert.Equal(new byte[] { 0, 255, 0, 255 }, overlapped);
        Assert.Equal(new byte[] { 0, 0, 255, 255 }, clipped);
    }

    /// <summary>Verifies width and color replacement reach actual prepared geometry, while fill is explicit failure.</summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpReadsChangedStrokeStateAndRejectsUnsupportedFillTest() {
        using var fixture = new SpriteFixture();
        using var factory = new D2DFactory();
        using var node = new PathNode2D();
        var core = (PathRenderCore2D) node.RenderCore;
        Configure(core, fixture.PathFactory, new Vector2(4, 8), new Vector2(20, 8));
        core.StrokeStyle = CreateStyle(factory);
        fixture.Renderer.Prepare([node], 32, 32, 1);
        using var context = new D2DDeviceContext();
        core.StrokeWidth = 5;
        core.StrokeBrush = new SolidColorBrush(context, new Vector4(0, 1, 0, 1));
        core.StrokeStyle = new StrokeStyle(factory, new StrokeStyleProperties {
            StartCap = CapStyle.Square, EndCap = CapStyle.Flat, DashCap = CapStyle.Flat,
            DashStyle = DashStyle.Solid, LineJoin = LineJoin.Bevel, MiterLimit = 1
        });
        fixture.Renderer.Prepare([node], 32, 32, 2);
        var draw = fixture.Renderer.Draws.Single();
        var span = draw.Vertices.Max(vertex => vertex.Position.Y) - draw.Vertices.Min(vertex => vertex.Position.Y);
        var colors = draw.Vertices.Select(vertex => vertex.Color).ToArray();
        var minimumX = draw.Vertices.Min(vertex => vertex.Position.X);
        core.FillBrush = new SolidColorBrush(context, Vector4.One);
        var failure = Record.Exception(() => fixture.Renderer.Prepare([node], 32, 32, 1));

        Assert.Equal(5f, span);
        Assert.Equal(1.5f, minimumX);
        Assert.All(colors, color => Assert.Equal(new Vector4(0, 1, 0, 1), color));
        Assert.IsType<NotSupportedException>(failure);
    }

    /// <summary>Creates the explicit flat-cap bevel-join managed stroke policy used by these tests.</summary>
    /// <param name="factory">The borrowed native geometry factory.</param>
    /// <returns>The owned lazy native stroke style.</returns>
    private static StrokeStyle CreateStyle(D2DFactory factory) => new(factory, new StrokeStyleProperties {
        StartCap = CapStyle.Flat, EndCap = CapStyle.Flat, DashCap = CapStyle.Flat,
        DashStyle = DashStyle.Solid, LineJoin = LineJoin.Bevel, MiterLimit = 1
    });

    /// <summary>Builds existing connected line commands without fill, closure, smoothing or duplicate removal.</summary>
    /// <param name="points">The nonempty local vector in its supplied order.</param>
    /// <returns>One derived open Figure.</returns>
    private static List<Figure> OpenFigure(params Vector2[] points) {
        var figure = new Figure(points[0], false, false);
        for (var index = 1; index < points.Length; index++)
            figure.AddSegment(new LineSegment(points[index]), true, false);
        return [figure];
    }

    /// <summary>Publishes the exact physical Screen stroke through an actual existing path core.</summary>
    /// <param name="core">The actual node core.</param>
    /// <param name="factory">The fixture-owned factory retained through native path use.</param>
    /// <param name="points">The already physical local points.</param>
    private static void Configure(PathRenderCore2D core, D2DFactory factory, params Vector2[] points) {
        using var context = new D2DDeviceContext();
        core.StrokeBrush = new SolidColorBrush(context, new Vector4(BitConverter.Int32BitsToSingle(0x3f547ae1),
            BitConverter.Int32BitsToSingle(0x3d8f5c29), BitConverter.Int32BitsToSingle(0x3d8f5c29), 1));
        core.StrokeWidth = 3;
        core.StrokeStyle = CreateStyle(factory);
        core.Figures = OpenFigure(points);
    }

    /// <summary>Checks a test point against actual native output triangles, ignoring zero-area triangles.</summary>
    /// <param name="triangles">The native triangle point stream.</param>
    /// <param name="point">The strict interior or negative-space sample.</param>
    /// <returns>Whether a nondegenerate native triangle covers the sample.</returns>
    private static bool Covers(Vector2[] triangles, Vector2 point) {
        for (var index = 0; index < triangles.Length; index += 3) {
            var a = triangles[index];
            var b = triangles[index + 1];
            var c = triangles[index + 2];
            if (Cross(b - a, c - a) == 0) continue;
            var ab = Cross(b - a, point - a);
            var bc = Cross(c - b, point - b);
            var ca = Cross(a - c, point - c);
            if ((ab >= 0 && bc >= 0 && ca >= 0) || (ab <= 0 && bc <= 0 && ca <= 0)) return true;
        }
        return false;
    }

    /// <summary>Computes a signed test-triangle edge product.</summary>
    /// <param name="left">The first test edge.</param>
    /// <param name="right">The second test edge.</param>
    /// <returns>The signed scalar cross product.</returns>
    private static float Cross(Vector2 left, Vector2 right) => left.X * right.Y - left.Y * right.X;

    /// <summary>Reads one tightly packed RGBA test pixel.</summary>
    /// <param name="pixels">The captured packed pixels.</param>
    /// <param name="width">The captured physical width.</param>
    /// <param name="x">The physical column.</param>
    /// <param name="y">The physical row.</param>
    /// <returns>The four RGBA bytes.</returns>
    private static byte[] Pixel(byte[] pixels, uint width, int x, int y) =>
        pixels.AsSpan(checked((y * (int) width + x) * 4), 4).ToArray();

    /// <summary>Produces a real deferred native sink error for the cache recovery regression.</summary>
    private sealed class InvalidFigureSegment : Segment {
        /// <summary>Starts a second figure before ending the first, leaving native Close to report the error.</summary>
        /// <param name="sink">The actual native sink.</param>
        public override void Create(GeometrySink sink) => sink.BeginFigure(new Vector2(8, 8), FigureBegin.Hollow);
    }

    /// <summary>Owns the real WARP Scene2D transport; no native window or substitute renderer is created.</summary>
    private sealed class SpriteFixture : IDisposable {
        /// <summary>The native WARP device.</summary>
        private readonly SilkD3D12Device device;

        /// <summary>The real resource descriptor heap.</summary>
        private readonly SilkD3D12DescriptorHeap resourceHeap;

        /// <summary>The real sampler descriptor heap.</summary>
        private readonly SilkD3D12DescriptorHeap samplerHeap;

        /// <summary>The existing upload and resource owner.</summary>
        private readonly SilkD3D12ResourceManager resources;

        /// <summary>Gets the actual existing renderer whose prepared output is under test.</summary>
        internal D3D12Scene2DRenderer Renderer { get; }

        /// <summary>Gets the explicit style factory retained for every configured test path.</summary>
        internal D2DFactory PathFactory { get; } = new();

        /// <summary>Allocates the actual WARP transport without presenting a window.</summary>
        internal SpriteFixture() {
            device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
            resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 512, true);
            samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 40, true);
            resources = new SilkD3D12ResourceManager(device, resourceHeap);
            Renderer = new D3D12Scene2DRenderer(device, resources, resourceHeap, samplerHeap);
        }

        /// <summary>Records prepared draws through the actual Sprite2D PSO and reads their fenced target.</summary>
        /// <param name="width">The physical target width.</param>
        /// <param name="height">The physical target height.</param>
        /// <returns>Tightly packed RGBA bytes.</returns>
        internal byte[] Render(uint width, uint height) {
            using var queue = device.CreateCommandQueue();
            using var context = device.CreateCommandContext();
            using var fence = device.CreateFence();
            using var catalog = new D3D12ScenePassCatalog(device,
                Format.FormatR8G8B8A8Unorm, Format.FormatD32FloatS8X24Uint);
            using var heap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
            using var view = heap.Allocate();
            using var target = device.CreateRenderTargetTexture2D(width, height, Format.FormatR8G8B8A8Unorm);
            device.CreateRenderTargetView(target, view);
            var footprint = device.GetCopyableFootprint(target, out var totalBytes);
            using var readback = device.CreateBuffer(totalBytes, HeapType.Readback);
            context.Reset();
            context.ClearRenderTarget(target, view, [0, 0, 1, 1]);
            var recorded = Renderer.RenderPrepared(context, catalog.ResolveSprite2D(), view, width, height);
            context.Transition(target, ResourceStates.CopySource);
            context.CopyTextureToBuffer(readback, target, in footprint);
            context.Close();
            queue.Execute(context);
            fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));
            device.ThrowIfDeviceRemoved();
            var result = new byte[checked((int) (width * height * 4))];
            for (uint row = 0; row < height; row++) {
                var bytes = readback.Read(checked((int) width * 4),
                    footprint.Offset + (ulong) row * footprint.Footprint.RowPitch);
                bytes.CopyTo(result, checked((int) (row * width * 4)));
            }
            Assert.Equal(Renderer.Draws.Count, recorded);
            return result;
        }

        /// <summary>Releases the renderer before its borrowed native transport owners.</summary>
        public void Dispose() {
            Renderer.Dispose();
            PathFactory.Dispose();
            resources.Dispose();
            samplerHeap.Dispose();
            resourceHeap.Dispose();
            device.Dispose();
        }
    }
}
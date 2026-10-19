using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model.Scene2D;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Shaders;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using CullMode = HelixToolkit.SharpDX.Core.Native.CullMode;
using Matrix = Silk.NET.Maths.Matrix4X4<float>;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace SilkCore.Tests;

/// <summary>Exercises actual offscreen BGRA coverage through the productive WARP scene and effect passes.</summary>
public sealed class NativePremultipliedOutputTests {
    /// <summary>The small physical extent shared by these deterministic pixel tests.</summary>
    private const uint Size = 16;

    /// <summary>Checks zero, half and full alpha for ordinary blending, weighted OIT and depth peeling.</summary>
    /// <param name="transparency">Zero selects ordinary draws, one weighted OIT, two depth peeling.</param>
    /// <param name="fxaa">Whether to execute both productive FXAA passes.</param>
    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [Trait("Category", "Warp")]
    public void WarpOverlappingLayersPreserveCoverage(int transparency, bool fxaa) {
        foreach (var alpha in new[] { 0f, 0.5f, 1f }) {
            var pixels = RenderPixels(transparency, fxaa, alpha, overlap: true);
            var expected = (int) MathF.Round((1 - (1 - alpha) * (1 - alpha)) * 255);
            var center = checked((int) ((Size / 2 * Size + Size / 2) * 4));
            Assert.InRange(pixels[center + 2], Math.Max(0, expected - 4), Math.Min(255, expected + 4));
            Assert.InRange(pixels[center + 3], Math.Max(0, expected - 4), Math.Min(255, expected + 4));
            AssertPremultiplied(pixels);
            for (var index = 0; index < pixels.Length; index += 4)
                Assert.InRange(Math.Abs(pixels[index + 2] - pixels[index + 3]), 0, 2);
        }
    }

    /// <summary>Checks that Scene2D after FXAA contributes straight input color with source-over coverage.</summary>
    /// <param name="fxaa">Whether to execute FXAA before the overlay.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Warp")]
    public void WarpScene2DAfterEffectsPreservesCoverage(bool fxaa) {
        var pixels = RenderPixels(0, fxaa, 0.5f, overlay: true);
        var center = checked((int) ((Size / 2 * Size + Size / 2) * 4));
        Assert.InRange(pixels[center], 0, 1);
        Assert.InRange(pixels[center + 1], 126, 130);
        Assert.InRange(pixels[center + 2], 62, 66);
        Assert.InRange(pixels[center + 3], 189, 194);
        AssertPremultiplied(pixels);
    }

    /// <summary>Checks premultiplied bloom extraction, downsampled blur and BGRA composition on transparent pixels.</summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpBloomPreservesPremultipliedCoverage() {
        var pixels = RenderPixels(0, true, 0.5f, bloom: true);
        AssertPremultiplied(pixels);
        Assert.Contains(pixels.Where((_, index) => index % 4 == 3), value => value > 0 && value < 255);
    }

    /// <summary>Checks DirectWrite glyph coverage is multiplied once and remains independent of prior FXAA.</summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpGlyphCoverageRemainsPremultiplied() {
        var pixels = RenderPixels(0, true, 0, glyph: true);
        AssertPremultiplied(pixels);
        var painted = 0;
        for (var index = 0; index < pixels.Length; index += 4) {
            Assert.InRange(pixels[index + 3], 0, 128);
            Assert.InRange(Math.Abs(pixels[index + 1] - pixels[index + 3]), 0, 1);
            if (pixels[index + 3] > 0) painted++;
        }

        Assert.True(painted > 0);
    }

    /// <summary>Checks BGRA outline masks, both blur directions and composition preserve transparent coverage.</summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpOutlinePreservesPremultipliedCoverage() {
        var pixels = RenderPixels(0, true, 0.5f, outline: true);
        AssertPremultiplied(pixels);
        var expanded = 0;
        for (var y = 0; y < Size; y++)
            if (pixels[y * Size * 4 + 3] > 0)
                expanded++;
        Assert.True(expanded > 0);
    }

    /// <summary>Checks the legacy zero-alpha additive XRay producer has explicit coverage in the new profile.</summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpXRayProducesVisibleCoverage() {
        var pixels = RenderPixels(0, false, 0, xray: true);
        AssertPremultiplied(pixels);
        var center = checked((int) ((Size / 2 * Size + Size / 2) * 4));
        Assert.InRange(pixels[center + 2], 126, 130);
        Assert.InRange(pixels[center + 3], 126, 130);
    }

    /// <summary>Checks productive mesh pass hits avoid rasterizer boxing and array/string/closure allocation.</summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpWarmMeshPassCacheDoesNotAllocate() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(driverType: SilkDriverType.Warp);
        using var catalog = new D3D12ScenePassCatalog(
            device, Format.FormatB8G8R8A8Unorm,
            Format.FormatD32FloatS8X24Uint, outputMode: D3D12ColorOutputMode.Premultiplied
        );
        using var node = CreateQuad(0.5f, 0.3f);
        node.Attach();
        for (var index = 0; index < 64; index++) catalog.Resolve(node);
        var expected = catalog.Resolve(node);
        Assert.NotNull(expected);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 1_000; index++) catalog.Resolve(node);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, allocated);
        Assert.Same(expected, catalog.Resolve(node));
    }

    /// <summary>Reads the captured input, luma intermediate and filtered output to prove original alpha survives.</summary>
    /// <param name="alpha">The consumed original coverage, independent of luminance.</param>
    [Theory]
    [InlineData(0f)]
    [InlineData(0.003921569f)]
    [InlineData(0.5f)]
    [InlineData(1f)]
    [Trait("Category", "Warp")]
    public void WarpLumaAndFxaaConsumeOriginalCoverage(float alpha) {
        using var device = SilkD3D12DeviceFactory.CreateDefault(driverType: SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var resources = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 512, true);
        using var samplers = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 40, true);
        using var post = new SilkD3D12PostProcessResources(
            device, resources, samplers, null,
            Format.FormatB8G8R8A8Unorm, D3D12ColorOutputMode.Premultiplied
        );
        using var catalog = new D3D12ScenePassCatalog(
            device, Format.FormatB8G8R8A8Unorm,
            Format.FormatD32FloatS8X24Uint, outputMode: D3D12ColorOutputMode.Premultiplied
        );
        using var rtvs = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        using var rtv = rtvs.Allocate();
        using var target = device.CreateRenderTargetTexture2D(Size, Size, Format.FormatB8G8R8A8Unorm);
        device.CreateRenderTargetView(target, rtv);
        var footprint = device.GetCopyableFootprint(target, out var bytes);
        using var input = device.CreateBuffer(bytes, HeapType.Readback);
        using var luma = device.CreateBuffer(bytes, HeapType.Readback);
        using var output = device.CreateBuffer(bytes, HeapType.Readback);
        var transforms = CreateTransforms();
        var parameters = SilkD3D12SceneRenderer.CreateFxaaParameters(FXAALevel.High, Size, Size);
        context.Reset();
        context.ClearRenderTarget(target, rtv, [0.125f * alpha, 0.5f * alpha, 0.875f * alpha, alpha]);
        post.Capture(context, target, Size, Size);
        Assert.Equal(Format.FormatB8G8R8A8Unorm, post.First.Description.Format);
        Assert.Equal(Format.FormatB8G8R8A8Unorm, post.Second.Description.Format);
        context.Transition(post.First, ResourceStates.CopySource);
        context.CopyTextureToBuffer(input, post.First, in footprint);
        post.Draw(
            context, catalog.ResolvePostProcess(
                DefaultRenderTechniqueNames.PostEffectFxaa,
                DefaultPassNames.LumaPass
            ), 0, 1, in transforms, in parameters
        );
        context.Transition(post.Second, ResourceStates.CopySource);
        context.CopyTextureToBuffer(luma, post.Second, in footprint);
        context.Transition(target, ResourceStates.RenderTarget);
        post.DrawToPresentation(
            context, catalog.ResolvePostProcess(
                DefaultRenderTechniqueNames.PostEffectFxaa,
                DefaultPassNames.FxaaPass
            ), 1, rtv, in transforms, in parameters
        );
        context.Transition(target, ResourceStates.CopySource);
        context.CopyTextureToBuffer(output, target, in footprint);
        context.Close();
        queue.Execute(context);
        fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(10));
        device.ThrowIfDeviceRemoved();
        var original = input.Read(4, footprint.Offset);
        Assert.Equal(original, luma.Read(4, footprint.Offset));
        Assert.Equal(original, output.Read(4, footprint.Offset));
        Assert.InRange(original[3], Math.Max(0, (int) (alpha * 255) - 1), Math.Min(255, (int) (alpha * 255) + 1));
    }

    /// <summary>Checks desktop RGB remains opaque even when DXGI supplies zero alpha bytes.</summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpDesktopAlphaIsNotMistakenForSceneCoverage() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(driverType: SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var resources = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 512, true);
        using var samplers = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 40, true);
        using var capture = new SilkD3D12ScreenCaptureResources(
            device, resources, samplers,
            D3D12ColorOutputMode.Premultiplied
        );
        using var core = new ScreenCloneRenderCore(static () => new OpaqueDesktopSource());
        using var catalog = new D3D12ScenePassCatalog(
            device, Format.FormatB8G8R8A8Unorm,
            Format.FormatD32FloatS8X24Uint, outputMode: D3D12ColorOutputMode.Premultiplied
        );
        using var rtvs = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        using var rtv = rtvs.Allocate();
        using var target = device.CreateRenderTargetTexture2D(Size, Size, Format.FormatB8G8R8A8Unorm);
        device.CreateRenderTargetView(target, rtv);
        var footprint = device.GetCopyableFootprint(target, out var bytes);
        using var readback = device.CreateBuffer(bytes, HeapType.Readback);
        context.Reset();
        context.ClearRenderTarget(target, rtv, [0, 0, 0, 0]);
        context.SetRenderTarget(rtv);
        context.SetViewport(Size, Size);
        Assert.True(
            capture.TryRender(
                context, catalog.ResolvePostProcess(
                    DefaultRenderTechniqueNames.ScreenDuplication,
                    DefaultPassNames.Default
                ), core, Size, Size
            )
        );
        context.Transition(target, ResourceStates.CopySource);
        context.CopyTextureToBuffer(readback, target, in footprint);
        context.Close();
        queue.Execute(context);
        fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(10));
        device.ThrowIfDeviceRemoved();
        Assert.Equal(new byte[] { 0, 0, 255, 255 }, readback.Read(4, footprint.Offset));
    }

    /// <summary>Records real mesh, optional transparency, bloom, FXAA and Scene2D passes into one BGRA image.</summary>
    /// <param name="transparency">The transparency stage selection.</param>
    /// <param name="fxaa">Whether to filter the completed 3D image.</param>
    /// <param name="alpha">The straight material alpha.</param>
    /// <param name="overlap">Whether to draw a second equal-color layer.</param>
    /// <param name="overlay">Whether to add a translucent green Scene2D rectangle after FXAA.</param>
    /// <param name="bloom">Whether to run bloom before FXAA.</param>
    /// <param name="glyph">Whether to draw a half-alpha green glyph after FXAA.</param>
    /// <param name="outline">Whether to render and blur an outline mask before FXAA.</param>
    /// <param name="xray">Whether to draw a red XRay layer behind an explicit occluder depth.</param>
    /// <returns>Tightly packed, fence-completed premultiplied BGRA pixels.</returns>
    private static byte[] RenderPixels(
        int transparency,
        bool fxaa,
        float alpha,
        bool overlap = false,
        bool overlay = false,
        bool bloom = false,
        bool glyph = false,
        bool outline = false,
        bool xray = false
    ) {
        using var device = SilkD3D12DeviceFactory.CreateDefault(driverType: SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var resources = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 2048, true);
        using var samplers = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 128, true);
        using var renderer = new SilkD3D12SceneRenderer(
            device, resources, samplers,
            Format.FormatB8G8R8A8Unorm, D3D12ColorOutputMode.Premultiplied
        );
        using var catalog = new D3D12ScenePassCatalog(
            device, Format.FormatB8G8R8A8Unorm,
            Format.FormatD32FloatS8X24Uint, outputMode: D3D12ColorOutputMode.Premultiplied
        );
        using var rtvs = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        using var rtv = rtvs.Allocate();
        using var target = device.CreateRenderTargetTexture2D(Size, Size, Format.FormatB8G8R8A8Unorm);
        using var dsvs = device.CreateDescriptorHeap(DescriptorHeapType.Dsv, 1);
        using var dsv = dsvs.Allocate();
        using var depth = device.CreateDepthStencilTexture2D(Size, Size, Format.FormatD32FloatS8X24Uint);
        device.CreateRenderTargetView(target, rtv);
        device.CreateDepthStencilView(depth, dsv);
        using var back = CreateQuad(alpha, 0.6f);
        using var front = CreateQuad(alpha, 0.3f);
        var nodes = new FastList<SceneNode> { back };
        if (overlap) nodes.Add(front);
        var transforms = CreateTransforms();
        var frustum = new BoundingFrustum(Matrix.Identity);
        renderer.BeginFrame();
        context.Reset();
        context.ClearRenderTarget(target, rtv, [0, 0, 0, 0]);
        context.ClearDepthStencil(depth, dsv);
        context.SetRenderTargets(rtv, dsv);
        context.SetViewport(Size, Size);
        var recorded = transparency switch {
            1 => renderer.RenderWeightedTransparency(
                context, nodes, catalog.ResolveWeightedTransparency,
                catalog.ResolveWeightedComposition(), in transforms, false, ref frustum, rtv, dsv, Size, Size,
                null, null, catalog.ResolveBoneSkinning, null
            ),
            2 => renderer.RenderDepthPeeling(
                context, nodes, catalog.ResolveDepthPeelingInitialization,
                catalog.ResolveDepthPeeling, catalog.ResolveDepthPeelingComposition(), 4, in transforms,
                false, ref frustum, target, rtv, dsv, Size, Size, null, null, catalog.ResolveBoneSkinning, null
            ),
            _ => renderer.RenderVisible(context, nodes, catalog.Resolve, in transforms, false, ref frustum)
        };
        Assert.True(recorded > 0);
        if (bloom) {
            var effect = new BorderEffectStruct {
                Color = new(0.1f, 0.1f, 0.1f, 0.1f), Param = new Matrix { M11 = 1, M12 = 1, M13 = 1, M14 = 1 }, ViewportScale = 1
            };
            renderer.RenderBloom(
                context, target, rtv,
                catalog.ResolvePostProcess(DefaultRenderTechniqueNames.PostEffectBloom, DefaultPassNames.ScreenQuad),
                catalog.ResolvePostProcess(DefaultRenderTechniqueNames.PostEffectBloom, DefaultPassNames.EffectBlurVertical),
                catalog.ResolvePostProcess(DefaultRenderTechniqueNames.PostEffectBloom, DefaultPassNames.EffectBlurHorizontal),
                catalog.ResolvePostProcess(DefaultRenderTechniqueNames.PostEffectBloom, DefaultPassNames.MeshOutline),
                in effect, 1, in transforms, Size, Size
            );
        }

        if (outline) {
            var effect = new BorderEffectStruct { Color = new(1, 0, 0, 0.5f), Param = new Matrix { M11 = 1, M12 = 1 }, ViewportScale = 1 };
            Assert.True(
                renderer.RenderOutline(
                    context, nodes,
                    node => catalog.ResolvePostEffectGeometry(
                        node, DefaultPassNames.EffectOutlineP1,
                        Format.FormatD32FloatS8X24Uint
                    ),
                    catalog.ResolvePostProcess(
                        DefaultRenderTechniqueNames.PostEffectMeshOutlineBlur,
                        DefaultPassNames.EffectBlurVertical
                    ),
                    catalog.ResolvePostProcess(
                        DefaultRenderTechniqueNames.PostEffectMeshOutlineBlur,
                        DefaultPassNames.EffectBlurHorizontal
                    ),
                    catalog.ResolvePostProcess(
                        DefaultRenderTechniqueNames.PostEffectMeshOutlineBlur,
                        DefaultPassNames.MeshOutline
                    ),
                    in effect, 1, in transforms, false, ref frustum, target, rtv, dsv, Size, Size
                ) > 0
            );
        }

        if (xray) {
            context.ClearDepthStencil(depth, dsv, 0.1f, 0);
            var effect = new BorderEffectStruct { Color = new(1, 0, 0, 0.5f), Param = default };
            Func<SceneNode, ShaderPass?>[] selectors = [
                node => catalog.ResolvePostEffectGeometry(
                    node, DefaultPassNames.EffectMeshXRayP2,
                    Format.FormatD32FloatS8X24Uint, 0
                )
            ];
            Assert.True(
                renderer.RenderXRay(
                    context, nodes, selectors, in effect, in transforms,
                    false, ref frustum, rtv, dsv, Size, Size
                ) > 0
            );
        }

        if (fxaa)
            renderer.RenderFxaa(
                context, target, rtv,
                catalog.ResolvePostProcess(DefaultRenderTechniqueNames.PostEffectFxaa, DefaultPassNames.LumaPass),
                catalog.ResolvePostProcess(DefaultRenderTechniqueNames.PostEffectFxaa, DefaultPassNames.FxaaPass),
                FXAALevel.High, in transforms, Size, Size
            );
        using var d2d = new D2DDeviceContext();
        using var green = new SolidColorBrush(d2d, new Vector4(0, 1, 0, 0.5f));
        using var rectangle = new RectangleNode2D { Fill = green };
        if (overlay) {
            rectangle.RenderCore.LayoutBound = new RectangleF(3, 3, 10, 10);
            Assert.Equal(
                1, renderer.Render2D(
                    context, new[] { rectangle }, catalog.ResolveSprite2D(),
                    rtv, Size, Size, 1
                )
            );
        }

        using var text = new TextNode2D { Text = "M", Foreground = green, FontSize = 12 };
        if (glyph) {
            text.RenderCore.LayoutBound = new RectangleF(1, 1, 14, 14);
            ((HelixToolkit.SharpDX.Core.Core2D.TextRenderCore2D) text.RenderCore).MaxWidth = 14;
            ((HelixToolkit.SharpDX.Core.Core2D.TextRenderCore2D) text.RenderCore).MaxHeight = 14;
            Assert.Equal(1, renderer.Render2D(context, new[] { text }, catalog.ResolveSprite2D(), rtv, Size, Size, 1));
        }

        var footprint = device.GetCopyableFootprint(target, out var bytes);
        using var readback = device.CreateBuffer(bytes, HeapType.Readback);
        context.Transition(target, ResourceStates.CopySource);
        context.CopyTextureToBuffer(readback, target, in footprint);
        context.Close();
        queue.Execute(context);
        fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(10));
        device.ThrowIfDeviceRemoved();
        var pixels = new byte[Size * Size * 4];
        for (uint y = 0; y < Size; y++)
            readback.Read(checked((int) Size * 4), footprint.Offset + y * footprint.Footprint.RowPitch)
                .CopyTo(pixels, checked((int) (y * Size * 4)));
        return pixels;
    }

    /// <summary>Creates a straight-color red quad at a distinct depth for overlapping-layer tests.</summary>
    /// <param name="alpha">The public straight material alpha.</param>
    /// <param name="depth">The clip-space layer depth.</param>
    /// <returns>The initialized scene node.</returns>
    private static MeshNode CreateQuad(float alpha, float depth) => new() {
        IsTransparent = true,
        CullMode = CullMode.None,
        FrontCcw = false,
        Material = new DiffuseMaterialCore { EnableUnLit = true, DiffuseColor = new Vector4(1, 0, 0, alpha) },
        Geometry = new MeshGeometry3D {
            Positions = new Vector3Collection(
                [
                    new(-0.8f, 0.8f, depth), new(0.8f, 0.8f, depth),
                    new(-0.8f, -0.8f, depth), new(0.8f, -0.8f, depth)
                ]
            ),
            Normals = new Vector3Collection([Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ]),
            Indices = new IntCollection([0, 1, 2, 2, 1, 3])
        }
    };

    /// <summary>Creates unchanged camera constants with a valid original OIT weight mode.</summary>
    /// <returns>The frame payload copied and tagged by the renderer.</returns>
    private static GlobalTransformStruct CreateTransforms() => new() {
        View = Matrix.Identity,
        Projection = Matrix.Identity,
        ViewProjection = Matrix.Identity,
        Viewport = new Vector4(Size, Size, 1f / Size, 1f / Size),
        Resolution = new Vector4(Size, Size, 1f / Size, 1f / Size),
        EyePos = new Vector3(0, 0, 2),
        OITWeightPower = 3,
        OITWeightDepthSlope = 1,
        OITWeightMode = (int) OITWeightMode.Linear1,
        DpiScale = 1
    };

    /// <summary>Checks every actual pixel, including transparent background and filtered shape boundaries.</summary>
    /// <param name="pixels">The tightly packed premultiplied BGRA result.</param>
    private static void AssertPremultiplied(byte[] pixels) {
        for (var index = 0; index < pixels.Length; index += 4)
        for (var channel = 0; channel < 3; channel++)
            Assert.InRange(pixels[index + channel], 0, pixels[index + 3]);
    }

    /// <summary>Supplies a desktop red pixel whose alpha byte is intentionally undefined/zero.</summary>
    private sealed class OpaqueDesktopSource : IDesktopCaptureSource {
        /// <summary>The CPU-owned test desktop frame.</summary>
        private readonly ScreenCaptureFrame frame = new(1, 1, Format.FormatB8G8R8A8Unorm, 4, [0, 0, 255, 0]);

        /// <inheritdoc />
        public void Start(int output) { }

        /// <inheritdoc />
        public bool TryAcquire(TimeSpan timeout, out ScreenCaptureFrame frame) {
            frame = this.frame;
            return true;
        }

        /// <inheritdoc />
        public void Dispose() { }
    }
}
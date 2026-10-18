using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Scene;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Matrix = Silk.NET.Maths.Matrix4X4<float>;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace HelixToolkit.SharpDX.Core.Native;

/// Exercises source selection effects under treatment-sized descriptor pressure through actual offscreen GPU output.
public sealed class NativeSelectionDescriptorPressureTests {
    /// <summary>
    ///     Preserves distinct effect constants over two pages beside one hundred ordinary scene bindings, then
    ///     reuses the pages across deselection and live color changes without affecting scene depth or stencil.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpTreatmentSelectionSpansPagesAndReusesCompletedFrameSlots() {
        using var fixture = new Fixture();
        var scene = new FastList<SceneNode>();
        for (var index = 0; index < 100; index++) scene.Add(Rectangle(80, 80, 84, 84));
        scene[0].Dispose();
        scene[0] = Rectangle(8, 16, 24, 48);
        scene[79].Dispose();
        scene[79] = Rectangle(40, 16, 56, 48);
        scene[98].Dispose();
        scene[98] = Rectangle(28, 52, 36, 60, 0.1f, new Vector4(0, 0, 1, 1));
        scene[99].Dispose();
        scene[99] = Rectangle(0, 0, 64, 64, 0.25f, new Vector4(0, 1, 0, 1));
        var selected = new FastList<SceneNode>(scene.Take(80));
        using var glow = new NodePostEffectMeshOutlineBlur {
            EffectName = "glow", NumberOfBlurPass = 6, Color = new Vector4(1, 1, 0, 1)
        };
        using var border = new NodePostEffectBorderHighlight { EffectName = "border", NumberOfBlurPass = 1 };
        using var xray = new NodePostEffectXRay {
            EffectName = "xrayLine", EnableDoublePass = false, OutlineFadingFactor = 0
        };
        var effects = new FastList<SceneNode> { glow, border, xray };
        try {
            foreach (var node in selected.Cast<MeshNode>()) node.PostEffects = Effects("#FF0000");
            ((MeshNode)selected[79]).PostEffects = Effects("#0000FF");
            var first = fixture.Render(scene, selected, effects, scene[98]);
            Assert.Equal(100, fixture.SceneDrawCount);
            Assert.Equal(240, fixture.EffectDrawCount);
            Assert.Equal(2, fixture.Renderer.EffectBindings.PageCount);
            var resourceCount = fixture.Renderer.EffectBindings.ResourceDescriptorCount;
            var samplerCount = fixture.Renderer.EffectBindings.SamplerDescriptorCount;
            Assert.True(resourceCount > D3D12EffectBindingPool.ResourcePageCapacity);
            AssertColor(first, 16, 32, 0);
            AssertColor(first, 48, 32, 2);
            AssertColor(first, 32, 56, 2);
            for (var frame = 0; frame < 6; frame++) {
                var deselected = frame % 3 == 0;
                ((MeshNode)selected[0]).PostEffects = Effects(frame % 2 == 0 ? "#0000FF" : "#FF0000");
                ((MeshNode)selected[79]).PostEffects = Effects(frame % 2 == 0 ? "#FF0000" : "#0000FF");
                var active = deselected ? new FastList<SceneNode>() : selected;
                var pixels = fixture.Render(scene, active, effects, scene[98]);
                Assert.Equal(deselected ? 0 : 240, fixture.EffectDrawCount);
                Assert.Equal(2, fixture.Renderer.EffectBindings.PageCount);
                Assert.Equal(resourceCount, fixture.Renderer.EffectBindings.ResourceDescriptorCount);
                Assert.Equal(samplerCount, fixture.Renderer.EffectBindings.SamplerDescriptorCount);
                if (!deselected) {
                    AssertColor(pixels, 16, 32, frame % 2 == 0 ? 2 : 0);
                    AssertColor(pixels, 48, 32, frame % 2 == 0 ? 0 : 2);
                }
                AssertColor(pixels, 32, 56, 2);
            }
        } finally {
            foreach (var node in scene) node.Dispose();
        }
    }

    /// Verifies a fresh compute dispatch restores effect-page heaps before its indirect XRay line draw.
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpFreshIntersectionComputeRestoresEffectPageBeforeIndirectDraw() {
        using var fixture = new Fixture();
        using var node = new GpuLineNode();
        node.Material = new LineMaterialCore { LineColor = new Vector4(1, 0, 0, 1), Thickness = 3 };
        node.PostEffects = "xrayLine";
        var first = new IntersectionMeshSnapshot([
            new Vector3(-0.8f, -0.8f, 0.5f), new Vector3(0.8f, -0.8f, 0.5f), new Vector3(0, 0.8f, 0.5f)
        ], [0, 1, 2]);
        var second = new IntersectionMeshSnapshot([
            new Vector3(-0.5f, 0, 0), new Vector3(0.5f, 0, 0), new Vector3(0, 0, 1)
        ], [0, 1, 2]);
        var core = (MeshIntersectionRenderCore)node.RenderCore;
        core.SetInputs([new IntersectionDispatchInput(first, second, Matrix.Identity, Matrix.Identity,
            [new IntersectionCandidate(0, 0, 77)])]);
        using var occluder = Rectangle(0, 0, 64, 64, 0.25f, new Vector4(0, 0, 1, 1));
        using var xray = new NodePostEffectXRay {
            EffectName = "xrayLine", EnableDoublePass = false, OutlineFadingFactor = 0
        };
        var pixels = fixture.Render(new FastList<SceneNode> { occluder },
            new FastList<SceneNode> { node }, new FastList<SceneNode> { xray });
        Assert.Equal(1, fixture.EffectDrawCount);
        var pixelOffset = (32 * 64 + 32) * 4;
        Assert.InRange(pixels[pixelOffset], 200, 255);
        Assert.InRange(pixels[pixelOffset + 1], 0, 40);
        Assert.InRange(pixels[pixelOffset + 2], 200, 255);
        var native = fixture.Renderer.FindIntersectionResources(core);
        Assert.NotNull(native);
        Assert.Equal(1, fixture.Renderer.EffectBindings.PageCount);
    }

    /// <summary>Creates the original three parsed selection effect names with an independent per-node color.</summary>
    /// <param name="color">The source HTML color attribute.</param>
    /// <returns>The source-compatible attribute string.</returns>
    private static string Effects(string color) =>
        $"glow[color:{color}];border[color:{color}];xrayLine[color:{color}]";

    /// <summary>Checks a physical pixel's selected red or blue channel remains distinct after every recorded draw.</summary>
    /// <param name="pixels">The packed fence-completed RGBA image.</param>
    /// <param name="x">The pixel column.</param>
    /// <param name="y">The pixel row.</param>
    /// <param name="channel">The expected red or blue channel.</param>
    private static void AssertColor(byte[] pixels, int x, int y, int channel) {
        var offset = (y * 64 + x) * 4;
        Assert.InRange(pixels[offset + channel], 200, 255);
        Assert.InRange(pixels[offset + 2 - channel], 0, 40);
    }

    /// <summary>Creates a real indexed rectangle in analytic clip space.</summary>
    /// <param name="left">The left pixel boundary.</param>
    /// <param name="top">The top pixel boundary.</param>
    /// <param name="right">The right pixel boundary.</param>
    /// <param name="bottom">The bottom pixel boundary.</param>
    /// <param name="depth">The projected depth.</param>
    /// <param name="color">The optional unlit material color.</param>
    /// <returns>The owned mesh node.</returns>
    private static MeshNode Rectangle(int left, int top, int right, int bottom, float depth = 0.75f,
        Vector4? color = null) => new() {
        Geometry = new MeshGeometry3D {
            Positions = new Vector3Collection([
                new(left / 32f - 1, 1 - top / 32f, depth), new(right / 32f - 1, 1 - top / 32f, depth),
                new(left / 32f - 1, 1 - bottom / 32f, depth), new(right / 32f - 1, 1 - bottom / 32f, depth)
            ]),
            Normals = new Vector3Collection([Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ]),
            Indices = new IntCollection([0, 1, 2, 2, 1, 3])
        },
        Material = new DiffuseMaterialCore { EnableUnLit = true, DiffuseColor = color ?? Vector4.One },
        CullMode = CullMode.None, FrontCcw = false
    };

    /// Uses ordinary line-node ownership while drawing actual compute-produced segments.
    private sealed class GpuLineNode : LineNode {
        /// Creates valid selection bounds; all rendered segment data is produced by the compute kernel.
        internal GpuLineNode() {
            Geometry = new LineGeometry3D {
                Positions = new Vector3Collection([new Vector3(-1), new Vector3(1)]), Indices = new IntCollection([0, 1])
            };
        }

        /// <summary>Creates the productive selected-pair compute core.</summary>
        /// <returns>The GPU intersection render core.</returns>
        protected override RenderCore OnCreateRenderCore() => new MeshIntersectionRenderCore();
    }

    /// Owns true offscreen targets and native queues, without a window, swap chain or application.
    private sealed class Fixture : IDisposable {
        /// The software Direct3D12 adapter used deterministically by the headless tests.
        private readonly SilkD3D12Device device =
            SilkD3D12DeviceFactory.CreateDefault(driverType: SilkDriverType.Warp);
        /// The native command queue.
        private readonly SilkD3D12CommandQueue queue;
        /// The reusable command list and allocator.
        private readonly SilkD3D12CommandContext context;
        /// The completion fence governing every table reuse.
        private readonly SilkD3D12Fence fence;
        /// The registered original shader catalog.
        private readonly D3D12ScenePassCatalog catalog;
        /// The ordinary scene resource heap, deliberately matching production capacity.
        private readonly SilkD3D12DescriptorHeap resources;
        /// The ordinary scene sampler heap.
        private readonly SilkD3D12DescriptorHeap samplers;
        /// The render-target descriptor owner.
        private readonly SilkD3D12DescriptorHeap targets;
        /// The depth/stencil descriptor owner.
        private readonly SilkD3D12DescriptorHeap depths;
        /// The native color texture.
        private readonly SilkD3D12Resource target;
        /// The original scene depth and stencil texture.
        private readonly SilkD3D12Resource depth;
        /// The target view.
        private readonly SilkD3D12Descriptor targetView;
        /// The scene depth/stencil view.
        private readonly SilkD3D12Descriptor depthView;

        /// Initializes the actual productive renderer and native resources for a 64-square-pixel frame.
        internal Fixture() {
            queue = device.CreateCommandQueue();
            context = device.CreateCommandContext();
            fence = device.CreateFence();
            catalog = new D3D12ScenePassCatalog(device, Format.FormatR8G8B8A8Unorm,
                Format.FormatD32FloatS8X24Uint);
            resources = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 16_384, true);
            samplers = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 2_048, true);
            Renderer = new SilkD3D12SceneRenderer(device, resources, samplers);
            targets = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
            depths = device.CreateDescriptorHeap(DescriptorHeapType.Dsv, 1);
            targetView = targets.Allocate();
            depthView = depths.Allocate();
            target = device.CreateRenderTargetTexture2D(64, 64, Format.FormatR8G8B8A8Unorm);
            depth = device.CreateDepthStencilTexture2D(64, 64, Format.FormatD32FloatS8X24Uint);
            device.CreateRenderTargetView(target, targetView);
            device.CreateDepthStencilView(depth, depthView);
        }

        /// Gets the real renderer and its descriptor pool for bounded lifetime assertions.
        internal SilkD3D12SceneRenderer Renderer { get; }
        /// Gets the ordinary draw count from the last command list.
        internal int SceneDrawCount { get; private set; }
        /// Gets the actual effect geometry draw count from the last command list.
        internal int EffectDrawCount { get; private set; }

        /// <summary>Renders source effects, checks preserved depth/stencil, and reads back the actual completed image.</summary>
        /// <param name="scene">The original ordinary scene nodes.</param>
        /// <param name="selected">The selected geometry candidates.</param>
        /// <param name="effects">The original ordered source effect nodes.</param>
        /// <param name="afterEffects">An optional ordinary draw after an effect page switch.</param>
        /// <returns>The packed RGBA pixels read from the native GPU target.</returns>
        internal byte[] Render(FastList<SceneNode> scene, FastList<SceneNode> selected, FastList<SceneNode> effects,
            SceneNode? afterEffects = null) {
            Renderer.BeginFrame();
            context.Reset();
            context.Transition(target, ResourceStates.RenderTarget);
            context.Transition(depth, ResourceStates.DepthWrite);
            context.ClearRenderTarget(target, targetView, [0, 0, 0, 1]);
            context.ClearDepthStencil(depth, depthView, 1, 7);
            context.SetRenderTargets(targetView, depthView);
            context.SetViewport(64, 64);
            var transforms = new GlobalTransformStruct {
                View = Matrix.Identity, Projection = Matrix.Identity, ViewProjection = Matrix.Identity,
                Viewport = new Vector4(64, 64, 1f / 64, 1f / 64),
                Resolution = new Vector4(64, 64, 1f / 64, 1f / 64), EyePos = new Vector3(0, 0, 2), DpiScale = 1
            };
            var frustum = new BoundingFrustum(Matrix.Identity);
            SceneDrawCount = Renderer.RenderVisible(context, scene, catalog.Resolve, in transforms, false, ref frustum);
            var footprint = device.GetCopyableFootprint(target, out var colorBytes);
            var depthFootprint = device.GetCopyableFootprint(depth, 0, out var depthBytes);
            var stencilFootprint = device.GetCopyableFootprint(depth, 1, out var stencilBytes);
            using var color = device.CreateBuffer(colorBytes, HeapType.Readback);
            using var beforeDepth = device.CreateBuffer(depthBytes, HeapType.Readback);
            using var afterDepth = device.CreateBuffer(depthBytes, HeapType.Readback);
            using var beforeStencil = device.CreateBuffer(stencilBytes, HeapType.Readback);
            using var afterStencil = device.CreateBuffer(stencilBytes, HeapType.Readback);
            context.Transition(depth, ResourceStates.CopySource);
            context.CopyTextureToBuffer(beforeDepth, depth, in depthFootprint, 0);
            context.CopyTextureToBuffer(beforeStencil, depth, in stencilFootprint, 1);
            context.Transition(depth, ResourceStates.DepthWrite);
            EffectDrawCount = Renderer.RenderPostEffects(context, selected, effects, catalog, in transforms, false,
                ref frustum, target, targetView, depth, depthView, 64, 64);
            context.Transition(depth, ResourceStates.CopySource);
            context.CopyTextureToBuffer(afterDepth, depth, in depthFootprint, 0);
            context.CopyTextureToBuffer(afterStencil, depth, in stencilFootprint, 1);
            context.Transition(depth, ResourceStates.DepthWrite);
            if (afterEffects is not null)
                Renderer.RenderVisible(context, new FastList<SceneNode> { afterEffects }, catalog.Resolve,
                    in transforms, false, ref frustum);
            context.Transition(target, ResourceStates.CopySource);
            context.CopyTextureToBuffer(color, target, in footprint);
            context.Close();
            queue.Execute(context);
            fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(20));
            var originalDepth = ReadRows(beforeDepth, in depthFootprint, 256);
            var preservedDepth = ReadRows(afterDepth, in depthFootprint, 256);
            var originalStencil = ReadRows(beforeStencil, in stencilFootprint, 64);
            var preservedStencil = ReadRows(afterStencil, in stencilFootprint, 64);
            Assert.Equal(originalDepth, preservedDepth);
            Assert.Equal(originalStencil, preservedStencil);
            device.ThrowIfDeviceRemoved();
            return ReadRows(color, in footprint, 256);
        }

        /// <summary>Packs the active native rows while excluding undefined native row padding.</summary>
        /// <param name="buffer">The completed native readback.</param>
        /// <param name="footprint">The native planar footprint.</param>
        /// <param name="rowBytes">The number of meaningful bytes per row.</param>
        /// <returns>The packed 64-row plane.</returns>
        private static byte[] ReadRows(SilkD3D12Resource buffer, in PlacedSubresourceFootprint footprint, int rowBytes) {
            var result = new byte[rowBytes * 64];
            for (var row = 0; row < 64; row++) {
                var bytes = buffer.Read(rowBytes, footprint.Offset + (ulong)row * footprint.Footprint.RowPitch);
                bytes.CopyTo(result, row * rowBytes);
            }
            return result;
        }

        /// Releases draw owners before heap pages and proves both ordinary and effect tables were returned.
        public void Dispose() {
            Renderer.Dispose();
            Assert.Equal(0, resources.Count);
            Assert.Equal(0, samplers.Count);
            Assert.Equal(0, Renderer.EffectBindings.ResourceDescriptorCount);
            Assert.Equal(0, Renderer.EffectBindings.SamplerDescriptorCount);
            Assert.True(Renderer.EffectBindings.IsDisposed);
            depth.Dispose();
            target.Dispose();
            depthView.Dispose();
            targetView.Dispose();
            depths.Dispose();
            targets.Dispose();
            resources.Dispose();
            samplers.Dispose();
            catalog.Dispose();
            fence.Dispose();
            context.Dispose();
            queue.Dispose();
            device.Dispose();
        }
    }
}

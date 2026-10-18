using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Scene;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Matrix = Silk.NET.Maths.Matrix4X4<float>;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace HelixToolkit.SharpDX.Core.Native;

/// Verifies physical layer composition and exact repeated offscreen frames through the native peeling renderer.
public sealed class NativeDepthPeelingDeterminismTests {
    /// <summary>
    ///     Checks a translated plane, optional instances, live wire colors and exhausted peeling passes on WARP.
    /// </summary>
    /// <param name="iterations">The initialization and peeling pass count.</param>
    /// <param name="instanced">Whether the original indexed geometry has two independently transformed instances.</param>
    [Theory]
    [InlineData(4, false)]
    [InlineData(4, true)]
    [InlineData(5, false)]
    [InlineData(5, true)]
    [InlineData(6, false)]
    [InlineData(6, true)]
    [InlineData(8, false)]
    [InlineData(8, true)]
    [Trait("Category", "Warp")]
    public void WarpRepeatedPlaneTest(int iterations, bool instanced) =>
        Verify(SilkDriverType.Warp, iterations, instanced, false, false);

    /// Checks scene paging beyond the production heap capacity, repeated draws, resize and disposal without a window.
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpDescriptorPressureTest() =>
        Verify(SilkDriverType.Warp, 6, false, false, false, 80);

    /// <summary>
    ///     Checks unchanged hardware frames, including every boundary pixel, without presentation or a window.
    /// </summary>
    /// <param name="iterations">The initialization and peeling pass count.</param>
    /// <param name="instanced">Whether the original indexed geometry has two independently transformed instances.</param>
    [Theory(Explicit = true)]
    [InlineData(4, false)]
    [InlineData(4, true)]
    [InlineData(5, false)]
    [InlineData(5, true)]
    [InlineData(6, false)]
    [InlineData(6, true)]
    [InlineData(8, false)]
    [InlineData(8, true)]
    [Trait("Category", "Hardware")]
    public void HardwareRepeatedPlaneTest(int iterations, bool instanced) =>
        Verify(SilkDriverType.Hardware, iterations, instanced, false, false);

    /// <summary>
    ///     Checks six distinct depth layers against independent alpha composition in both submission orders on WARP.
    /// </summary>
    /// <param name="iterations">The initialization and peeling pass count.</param>
    /// <param name="reverse">Whether far geometry is submitted before near geometry.</param>
    [Theory]
    [InlineData(4, false)]
    [InlineData(4, true)]
    [InlineData(5, false)]
    [InlineData(5, true)]
    [InlineData(6, false)]
    [InlineData(6, true)]
    [InlineData(8, false)]
    [InlineData(8, true)]
    [Trait("Category", "Warp")]
    public void WarpLayerCompositionTest(int iterations, bool reverse) =>
        Verify(SilkDriverType.Warp, iterations, false, true, reverse);

    /// <summary>
    ///     Checks genuine hardware peeling of six colored layers against independent alpha composition.
    /// </summary>
    /// <param name="iterations">The initialization and peeling pass count.</param>
    /// <param name="reverse">Whether far geometry is submitted before near geometry.</param>
    [Theory(Explicit = true)]
    [InlineData(4, false)]
    [InlineData(4, true)]
    [InlineData(5, false)]
    [InlineData(5, true)]
    [InlineData(6, false)]
    [InlineData(6, true)]
    [InlineData(8, false)]
    [InlineData(8, true)]
    [Trait("Category", "Hardware")]
    public void HardwareLayerCompositionTest(int iterations, bool reverse) =>
        Verify(SilkDriverType.Hardware, iterations, false, true, reverse);

    /// <summary>
    ///     Exercises the productive scene renderer, real indexed geometry, descriptors and fence-completed readback.
    /// </summary>
    /// <param name="driver">The hardware or WARP driver.</param>
    /// <param name="iterations">The initialization and peeling pass count.</param>
    /// <param name="instanced">Whether a single plane uses two transformed instances.</param>
    /// <param name="multipleLayers">Whether six distinct depths must be consumed by three actual peeling passes.</param>
    /// <param name="reverse">Whether far layers are submitted first.</param>
    /// <param name="extraMeshes">The offscreen meshes forcing scene descriptors across a page boundary.</param>
    private static void Verify(SilkDriverType driver, int iterations, bool instanced, bool multipleLayers, bool reverse,
        int extraMeshes = 0) {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, driver);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var catalog = new D3D12ScenePassCatalog(device, Format.FormatR8G8B8A8Unorm,
            Format.FormatD32FloatS8X24Uint);
        using var resources = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 16_384, true);
        using var samplers = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 2_048, true);
        using var renderer = new SilkD3D12SceneRenderer(device, resources, samplers);
        using var targetHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 2);
        using var targetView = targetHeap.Allocate();
        using var smallTargetView = targetHeap.Allocate();
        using var target = device.CreateRenderTargetTexture2D(64, 64, Format.FormatR8G8B8A8Unorm);
        using var smallTarget = device.CreateRenderTargetTexture2D(32, 32, Format.FormatR8G8B8A8Unorm);
        using var depthHeap = device.CreateDescriptorHeap(DescriptorHeapType.Dsv, 2);
        using var depthView = depthHeap.Allocate();
        using var smallDepthView = depthHeap.Allocate();
        using var depth = device.CreateDepthStencilTexture2D(64, 64, Format.FormatD32FloatS8X24Uint);
        using var smallDepth = device.CreateDepthStencilTexture2D(32, 32, Format.FormatD32FloatS8X24Uint);
        device.CreateRenderTargetView(target, targetView);
        device.CreateRenderTargetView(smallTarget, smallTargetView);
        device.CreateDepthStencilView(depth, depthView);
        device.CreateDepthStencilView(smallDepth, smallDepthView);
        var footprint = device.GetCopyableFootprint(target, out var totalBytes);
        var smallFootprint = device.GetCopyableFootprint(smallTarget, out var smallTotalBytes);
        using var readback = device.CreateBuffer(totalBytes, HeapType.Readback);
        using var smallReadback = device.CreateBuffer(smallTotalBytes, HeapType.Readback);
        var geometry = new MeshGeometry3D {
            Positions = new Vector3Collection([
                new Vector3(-0.75f, 0.75f, 0.5f), new Vector3(0.75f, 0.75f, 0.5f),
                new Vector3(-0.75f, -0.75f, 0.5f), new Vector3(0.75f, -0.75f, 0.5f)
            ]),
            Normals = new Vector3Collection([Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ]),
            Indices = new IntCollection([0, 1, 2, 2, 1, 3])
        };
        Vector4[] colors = multipleLayers ? [
            new(1, 0, 0, 0.5f), new(0, 1, 0, 0.25f), new(0, 0, 1, 0.5f),
            new(1, 1, 0, 0.25f), new(0, 1, 1, 0.5f), new(1, 0, 1, 0.25f)
        ] : [new(1, 0, 0, 0.5f)];
        var owners = colors.Select((color, index) => {
            var model = Matrix.Identity;
            model.M43 = multipleLayers ? (index + 1) * 0.125f - 0.5f : 0.125f;
            var node = new MeshNode {
                Material = new DiffuseMaterialCore { DiffuseColor = color, EnableUnLit = true },
                Geometry = geometry, ModelMatrix = model, IsTransparent = true,
                WireframeColor = new Vector4(0, 1, 0, 0.5f)
            };
            if (instanced) {
                var left = Matrix.Identity;
                left.M11 = left.M22 = 0.4f;
                left.M41 = -0.5f;
                var right = left;
                right.M41 = 0.5f;
                node.Instances = [left, right];
            }
            return node;
        }).ToArray();
        owners = [..owners, ..Enumerable.Range(0, extraMeshes).Select(_ => {
            var offscreen = Matrix.Identity;
            offscreen.M41 = 10;
            return new MeshNode {
                Geometry = geometry, ModelMatrix = offscreen, IsTransparent = true,
                Material = new DiffuseMaterialCore { DiffuseColor = colors[0], EnableUnLit = true }
            };
        })];
        var nodes = new FastList<SceneNode>(owners.Length);
        foreach (var owner in reverse ? owners.Reverse() : owners) nodes.Add(owner);
        var transforms = new GlobalTransformStruct {
            View = Matrix.Identity, Projection = Matrix.Identity, ViewProjection = Matrix.Identity,
            Viewport = new Vector4(64, 64, 1f / 64, 1f / 64),
            Resolution = new Vector4(64, 64, 1f / 64, 1f / 64), EyePos = new Vector3(0, 0, 2), DpiScale = 1
        };

        /// <summary>
        ///     Records one complete frame and returns only bytes copied after its GPU fence completes.
        /// </summary>
        /// <param name="resized">Whether to replace the transparency targets with a separate 32-pixel target.</param>
        /// <param name="transparent">Whether to use peeling rather than the ordinary mesh pass.</param>
        /// <returns>The complete RGBA target without row-pitch padding.</returns>
        byte[] Render(bool resized = false, bool transparent = true) {
            var colorTarget = resized ? smallTarget : target;
            var colorView = resized ? smallTargetView : targetView;
            var depthTarget = resized ? smallDepth : depth;
            var stencilView = resized ? smallDepthView : depthView;
            var buffer = resized ? smallReadback : readback;
            var layout = resized ? smallFootprint : footprint;
            var size = resized ? 32u : 64u;
            var frameTransforms = transforms;
            frameTransforms.Viewport = frameTransforms.Resolution = new Vector4(size, size, 1f / size, 1f / size);
            var frustum = new BoundingFrustum(Matrix.Identity);
            renderer.BeginFrame();
            context.Reset();
            context.Transition(colorTarget, ResourceStates.RenderTarget);
            context.ClearRenderTarget(colorTarget, colorView, [0, 0, 1, 1]);
            context.ClearDepthStencil(depthTarget, stencilView);
            context.SetRenderTargets(colorView, stencilView);
            context.SetViewport(size, size);
            if (transparent) {
                renderer.RenderDepthPeeling(context, nodes, catalog.ResolveDepthPeelingInitialization,
                    catalog.ResolveDepthPeeling, catalog.ResolveDepthPeelingComposition(), iterations, in frameTransforms,
                    false, ref frustum, colorTarget, colorView, stencilView, size, size, null, null,
                    catalog.ResolveBoneSkinning, null);
            } else {
                renderer.RenderVisible(context, nodes, catalog.Resolve, in frameTransforms, false, ref frustum);
            }
            context.Transition(colorTarget, ResourceStates.CopySource);
            context.CopyTextureToBuffer(buffer, colorTarget, in layout);
            D3D12Profiler.ReportDescriptors(renderer.SceneBindings, renderer.EffectBindings, resources, samplers);
            context.Close();
            queue.Execute(context);
            fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(10));
            device.ThrowIfDeviceRemoved();
            var pixels = new byte[size * size * 4];
            for (var row = 0; row < size; row++) {
                var bytes = buffer.Read((int) size * 4, layout.Offset + (ulong) row * layout.Footprint.RowPitch);
                bytes.CopyTo(pixels, row * (int) size * 4);
            }
            return pixels;
        }

        try {
            var baseline = Render();
            var expected = Compose(colors);
            AssertPhysicalPlane(baseline, expected, instanced, colors.Length);
            var descriptorCount = resources.Count;
            var samplerCount = samplers.Count;
            var tableSize = SilkD3D12GraphicsBindings.ConstantBufferCount +
                SilkD3D12GraphicsBindings.ShaderResourceCount + SilkD3D12GraphicsBindings.UnorderedAccessCount;
            var expectedTables = owners.Length * 2;
            Assert.Equal(tableSize, descriptorCount);
            Assert.Equal(SilkD3D12GraphicsBindings.SamplerCount, samplerCount);
            Assert.Equal(tableSize * expectedTables, renderer.SceneBindings.ResourceDescriptorCount);
            Assert.Equal(SilkD3D12GraphicsBindings.SamplerCount * expectedTables,
                renderer.SceneBindings.SamplerDescriptorCount);
            if (extraMeshes > 0) {
                Assert.Equal(2, renderer.SceneBindings.PageCount);
                for (var toggle = 0; toggle < 3; toggle++) {
                    _ = Render(transparent: false);
                    var translucent = Render();
                    Assert.Equal(baseline, translucent);
                }
            }
            for (var repeat = 0; repeat < 20; repeat++) {
                var unchanged = Render();
                Assert.Equal(baseline, unchanged);
            }
            Assert.Equal(descriptorCount, resources.Count);
            Assert.Equal(samplerCount, samplers.Count);

            owners[0].RenderWireframe = true;
            var green = Render();
            for (var repeat = 0; repeat < 5; repeat++) {
                var unchanged = Render();
                Assert.Equal(green, unchanged);
            }
            owners[0].WireframeColor = new Vector4(0, 0, 1, 0.25f);
            var blue = Render();
            for (var repeat = 0; repeat < 5; repeat++) {
                var unchanged = Render();
                Assert.Equal(blue, unchanged);
            }
            owners[0].RenderWireframe = false;
            var disabled = Render();
            Assert.Equal(baseline, disabled);
            for (var repeat = 0; repeat < 20; repeat++) {
                var unchanged = Render();
                Assert.Equal(baseline, unchanged);
            }
            int[] centers = instanced ? [16, 48] : [32];
            foreach (var centerX in centers) {
                var edge = (32 * 64 + centerX - 1) * 4;
                var interior = (29 * 64 + centerX - 3) * 4;
                TestContext.Current.TestOutputHelper?.WriteLine(
                    $"{driver} iterations={iterations} layers={colors.Length} instances={instanced} reverse={reverse} " +
                    $"({centerX - 1},32): base=[{string.Join(',', baseline[edge..(edge + 4)])}] " +
                    $"green=[{string.Join(',', green[edge..(edge + 4)])}] blue=[{string.Join(',', blue[edge..(edge + 4)])}]");
                Assert.True(green[edge + 1] > baseline[edge + 1] + 40);
                Assert.True(blue[edge + 2] > green[edge + 2] + 10);
                Assert.True(blue[edge + 1] < green[edge + 1]);
                Assert.Equal(baseline[interior..(interior + 4)], green[interior..(interior + 4)]);
                Assert.Equal(baseline[interior..(interior + 4)], blue[interior..(interior + 4)]);
                Assert.Equal(255, green[edge + 3]);
                Assert.Equal(255, blue[edge + 3]);
                if (!multipleLayers) {
                    // Both original indexed triangles rasterize their shared wire edge at the nearest depth.
                    var greenExpected = Compose([new(0, 1, 0, 0.5f), new(0, 1, 0, 0.5f), colors[0]]);
                    var blueExpected = Compose([new(0, 0, 1, 0.25f), new(0, 0, 1, 0.25f), colors[0]]);
                    for (var channel = 0; channel < 3; channel++) {
                        Assert.InRange(green[edge + channel], greenExpected[channel] - 1, greenExpected[channel] + 1);
                        Assert.InRange(blue[edge + channel], blueExpected[channel] - 1, blueExpected[channel] + 1);
                    }
                }
            }
            var smaller = Render(resized: true);
            var smallOffset = (16 * 32 + (instanced ? 8 : 16)) * 4;
            for (var channel = 0; channel < 3; channel++)
                Assert.InRange(smaller[smallOffset + channel], expected[channel] - 3, expected[channel] + 3);
            var restored = Render();
            Assert.Equal(baseline, restored);
            var afterRetirement = Render();
            Assert.Equal(baseline, afterRetirement);
            Assert.Equal(descriptorCount, resources.Count);
            Assert.Equal(samplerCount, samplers.Count);
            Assert.Equal(tableSize * expectedTables, renderer.SceneBindings.ResourceDescriptorCount);
            foreach (var owner in owners) {
                Assert.Same(geometry, owner.Geometry);
                owner.Dispose();
            }
            renderer.BeginFrame();
            Assert.Equal(0, renderer.SceneBindings.ResourceDescriptorCount);
            Assert.Equal(0, renderer.SceneBindings.SamplerDescriptorCount);
            // Only the full-screen transparency table remains after all source mesh owners are disposed.
            var fullScreenDescriptors = SilkD3D12GraphicsBindings.ConstantBufferCount +
                SilkD3D12GraphicsBindings.ShaderResourceCount + SilkD3D12GraphicsBindings.UnorderedAccessCount;
            Assert.Equal(fullScreenDescriptors, resources.Count);
            Assert.Equal(SilkD3D12GraphicsBindings.SamplerCount, samplers.Count);
            if (extraMeshes > 0) {
                var replacementTransform = Matrix.Identity;
                replacementTransform.M43 = 0.125f;
                using var replacement = new MeshNode {
                    Geometry = geometry, IsTransparent = true, ModelMatrix = replacementTransform,
                    Material = new DiffuseMaterialCore { DiffuseColor = colors[0], EnableUnLit = true }
                };
                nodes.Clear();
                nodes.Add(replacement);
                var replacementPixels = Render();
                Assert.Equal(baseline, replacementPixels);
                Assert.Equal(tableSize * 2, renderer.SceneBindings.ResourceDescriptorCount);
                Assert.Equal(2, renderer.SceneBindings.PageCount);
                replacement.Dispose();
                renderer.BeginFrame();
                Assert.Equal(0, renderer.SceneBindings.ResourceDescriptorCount);
                Assert.Equal(0, renderer.SceneBindings.SamplerDescriptorCount);
            }
            renderer.Dispose();
            Assert.Equal(0, resources.Count);
            Assert.Equal(0, samplers.Count);
        } finally {
            foreach (var owner in owners) owner.Dispose();
        }
    }

    /// <summary>
    ///     Composes independent far-to-near alpha layers over the known opaque blue background.
    /// </summary>
    /// <param name="colors">The source colors ordered from nearest to farthest.</param>
    /// <returns>The analytic RGB components on the byte scale.</returns>
    private static float[] Compose(Vector4[] colors) {
        var rgb = new Vector3(0, 0, 1);
        foreach (var color in colors.Reverse())
            rgb = new Vector3(color.X, color.Y, color.Z) * color.W + rgb * (1 - color.W);
        return [rgb.X * 255, rgb.Y * 255, rgb.Z * 255];
    }

    /// <summary>
    ///     Checks every physical pixel, including outer boundaries, against analytic coverage and layer color.
    /// </summary>
    /// <param name="pixels">The complete fence-completed RGBA target.</param>
    /// <param name="expected">The independently composed covered RGB color on the byte scale.</param>
    /// <param name="instanced">Whether the two known instance transforms determine coverage.</param>
    /// <param name="layers">The number of independently blended source layers.</param>
    private static void AssertPhysicalPlane(byte[] pixels, float[] expected, bool instanced, int layers) {
        for (var y = 0; y < 64; y++) {
            for (var x = 0; x < 64; x++) {
                var covered = instanced
                    ? y is >= 22 and <= 41 && (x is >= 6 and <= 25 or >= 38 and <= 57)
                    : x is >= 8 and <= 55 && y is >= 8 and <= 55;
                var offset = (y * 64 + x) * 4;
                for (var channel = 0; channel < 3; channel++) {
                    var value = covered ? expected[channel] : channel == 2 ? 255 : 0;
                    var tolerance = covered ? layers == 1 ? 0.5f : 3 : 0;
                    // UNORM blending quantizes after each consumed layer; it does not change coverage or equality checks.
                    Assert.InRange(pixels[offset + channel], value - tolerance, value + tolerance);
                }
                Assert.Equal(255, pixels[offset + 3]);
            }
        }
    }
}

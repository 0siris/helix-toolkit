using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Collection;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.DefaultShaders;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Matrix = Silk.NET.Maths.Matrix4X4<float>;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace HelixToolkit.SharpDX.Core.Native;

/// Exercises native filled meshes and their registered wireframe passes through offscreen pixel readback.
public sealed class NativeMeshWireframeTests {
    /// Checks registered auxiliary passes whose instance matrices occupy slot four on WARP.
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpRegisteredSlotFourWireframeReadbackTest() => VerifyOverlay(SilkDriverType.Warp, 0, true, slotFour: true);

    /// Checks registered auxiliary passes whose instance matrices occupy slot four on hardware.
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    public void HardwareRegisteredSlotFourWireframeReadbackTest() => VerifyOverlay(SilkDriverType.Hardware, 0, true, slotFour: true);

    /// Checks that the wireframe overlay retains the actual GPU-skinned vertex stream on WARP.
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpSkinnedWireframeReadbackTest() => VerifyOverlay(SilkDriverType.Warp, 0, true, true);

    /// Checks that the wireframe overlay retains the actual GPU-skinned vertex stream on hardware.
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    public void HardwareSkinnedWireframeReadbackTest() => VerifyOverlay(SilkDriverType.Hardware, 0, true, true);

    /// <summary>
    ///     Checks toggling, live RGBA updates, unchanged interiors and transformed instances on real render targets.
    /// </summary>
    /// <param name="stage">Zero for opaque, one for weighted OIT, or two for dual-depth peeling.</param>
    /// <param name="instanced">Whether two independently transformed instances share the original geometry.</param>
    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [Trait("Category", "Warp")]
    public void WarpWireframeOverlayReadbackTest(int stage, bool instanced) => VerifyOverlay(SilkDriverType.Warp, stage, instanced);

    /// <summary>
    ///     Repeats the physical pixel checks on the selected hardware adapter without any window or swap chain.
    /// </summary>
    /// <param name="stage">Zero for opaque, one for weighted OIT, or two for dual-depth peeling.</param>
    /// <param name="instanced">Whether two independently transformed instances share the original geometry.</param>
    [Theory(Explicit = true)]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [Trait("Category", "Hardware")]
    public void HardwareWireframeOverlayReadbackTest(int stage, bool instanced) => VerifyOverlay(SilkDriverType.Hardware, stage, instanced);

    /// <summary>
    ///     Renders original indexed triangles, reads actual RGBA pixels and checks source wireframe color semantics.
    /// </summary>
    /// <param name="driver">The native adapter type.</param>
    /// <param name="stage">The transparency stage, or zero for an opaque mesh.</param>
    /// <param name="instanced">Whether the original mesh is rendered twice through the instance buffer.</param>
    /// <param name="skinned">Whether the shared vertices are transformed by an actual GPU bone precompute.</param>
    /// <param name="slotFour">Whether a registered custom technique moves instance matrices to stream four.</param>
    private static void VerifyOverlay(SilkDriverType driver, int stage, bool instanced,
        bool skinned = false, bool slotFour = false) {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, driver);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var registry = slotFour ? CreateSlotFourRegistry() : null;
        using var catalog = new D3D12ScenePassCatalog(device, Format.FormatR8G8B8A8Unorm,
            Format.FormatD32FloatS8X24Uint, registry?.TechniqueDescriptions, registry);
        using var resources = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 1024, true);
        using var samplers = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 128, true);
        using var renderer = new SilkD3D12SceneRenderer(device, resources, samplers);
        using var targetHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        using var targetView = targetHeap.Allocate();
        using var target = device.CreateRenderTargetTexture2D(64, 64, Format.FormatR8G8B8A8Unorm);
        using var depthHeap = device.CreateDescriptorHeap(DescriptorHeapType.Dsv, 1);
        using var depthView = depthHeap.Allocate();
        using var depth = device.CreateDepthStencilTexture2D(64, 64, Format.FormatD32FloatS8X24Uint);
        device.CreateRenderTargetView(target, targetView);
        device.CreateDepthStencilView(depth, depthView);
        var footprint = device.GetCopyableFootprint(target, out var totalBytes);
        using var readback = device.CreateBuffer(totalBytes, HeapType.Readback);
        var geometry = new MeshGeometry3D {
            Positions = new Vector3Collection([
                new Vector3(-0.75f, 0.75f, 0.5f), new Vector3(0.75f, 0.75f, 0.5f),
                new Vector3(-0.75f, -0.75f, 0.5f), new Vector3(0.75f, -0.75f, 0.5f)
            ]),
            Normals = new Vector3Collection([Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ]),
            Indices = new IntCollection([0, 1, 2, 2, 1, 3])
        };
        var model = Matrix.Identity;
        model.M43 = 0.125f;
        using var node = skinned ? new BoneSkinMeshNode() : new MeshNode();
        if (node is BoneSkinMeshNode bone) {
            var translation = Matrix.Identity;
            translation.M41 = 0.125f;
            bone.BoneMatrices = [translation];
            geometry = new BoneSkinnedMeshGeometry3D {
                Positions = new Vector3Collection(geometry.Positions!.Select(position => position - new Vector3(0.125f, 0, 0))),
                Normals = geometry.Normals,
                Indices = geometry.Indices,
                VertexBoneIds = Enumerable.Repeat(new BoneIds { Bone1 = 0, Weights = Vector4.UnitX }, 4).ToList()
            };
        }
        node.Material = new DiffuseMaterialCore {
            DiffuseColor = new Vector4(1, 0, 0, stage == 0 ? 1 : 0.5f), EnableUnLit = true
        };
        node.Geometry = geometry;
        node.ModelMatrix = model;
        node.IsTransparent = stage != 0;
        node.WireframeColor = new Vector4(0, 1, 0, 0.5f);
        if (slotFour) node.OnSetRenderTechnique = manager => manager["WireframeSlotFour"];
        if (instanced) {
            var left = Matrix.Identity;
            left.M11 = left.M22 = 0.4f;
            left.M41 = -0.5f;
            var right = left;
            right.M41 = 0.5f;
            node.Instances = [left, right];
        }
        var nodes = new FastList<SceneNode>(1) { node };
        var transforms = new GlobalTransformStruct {
            View = Matrix.Identity, Projection = Matrix.Identity, ViewProjection = Matrix.Identity,
            Viewport = new Vector4(64, 64, 1f / 64, 1f / 64),
            Resolution = new Vector4(64, 64, 1f / 64, 1f / 64), EyePos = new Vector3(0, 0, 2),
            OITWeightPower = 3, OITWeightDepthSlope = 1, OITWeightMode = (int) OitWeightMode.Linear1, DpiScale = 1
        };

        /// <summary>
        ///     Submits a fresh frame and packs the fence-completed readback rows without GPU pitch padding.
        /// </summary>
        /// <returns>The actual RGBA render target bytes.</returns>
        byte[] Render() {
            var frustum = new BoundingFrustum(Matrix.Identity);
            renderer.BeginFrame();
            context.Reset();
            context.Transition(target, ResourceStates.RenderTarget);
            context.ClearRenderTarget(target, targetView, [0, 0, 1, 1]);
            context.ClearDepthStencil(depth, depthView);
            context.SetRenderTargets(targetView, depthView);
            context.SetViewport(64, 64);
            if (stage == 0)
                renderer.RenderVisible(context, nodes, catalog.Resolve, in transforms, false, ref frustum,
                    boneSkinningPassSelector: catalog.ResolveBoneSkinning);
            else if (stage == 1)
                renderer.RenderWeightedTransparency(context, nodes, catalog.ResolveWeightedTransparency,
                    catalog.ResolveWeightedComposition(), in transforms, false, ref frustum, targetView, depthView,
                    64, 64, null, null, catalog.ResolveBoneSkinning, null);
            else
                renderer.RenderDepthPeeling(context, nodes, catalog.ResolveDepthPeelingInitialization,
                    catalog.ResolveDepthPeeling, catalog.ResolveDepthPeelingComposition(), 2, in transforms,
                    false, ref frustum, target, targetView, depthView, 64, 64, null, null,
                    catalog.ResolveBoneSkinning, null);
            context.Transition(target, ResourceStates.CopySource);
            context.CopyTextureToBuffer(readback, target, in footprint);
            context.Close();
            queue.Execute(context);
            fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(10));
            device.ThrowIfDeviceRemoved();
            var pixels = new byte[64 * 64 * 4];
            for (var row = 0; row < 64; row++) {
                var bytes = readback.Read(64 * 4, footprint.Offset + (ulong) row * footprint.Footprint.RowPitch);
                bytes.CopyTo(pixels, row * 64 * 4);
            }
            return pixels;
        }

        var baseline = Render();
        node.RenderWireframe = true;
        var green = Render();
        if (stage == 0) {
            var materialPass = catalog.Resolve(node);
            var cachedPass = catalog.Resolve(node);
            var shadowPass = catalog.ResolveShadow(node);
            var effectPass = catalog.ResolvePostEffectGeometry(node, DefaultPassNames.EffectOutlineP1,
                Format.FormatD32FloatS8X24Uint);
            Assert.NotNull(materialPass?.D3D12WireframePass);
            Assert.Same(materialPass, cachedPass);
            Assert.Null(shadowPass?.D3D12WireframePass);
            Assert.Null(effectPass?.D3D12WireframePass);
            node.CullMode = CullMode.Front;
            var frontCulled = Render();
            node.CullMode = CullMode.Back;
            var backCulled = Render();
            var clear = new byte[baseline.Length];
            for (var pixel = 0; pixel < clear.Length; pixel += 4) {
                clear[pixel + 2] = 255;
                clear[pixel + 3] = 255;
            }
            var frontHidesMesh = frontCulled.SequenceEqual(clear);
            Assert.Equal(frontHidesMesh ? green : clear, backCulled);
            Assert.Equal(frontHidesMesh ? clear : green, frontCulled);
            node.CullMode = CullMode.None;
        }
        node.WireframeColor = new Vector4(0, 0, 1, 0.25f);
        var blue = Render();
        node.RenderWireframe = false;
        var disabled = Render();
        Assert.Equal(baseline, disabled);
        Assert.Same(geometry, node.Geometry);
        int[] centers = instanced ? [16, 48] : [32];
        foreach (var centerX in centers) {
            // Identity camera maps the internal triangle edge through (centerX, 32); inspect its two-pixel footprint.
            var neighborhood = Enumerable.Range(30, 4).SelectMany(y => Enumerable.Range(centerX - 2, 4)
                .Select(x => (y * 64 + x) * 4)).ToArray();
            var edge = neighborhood.MaxBy(offset => green[offset + 1]);
            TestContext.Current.TestOutputHelper?.WriteLine(
                $"{driver} stage={stage} instances={instanced} skin={skinned} slotFour={slotFour} x={centerX}: " +
                $"baseline=[{string.Join(',', baseline[edge..(edge + 4)])}] " +
                $"green=[{string.Join(',', green[edge..(edge + 4)])}] " +
                $"blue=[{string.Join(',', blue[edge..(edge + 4)])}]");
            Assert.True(green[edge + 1] > baseline[edge + 1] + 40,
                $"{driver}, stage {stage}, x={centerX}: edge RGBA {string.Join(',', green[edge..(edge + 4)])}");
            Assert.True(blue[edge + 2] > green[edge + 2] + 10);
            Assert.True(blue[edge + 1] < green[edge + 1]);
            var interior = (29 * 64 + centerX - 3) * 4;
            Assert.Equal(baseline[interior..(interior + 4)], green[interior..(interior + 4)]);
            Assert.InRange(baseline[interior], stage == 0 ? 254 : 120, 255);
            if (stage == 0) {
                Assert.InRange(green[edge + 3], 127, 128);
                Assert.InRange(blue[edge + 3], 63, 64);
            }
        }
        if (stage == 0 && !skinned) {
            var nearModel = Matrix.Identity;
            nearModel.M11 = nearModel.M22 = 2;
            nearModel.M43 = -0.25f;
            using var occluder = new MeshNode {
                Material = new DiffuseMaterialCore { DiffuseColor = new Vector4(1, 1, 0, 1), EnableUnLit = true },
                Geometry = geometry, ModelMatrix = nearModel
            };
            nodes.Insert(0, occluder);
            var occludedBaseline = Render();
            node.RenderWireframe = true;
            var occludedWireframe = Render();
            Assert.Equal(occludedBaseline, occludedWireframe);
            var center = (32 * 64 + centers[0]) * 4;
            Assert.Equal(new byte[] { 255, 255, 0, 255 }, occludedWireframe[center..(center + 4)]);
            nodes.Remove(occluder);
        }
        renderer.Dispose();
        Assert.Equal(0, resources.Count);
        Assert.Equal(0, samplers.Count);
    }

    /// <summary>
    ///     Registers the same auxiliary pass layout adaptation used when a custom vertex stream precedes instances.
    /// </summary>
    /// <returns>The owned effects manager containing an independent registered technique.</returns>
    private static DefaultEffectsManager CreateSlotFourRegistry() {
        var manager = new DefaultEffectsManager();
        var source = manager[DefaultRenderTechniqueNames.Mesh].Description;

        /// <summary>
        ///     Copies the actual input layout and moves the instance matrix without altering shared descriptions.
        /// </summary>
        /// <param name="layout">The optional pass layout override.</param>
        /// <returns>The independent native input layout.</returns>
        InputLayoutDescription MoveInstances(InputLayoutDescription? layout) {
            var original = layout ?? source.InputLayoutDescription!;
            var elements = original.InputElements!.Select(element => {
                if (element.Classification == InputClassification.PerInstanceData) element.Slot = 4;
                return element;
            }).ToArray();
            return new InputLayoutDescription(original.ShaderByteCode!, elements);
        }

        var passes = source.PassDescriptions!.Select(pass => new ShaderPassDescription(pass.Name!) {
            ShaderList = pass.ShaderList,
            InputLayoutDescription = MoveInstances(pass.InputLayoutDescription),
            BlendStateDescription = pass.BlendStateDescription,
            DepthStencilStateDescription = pass.DepthStencilStateDescription,
            RasterStateDescription = pass.RasterStateDescription,
            BlendFactor = pass.BlendFactor,
            SampleMask = pass.SampleMask,
            StencilRef = pass.StencilRef,
            Topology = pass.Topology
        }).ToList();
        manager.AddTechnique(new TechniqueDescription("WireframeSlotFour") {
            InputLayoutDescription = MoveInstances(null), PassDescriptions = passes
        });
        return manager;
    }
}

using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Cameras;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Scene;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Matrix = Silk.NET.Maths.Matrix4X4<float>;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace HelixToolkit.SharpDX.Core.Native;

/// Exercises generic hidden-node native activation, original update hooks and physical draw-list classification.
public sealed class NativeScenePreparationTests {
    /// Checks hidden detach/reactivation and changed native draw classification with WARP readback.
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpHiddenReactivationReadbackTest() => VerifyReactivation(SilkDriverType.Warp);

    /// Checks the same hidden-node preparation and physical draw-list transitions on hardware.
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    public void HardwareHiddenReactivationReadbackTest() => VerifyReactivation(SilkDriverType.Hardware);

    /// <summary>
    ///     Drives original node attachment/update hooks through transparent, hidden, detached and opaque states.
    /// </summary>
    /// <param name="driver">The physical adapter selection.</param>
    private static void VerifyReactivation(SilkDriverType driver) {
        using var device = SilkD3D12DeviceFactory.CreateDefault(driverType: driver);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var resources = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 2048, true);
        using var samplers = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 128, true);
        using var renderer = new SilkD3D12SceneRenderer(device, resources, samplers);
        using var catalog = new D3D12ScenePassCatalog(device, Format.FormatR8G8B8A8Unorm,
            Format.FormatD32FloatS8X24Uint);
        using var targets = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        using var targetView = targets.Allocate();
        using var target = device.CreateRenderTargetTexture2D(32, 32, Format.FormatR8G8B8A8Unorm);
        using var depths = device.CreateDescriptorHeap(DescriptorHeapType.Dsv, 1);
        using var depthView = depths.Allocate();
        using var depth = device.CreateDepthStencilTexture2D(32, 32, Format.FormatD32FloatS8X24Uint);
        device.CreateRenderTargetView(target, targetView);
        device.CreateDepthStencilView(depth, depthView);
        using var root = new GroupNode();
        using var node = new UpdatingMesh {
            Visible = false, RequestedVisible = true, RequestedTransparent = true,
            Material = new DiffuseMaterialCore { EnableUnLit = true, DiffuseColor = new Vector4(1, 0, 0, 0.5f) },
            Geometry = new MeshGeometry3D {
                Positions = new Vector3Collection([new(-0.5f, 0.5f, 0.5f), new(0.5f, 0.5f, 0.5f),
                    new(-0.5f, -0.5f, 0.5f), new(0.5f, -0.5f, 0.5f)]),
                Normals = new Vector3Collection([Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ]),
                Indices = new IntCollection([0, 1, 2, 2, 1, 3])
            }, CullMode = CullMode.None, FrontCcw = false
        };
        root.AddChildNode(node);
        using var update = new RenderContext(new PerspectiveCameraCore(), 32, 32);
        var transforms = new GlobalTransformStruct {
            View = Matrix.Identity, Projection = Matrix.Identity, ViewProjection = Matrix.Identity,
            Viewport = new Vector4(32, 32, 1f / 32, 1f / 32),
            Resolution = new Vector4(32, 32, 1f / 32, 1f / 32), EyePos = new Vector3(0, 0, 2),
            OITWeightPower = 3, OITWeightDepthSlope = 1, OITWeightMode = (int)OITWeightMode.Linear1, DpiScale = 1
        };
        var footprint = device.GetCopyableFootprint(target, out var bytes);
        using var readback = device.CreateBuffer(bytes, HeapType.Readback);

        /// <summary>
        ///     Classifies the prepared hierarchy and records its actual opaque and weighted-transparent draws.
        /// </summary>
        /// <returns>The two native draw counts and every fence-completed RGBA pixel.</returns>
        (int Opaque, int Transparent, byte[] Pixels) Render() {
            renderer.BeginFrame();
            var candidates = renderer.PrepareScene(new[] { root }, update);
            var opaque = new FastList<SceneNode>();
            var transparent = new FastList<SceneNode>();
            foreach (var candidate in candidates) {
                if (candidate.RenderType == RenderType.Opaque) opaque.Add(candidate);
                else if (candidate.RenderType == RenderType.Transparent) transparent.Add(candidate);
            }
            context.Reset();
            context.Transition(target, ResourceStates.RenderTarget);
            context.ClearRenderTarget(target, targetView, [0, 0, 1, 1]);
            context.ClearDepthStencil(depth, depthView);
            context.SetRenderTargets(targetView, depthView);
            context.SetViewport(32, 32);
            var frustum = new BoundingFrustum(Matrix.Identity);
            var opaqueCount = renderer.RenderVisible(context, opaque, catalog.Resolve,
                in transforms, false, ref frustum);
            var transparentCount = renderer.RenderWeightedTransparency(context, transparent,
                catalog.ResolveWeightedTransparency, catalog.ResolveWeightedComposition(), in transforms,
                false, ref frustum, targetView, depthView, 32, 32, null, null, catalog.ResolveBoneSkinning, null);
            context.Transition(target, ResourceStates.CopySource);
            context.CopyTextureToBuffer(readback, target, in footprint);
            context.Close();
            queue.Execute(context);
            fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(10));
            var pixels = new byte[4096];
            for (var y = 0; y < 32; y++) {
                var row = readback.Read(128, footprint.Offset + (ulong)y * footprint.Footprint.RowPitch);
                row.CopyTo(pixels, y * 128);
            }
            return (opaqueCount, transparentCount, pixels);
        }

        var first = Render();
        Assert.True(node.RenderCore.IsAttached);
        Assert.Equal(1, node.Activations);
        Assert.Equal(1, node.Updates);
        Assert.Equal(RenderType.Transparent, node.RenderType);
        Assert.Equal(0, first.Opaque);
        Assert.Equal(1, first.Transparent);
        CheckImage(first.Pixels, 128, 127);
        node.RequestedVisible = false;
        var hidden = Render();
        Assert.False(node.Visible);
        Assert.False(node.IsRenderable);
        Assert.Equal(0, hidden.Opaque);
        Assert.Equal(0, hidden.Transparent);
        CheckImage(hidden.Pixels, 0, 255);
        node.Detach();
        Assert.False(node.Listening);
        node.RequestedVisible = true;
        node.RequestedTransparent = false;
        var material = (DiffuseMaterialCore)node.Material!;
        material.DiffuseColor = new Vector4(1, 0, 0, 1);
        var reactivated = Render();
        Assert.Equal(2, node.Activations);
        Assert.True(node.Listening);
        Assert.True(node.Visible);
        Assert.True(node.IsRenderable);
        Assert.Equal(RenderType.Opaque, node.RenderType);
        Assert.Equal(1, reactivated.Opaque);
        Assert.Equal(0, reactivated.Transparent);
        CheckImage(reactivated.Pixels, 255, 0);
        root.Visible = false;
        var hiddenParent = Render();
        Assert.True(node.Visible);
        Assert.Equal(0, hiddenParent.Opaque);
        Assert.Equal(0, hiddenParent.Transparent);
        CheckImage(hiddenParent.Pixels, 0, 255);
        root.Visible = true;
        node.RequestedTransparent = true;
        material.DiffuseColor = new Vector4(1, 0, 0, 0.5f);
        var changed = Render();
        Assert.Equal(2, node.Activations);
        Assert.Equal(0, changed.Opaque);
        Assert.Equal(1, changed.Transparent);
        CheckImage(changed.Pixels, 128, 127);
        renderer.Dispose();
        Assert.False(node.Listening);
        Assert.Equal(0, resources.Count);
        Assert.Equal(0, samplers.Count);
        device.ThrowIfDeviceRemoved();
    }

    /// <summary>
    ///     Checks every pixel against the independent rectangle, transparency and background predictions.
    /// </summary>
    /// <param name="pixels">The complete RGBA image.</param>
    /// <param name="red">The expected covered red channel.</param>
    /// <param name="blue">The expected covered blue channel.</param>
    private static void CheckImage(byte[] pixels, int red, int blue) {
        for (var y = 0; y < 32; y++)
            for (var x = 0; x < 32; x++) {
                var index = (y * 32 + x) * 4;
                var covered = x >= 8 && x < 24 && y >= 8 && y < 24;
                Assert.InRange(Math.Abs(pixels[index] - (covered ? red : 0)), 0, 1);
                Assert.Equal(0, pixels[index + 1]);
                Assert.InRange(Math.Abs(pixels[index + 2] - (covered ? blue : 255)), 0, 1);
            }
    }

    /// Uses the original generic attachment and update extension points to model detached observable ownership.
    private sealed class UpdatingMesh : MeshNode {
        /// Gets or sets the externally requested visibility while detached or listening.
        internal bool RequestedVisible { get; set; }
        /// Gets or sets the externally requested native transparency classification.
        internal bool RequestedTransparent { get; set; }
        /// Gets whether native attachment restored this node's observable-owner subscription.
        internal bool Listening { get; private set; }
        /// Gets the number of actual detached-to-attached activations.
        internal int Activations { get; private set; }
        /// Gets the number of original scene update callbacks.
        internal int Updates { get; private set; }

        /// <summary>
        ///     Restores the original observable owner when the native core actually reattaches.
        /// </summary>
        /// <param name="core">The original native render core.</param>
        protected override void AssignDefaultValuesToCore(RenderCore core) {
            base.AssignDefaultValuesToCore(core);
            Activations++;
            Listening = true;
            Visible = RequestedVisible;
        }

        public override void Update(RenderContext context) {
            Updates++;
            if (Listening) {
                Visible = RequestedVisible;
                IsTransparent = RequestedTransparent;
            }
            base.Update(context);
        }

        protected override void OnDetachD3D12() {
            Listening = false;
            base.OnDetachD3D12();
        }
    }
}

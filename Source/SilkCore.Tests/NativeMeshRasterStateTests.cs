using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Collection;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Matrix = Silk.NET.Maths.Matrix4X4<float>;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace HelixToolkit.SharpDX.Core.Native;

/// Checks actual mesh-facing and culling through filled opaque and transparent native passes without a window.
public sealed class NativeMeshRasterStateTests {
    /// <summary>Changing the mesh's front-face definition must invert actual back-face culling in every stage.</summary>
    /// <param name="stage">Zero for opaque, one for weighted transparency, or two for depth peeling.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [Trait("Category", "Warp")]
    public void FilledPassUsesMeshRasterState(int stage) {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var catalog = new D3D12ScenePassCatalog(device, Format.FormatR8G8B8A8Unorm,
            Format.FormatD32FloatS8X24Uint);
        using var resources = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 1024, true);
        using var samplers = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 128, true);
        using var renderer = new SilkD3D12SceneRenderer(device, resources, samplers);
        using var targetHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        using var targetView = targetHeap.Allocate();
        using var target = device.CreateRenderTargetTexture2D(16, 16, Format.FormatR8G8B8A8Unorm);
        using var depthHeap = device.CreateDescriptorHeap(DescriptorHeapType.Dsv, 1);
        using var depthView = depthHeap.Allocate();
        using var depth = device.CreateDepthStencilTexture2D(16, 16, Format.FormatD32FloatS8X24Uint);
        device.CreateRenderTargetView(target, targetView);
        device.CreateDepthStencilView(depth, depthView);
        var footprint = device.GetCopyableFootprint(target, out var size);
        using var readback = device.CreateBuffer(size, HeapType.Readback);
        using var node = new MeshNode {
            Geometry = new MeshGeometry3D {
                Positions = new Vector3Collection([new(-.9f, -.9f, .5f), new(.9f, -.9f, .5f), new(0, .9f, .5f)]),
                Colors = new Color4Collection([new(0, 1, 0, 1), new(0, 1, 0, 1), new(0, 1, 0, 1)]),
                Indices = new IntCollection([0, 1, 2])
            },
            Material = new DiffuseMaterialCore { DiffuseColor = new(0, 1, 0, stage == 0 ? 1 : .75f), EnableUnLit = true },
            IsTransparent = stage != 0,
            CullMode = CullMode.Back,
            FrontCcw = true
        };
        var nodes = new FastList<SceneNode> { node };
        var transforms = new GlobalTransformStruct {
            View = Matrix.Identity, Projection = Matrix.Identity, ViewProjection = Matrix.Identity,
            Viewport = new Vector4(16, 16, 1f / 16, 1f / 16),
            Resolution = new Vector4(16, 16, 1f / 16, 1f / 16), EyePos = new Vector3(0, 0, 2),
            OITWeightPower = 3, OITWeightDepthSlope = 1, OITWeightMode = (int)OitWeightMode.Linear1, DpiScale = 1
        };
        var counterclockwise = Render();
        node.FrontCcw = false;
        var clockwise = Render();
        node.CullMode = CullMode.None;
        var twoSided = Render();
        Assert.NotEqual(counterclockwise[1] > 100, clockwise[1] > 100);
        Assert.True(twoSided[1] > 100);
        node.FrontCcw = true;
        node.CullMode = CullMode.Back;
        var restored = Render();
        Assert.Equal(counterclockwise, restored);
        renderer.Dispose();
        Assert.Equal(0, resources.Count);
        Assert.Equal(0, samplers.Count);

        /// <summary>Reads the center pixel after the submitted native commands finish.</summary>
        /// <returns>The actual RGBA bytes.</returns>
        byte[] Render() {
            var frustum = new BoundingFrustum(Matrix.Identity);
            renderer.BeginFrame();
            context.Reset();
            context.Transition(target, ResourceStates.RenderTarget);
            context.ClearRenderTarget(target, targetView, [0, 0, 1, 1]);
            context.ClearDepthStencil(depth, depthView);
            context.SetRenderTargets(targetView, depthView);
            context.SetViewport(16, 16);
            if (stage == 0)
                renderer.RenderVisible(context, nodes, catalog.Resolve, in transforms, false, ref frustum);
            else if (stage == 1)
                renderer.RenderWeightedTransparency(context, nodes, catalog.ResolveWeightedTransparency,
                    catalog.ResolveWeightedComposition(), in transforms, false, ref frustum, targetView, depthView,
                    16, 16, null, null, null, null);
            else
                renderer.RenderDepthPeeling(context, nodes, catalog.ResolveDepthPeelingInitialization,
                    catalog.ResolveDepthPeeling, catalog.ResolveDepthPeelingComposition(), 2, in transforms,
                    false, ref frustum, target, targetView, depthView, 16, 16, null, null, null, null);
            context.Transition(target, ResourceStates.CopySource);
            context.CopyTextureToBuffer(readback, target, in footprint);
            context.Close();
            queue.Execute(context);
            fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(10));
            device.ThrowIfDeviceRemoved();
            return readback.Read(4, footprint.Offset + 8UL * footprint.Footprint.RowPitch + 8UL * 4);
        }
    }
}

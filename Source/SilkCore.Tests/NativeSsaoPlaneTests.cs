using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Collection;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.ShaderManager;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Matrix = Silk.NET.Maths.Matrix4X4<float>;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace HelixToolkit.SharpDX.Core.Native;

/// Exercises the original SSAO hemisphere comparison using actual planes, native passes and half-float readback.
public sealed class NativeSsaoPlaneTests {
    /// <summary>
    ///     Checks opposite plane normals against independent all-samples-front or all-samples-behind expectations.
    /// </summary>
    /// <param name="normalZ">The signed unit normal of the plane.</param>
    /// <param name="expected">The analytic visibility fraction after evaluating the hemisphere.</param>
    [Theory]
    [InlineData(-1f, 1f)]
    [InlineData(1f, 0f)]
    [Trait("Category", "Warp")]
    public void PlanarHemisphereReadbackTest(float normalZ, float expected) {
        using var device = SilkD3D12DeviceFactory.CreateDefault(driverType: SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var catalog = new D3D12ScenePassCatalog(device, Format.FormatB8G8R8A8Unorm,
            Format.FormatD32FloatS8X24Uint);
        using var resources = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 1024, true);
        using var samplers = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 100, true);
        using var renderer = new SilkD3D12SceneRenderer(device, resources, samplers);
        var normal = new Vector3(0, 0, normalZ);
        var geometry = new MeshGeometry3D {
            Positions = new Vector3Collection([new(-1, 1, 0.5f), new(1, 1, 0.5f),
                new(-1, -1, 0.5f), new(1, -1, 0.5f)]),
            Normals = new Vector3Collection([normal, normal, normal, normal]),
            Indices = new IntCollection([0, 1, 2, 2, 1, 3])
        };
        using var node = new MeshNode {
            Geometry = geometry, Material = new DiffuseMaterialCore(), CullMode = CullMode.None
        };
        var nodes = new FastList<SceneNode> { node };
        var transforms = new GlobalTransformStruct {
            View = Matrix.Identity, Projection = Matrix.Identity, ViewProjection = Matrix.Identity,
            Viewport = new Vector4(32, 32, 1f / 32, 1f / 32),
            Resolution = new Vector4(32, 32, 1f / 32, 1f / 32), EyePos = new Vector3(0, 0, 2),
            SSAOEnabled = 1, SSAOBias = 0, SSAOIntensity = 1, DpiScale = 1
        };
        var frustum = new BoundingFrustum(Matrix.Identity);
        for (var frame = 0; frame < 2; frame++) {
            renderer.BeginFrame();
            context.Reset();
            var occlusion = renderer.RenderSsao(context, nodes, catalog.ResolveSsaoGeometry,
                catalog.ResolvePostProcess(DefaultRenderTechniqueNames.Ssao, DefaultPassNames.Default),
                catalog.ResolvePostProcess(DefaultRenderTechniqueNames.Ssao, DefaultPassNames.EffectBlurHorizontal),
                catalog.ResolveBoneSkinning, in transforms, false, ref frustum, 32, 32, SsaoQuality.High, 0.25f);
            var footprint = device.GetCopyableFootprint(occlusion, out var byteCount);
            using var readback = device.CreateBuffer(byteCount, HeapType.Readback);
            context.Transition(occlusion, ResourceStates.CopySource);
            context.CopyTextureToBuffer(readback, occlusion, in footprint);
            context.Close();
            queue.Execute(context);
            fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(10));
            for (var y = 12; y < 20; y++)
                for (var x = 12; x < 20; x++) {
                    var bytes = readback.Read(2, footprint.Offset + (ulong)y * footprint.Footprint.RowPitch + (ulong)x * 2);
                    var actual = (float)BitConverter.UInt16BitsToHalf(BitConverter.ToUInt16(bytes));
                    Assert.Equal(expected, actual, 3);
                }
        }
        renderer.Dispose();
        Assert.Equal(0, resources.Count);
        Assert.Equal(0, samplers.Count);
        Assert.Same(geometry, node.Geometry);
        device.ThrowIfDeviceRemoved();
    }
}

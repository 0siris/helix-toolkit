using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Scene;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Matrix = Silk.NET.Maths.Matrix4X4<float>;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace HelixToolkit.SharpDX.Core.Native;

/// Verifies original indexed post-effect geometry through native GPU draws and complete image/depth readback.
public sealed class NativePostEffectTests {
    /// <summary>
    ///     Checks stencil interior exclusion, zero blur, resized targets and preservation of scene depth/stencil.
    /// </summary>
    /// <param name="blur">The requested number of full blur pairs.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Trait("Category", "Warp")]
    public void WarpOutlineInteriorReadbackTest(int blur) => VerifyOutline(SilkDriverType.Warp, blur);

    /// <summary>
    ///     Runs the same analytic outline coverage and lifetime checks on the hardware adapter.
    /// </summary>
    /// <param name="blur">The requested number of full blur pairs.</param>
    [Theory(Explicit = true)]
    [InlineData(0)]
    [InlineData(1)]
    [Trait("Category", "Hardware")]
    public void HardwareOutlineInteriorReadbackTest(int blur) => VerifyOutline(SilkDriverType.Hardware, blur);

    /// <summary>
    ///     Renders a rectangle whose exact projected bounds give independent interior and exterior expectations.
    /// </summary>
    /// <param name="driver">The physical adapter selection.</param>
    /// <param name="blur">The number of blur pairs.</param>
    private static void VerifyOutline(SilkDriverType driver, int blur) {
        using var device = SilkD3D12DeviceFactory.CreateDefault(driverType: driver);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var catalog = new D3D12ScenePassCatalog(device, Format.FormatR8G8B8A8Unorm,
            Format.FormatD32FloatS8X24Uint);
        using var resources = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 4096, true);
        using var samplers = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 256, true);
        using var renderer = new SilkD3D12SceneRenderer(device, resources, samplers);
        using var targetHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        using var targetView = targetHeap.Allocate();
        using var depthHeap = device.CreateDescriptorHeap(DescriptorHeapType.Dsv, 1);
        using var depthView = depthHeap.Allocate();
        var geometry = new MeshGeometry3D {
            Positions = new Vector3Collection([new(-0.5f, 0.5f, 0.75f), new(0.5f, 0.5f, 0.75f),
                new(-0.5f, -0.5f, 0.75f), new(0.5f, -0.5f, 0.75f)]),
            Normals = new Vector3Collection([Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ]),
            Indices = new IntCollection([0, 1, 2, 2, 1, 3])
        };
        using var node = new MeshNode {
            Geometry = geometry, Material = new DiffuseMaterialCore(), CullMode = CullMode.None,
            IsTransparent = true, PostEffects = "border[color:#00FF00]; glow[color:#0000FF]"
        };
        var nodes = new FastList<SceneNode> { node };
        var technique = DefaultRenderTechniqueNames.PostEffectMeshBorderHighlight;
        foreach (var size in new uint[] { 64, 80, 64 }) {
            using var target = device.CreateRenderTargetTexture2D(size, size, Format.FormatR8G8B8A8Unorm);
            using var depth = device.CreateDepthStencilTexture2D(size, size, Format.FormatD32FloatS8X24Uint);
            device.CreateRenderTargetView(target, targetView);
            device.CreateDepthStencilView(depth, depthView);
            var footprint = device.GetCopyableFootprint(target, out var byteCount);
            var depthFootprint = device.GetCopyableFootprint(depth, 0, out var depthBytes);
            using var readback = device.CreateBuffer(byteCount, HeapType.Readback);
            using var depthReadback = device.CreateBuffer(depthBytes, HeapType.Readback);
            var transforms = new GlobalTransformStruct {
                View = Matrix.Identity, Projection = Matrix.Identity, ViewProjection = Matrix.Identity,
                Viewport = new Vector4(size, size, 1f / size, 1f / size),
                Resolution = new Vector4(size, size, 1f / size, 1f / size), EyePos = new Vector3(0, 0, 2),
                DpiScale = 1
            };
            var parameters = new BorderEffectStruct {
                Color = new Vector4(1, 0, 0, 1), Param = new Matrix { M11 = 1, M12 = 1 }, ViewportScale = 1
            };
            var frustum = new BoundingFrustum(Matrix.Identity);
            renderer.BeginFrame();
            context.Reset();
            context.ClearRenderTarget(target, targetView, [0, 0, 0, 1]);
            context.ClearDepthStencil(depth, depthView, 0.25f, 7);
            renderer.RenderOutline(context, nodes,
                selected => catalog.ResolvePostEffectGeometry(selected, DefaultPassNames.EffectOutlineP1,
                    Format.FormatD32FloatS8X24Uint),
                catalog.ResolvePostProcess(technique, DefaultPassNames.EffectBlurVertical),
                catalog.ResolvePostProcess(technique, DefaultPassNames.EffectBlurHorizontal),
                catalog.ResolvePostProcess(technique, DefaultPassNames.MeshOutline),
                in parameters, blur, in transforms, false, ref frustum, target, targetView, depthView, size, size);
            context.Transition(target, ResourceStates.CopySource);
            context.CopyTextureToBuffer(readback, target, in footprint);
            context.Transition(depth, ResourceStates.CopySource);
            context.CopyTextureToBuffer(depthReadback, depth, in depthFootprint, 0);
            context.Close();
            queue.Execute(context);
            fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(10));
            var pixels = readback.Read(checked((int)byteCount));
            var depths = depthReadback.Read(checked((int)depthBytes));
            var exteriorEnergy = 0;
            for (uint y = 0; y < size; y++)
                for (uint x = 0; x < size; x++) {
                    var pixel = checked((int)(footprint.Offset + y * footprint.Footprint.RowPitch + x * 4));
                    var interior = x >= size / 4 && x < size * 3 / 4 && y >= size / 4 && y < size * 3 / 4;
                    if (interior || blur == 0) {
                        Assert.Equal(0, pixels[pixel]);
                        Assert.Equal(0, pixels[pixel + 1]);
                        Assert.Equal(0, pixels[pixel + 2]);
                    } else exteriorEnergy += pixels[pixel] + pixels[pixel + 1] + pixels[pixel + 2];
                    var depthOffset = checked((int)(depthFootprint.Offset + y * depthFootprint.Footprint.RowPitch + x * 4));
                    Assert.Equal(0.25f, BitConverter.ToSingle(depths, depthOffset));
                }
            if (blur > 0) Assert.True(exteriorEnergy > 1000);
            Assert.Same(geometry, node.Geometry);
        }
        renderer.Dispose();
        Assert.Equal(0, resources.Count);
        Assert.Equal(0, samplers.Count);
        device.ThrowIfDeviceRemoved();
    }
}

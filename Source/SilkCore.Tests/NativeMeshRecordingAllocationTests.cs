using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Shaders;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Matrix = Silk.NET.Maths.Matrix4X4<float>;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace HelixToolkit.SharpDX.Core.Native;

/// <summary>Measures warmed actual mesh recording with unchanged and empty material texture slots.</summary>
/// <param name="output">Receives allocation totals after measurement completes.</param>
public sealed class NativeMeshRecordingAllocationTests(ITestOutputHelper output) {
    /// <summary>Records every mesh on every measured frame while reusing its texture-source callbacks.</summary>
    /// <param name="textured">Whether the existing diffuse/albedo slot contains an unchanged real texture.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Warp")]
    public void WarmedMeshRecordingReusesTextureCallbacks(bool textured) {
        using var device = SilkD3D12DeviceFactory.CreateDefault(driverType: SilkDriverType.Warp);
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
        var texture = textured ? new TextureModel([Vector4.One], 1, 1) : null;
        using var diffuse = CreateMesh(new DiffuseMaterialCore { DiffuseMap = texture });
        using var phong = CreateMesh(new PhongMaterialCore { DiffuseMap = texture });
        using var pbr = CreateMesh(new PBRMaterialCore { AlbedoMap = texture });
        var nodes = new FastList<SceneNode> { diffuse, phong, pbr };
        Func<SceneNode, ShaderPass?> selectPass = catalog.Resolve;
        var transforms = new GlobalTransformStruct {
            View = Matrix.Identity, Projection = Matrix.Identity, ViewProjection = Matrix.Identity,
            Viewport = new Vector4(16, 16, 1f / 16, 1f / 16),
            Resolution = new Vector4(16, 16, 1f / 16, 1f / 16), EyePos = new Vector3(0, 0, 2), DpiScale = 1
        };
        for (var index = 0; index < 128; index++) {
            Assert.Equal(nodes.Count, Record());
            CompleteFrame();
        }

        const int iterations = 256;
        long allocated = 0;
        var recorded = 0;
        for (var index = 0; index < iterations; index++) {
            var before = GC.GetAllocatedBytesForCurrentThread();
            recorded += Record();
            allocated += GC.GetAllocatedBytesForCurrentThread() - before;
            CompleteFrame();
        }

        Assert.Equal(iterations * nodes.Count, recorded);
        output.WriteLine($"Mesh recording (textured={textured}): {allocated} bytes / {iterations} frames / {recorded} draws.");
        Assert.InRange(allocated, 0, 256L * iterations);

        /// <summary>Records actual mesh commands and frame cleanup; all delegates are retained outside this pass.</summary>
        /// <returns>The renderer's actual successfully recorded mesh draw count.</returns>
        int Record() {
            renderer.BeginFrame();
            context.Reset();
            context.Transition(target, ResourceStates.RenderTarget);
            context.ClearRenderTarget(target, targetView, [0, 0, 0, 1]);
            context.ClearDepthStencil(depth, depthView);
            context.SetRenderTargets(targetView, depthView);
            context.SetViewport(16, 16);
            var frustum = new BoundingFrustum(Matrix.Identity);
            var count = renderer.RenderVisible(context, nodes, selectPass, in transforms, false, ref frustum);
            context.Close();
            return count;
        }

        /// <summary>Completes the submitted commands before any binding or upload buffer is reused.</summary>
        void CompleteFrame() {
            queue.Execute(context);
            fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(10));
            device.ThrowIfDeviceRemoved();
        }
    }

    /// <summary>Creates a genuine indexed mesh with the material used by its actual native draw.</summary>
    /// <param name="material">The existing native mesh material.</param>
    /// <returns>The owned mesh node.</returns>
    private static MeshNode CreateMesh(MaterialCore material) => new() {
        Geometry = new MeshGeometry3D {
            Positions = new Vector3Collection([new(-.8f, -.8f, .5f), new(.8f, -.8f, .5f), new(0, .8f, .5f)]),
            Normals = new Vector3Collection([Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ]),
            Indices = new IntCollection([0, 1, 2])
        },
        Material = material,
        CullMode = CullMode.None
    };
}

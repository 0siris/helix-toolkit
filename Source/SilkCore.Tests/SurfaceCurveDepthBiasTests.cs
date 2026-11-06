using HelixToolkit.SharpDX.Core.Cameras;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Shaders;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Matrix = Silk.NET.Maths.Matrix4X4<float>;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace HelixToolkit.SharpDX.Core.Native;

/// <summary>Protects source surface-curve bias, dynamic pipeline identity and ordinary foreground occlusion.</summary>
public sealed class SurfaceCurveDepthBiasTests {
    /// <summary>Retains every bias component in the native PSO key without creating a graphics device.</summary>
    [Fact]
    public void PipelineKeyIncludesEachDepthBiasComponentTest() {
        var vertex = D3D12ShaderModule.Load("VS", "vsPoint");
        var pixel = D3D12ShaderModule.Load("PS", "psLine");
        var raster = new RasterizerStateDescription {
            FillMode = FillMode.Solid, CullMode = CullMode.None,
            DepthBias = -50000, SlopeScaledDepthBias = 0, DepthBiasClamp = -1000
        };
        var original = D3D12PipelineStateKey.Graphics(vertex, pixel, rasterizerState: raster);
        raster.DepthBias = -1000;
        var bias = D3D12PipelineStateKey.Graphics(vertex, pixel, rasterizerState: raster);
        raster.SlopeScaledDepthBias = -50;
        var slope = D3D12PipelineStateKey.Graphics(vertex, pixel, rasterizerState: raster);
        raster.DepthBiasClamp = -0.00001f;
        var clamp = D3D12PipelineStateKey.Graphics(vertex, pixel, rasterizerState: raster);

        Assert.NotEqual(original, bias);
        Assert.NotEqual(bias, slope);
        Assert.NotEqual(slope, clamp);
    }

    /// <summary>Uses per-node rasterizers in registered material stages, including attached updates.</summary>
    /// <param name="kind">The existing geometry family sharing the material-pass resolver.</param>
    [Theory]
    [InlineData("line")]
    [InlineData("point")]
    [InlineData("billboard")]
    [InlineData("mesh")]
    [Trait("Category", "Warp")]
    public void MaterialPassTracksAttachedRasterChangesTest(string kind) {
        using var device = SilkD3D12DeviceFactory.CreateDefault(driverType: SilkDriverType.Warp);
        VerifySoftwareAdapter(device);
        using var catalog = new D3D12ScenePassCatalog(device, Format.FormatR8G8B8A8Unorm,
            Format.FormatD32FloatS8X24Uint);
        using GeometryNode node = kind switch {
            "line" => new LineNode { Geometry = new LineGeometry3D {
                Positions = new Vector3Collection([Vector3.Zero, Vector3.UnitX]),
                Indices = new IntCollection([0, 1]) }, Material = new LineMaterialCore() },
            "point" => new PointNode { Geometry = new PointGeometry3D {
                Positions = new Vector3Collection([Vector3.Zero]) }, Material = new PointMaterialCore() },
            "billboard" => new BillboardNode { Geometry = new BillboardSingleText3D(),
                Material = new BillboardMaterialCore() },
            _ => CreateMesh(-1, 1, 0, new Vector4(0, 1, 0, 1))
        };
        node.DepthBias = -50000;
        node.SlopeScaledDepthBias = 0;
        var attached = node.Attach();
        var core = Assert.IsAssignableFrom<GeometryRenderCore>(node.RenderCore);
        var initial = ResolveStages();
        Assert.True(attached);
        Assert.Equal(-50000, core.RasterDescription.DepthBias);
        Assert.Equal(-1000, core.RasterDescription.DepthBiasClamp);

        node.DepthBias = -1000;
        var biased = ResolveStages();
        node.SlopeScaledDepthBias = -50;
        var sloped = ResolveStages();
        var raster = core.RasterDescription;
        raster.DepthBiasClamp = -0.00001f;
        core.RasterDescription = raster;
        var clamped = ResolveStages();
        for (var index = 0; index < initial.Length; index++) {
            Assert.NotSame(initial[index].D3D12PipelineState, biased[index].D3D12PipelineState);
            Assert.NotSame(biased[index].D3D12PipelineState, sloped[index].D3D12PipelineState);
            Assert.NotSame(sloped[index].D3D12PipelineState, clamped[index].D3D12PipelineState);
        }

        node.DepthBias = -50000;
        node.SlopeScaledDepthBias = 0;
        var restored = ResolveStages();
        for (var index = 0; index < initial.Length; index++)
            Assert.Same(initial[index], restored[index]);

        /// <summary>Resolves registered material stages without submitting a frame.</summary>
        /// <returns>The opaque pass, plus registered transparency stages for mesh and billboard nodes.</returns>
        ShaderPass[] ResolveStages() {
            var opaque = Assert.IsType<ShaderPass>(catalog.Resolve(node));
            // The built-in line and point techniques register no OIT passes.
            if (kind is "line" or "point") return [opaque];
            return [
                opaque,
                Assert.IsType<ShaderPass>(catalog.ResolveWeightedTransparency(node)),
                Assert.IsType<ShaderPass>(catalog.ResolveDepthPeelingInitialization(node)),
                Assert.IsType<ShaderPass>(catalog.ResolveDepthPeeling(node))
            ];
        }
    }

    /// <summary>Restores an on-surface ribbon while a separate nearer mesh still hides its left half.</summary>
    /// <param name="bias">The unchanged application bias: TrimGum or TeethSeparation.</param>
    /// <param name="slope">The corresponding unchanged slope scale.</param>
    /// <param name="perspective">Whether to use the ordinary perspective rather than orthographic camera.</param>
    [Theory]
    [InlineData(-50000, 0f, false)]
    [InlineData(-50000, 0f, true)]
    [InlineData(-1000, -50f, false)]
    [InlineData(-1000, -50f, true)]
    [Trait("Category", "Warp")]
    public void SurfaceRibbonRetainsForegroundOcclusionTest(int bias, float slope, bool perspective) {
        using var device = SilkD3D12DeviceFactory.CreateDefault(driverType: SilkDriverType.Warp);
        VerifySoftwareAdapter(device);
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
        using var target = device.CreateRenderTargetTexture2D(32, 32, Format.FormatR8G8B8A8Unorm);
        using var depthHeap = device.CreateDescriptorHeap(DescriptorHeapType.Dsv, 1);
        using var depthView = depthHeap.Allocate();
        using var depth = device.CreateDepthStencilTexture2D(32, 32, Format.FormatD32FloatS8X24Uint);
        device.CreateRenderTargetView(target, targetView);
        device.CreateDepthStencilView(depth, depthView);
        var footprint = device.GetCopyableFootprint(target, out var size);
        using var readback = device.CreateBuffer(size, HeapType.Readback);
        using var surface = CreateMesh(-1, 1, 0, new Vector4(0, 1, 0, 1));
        using var foreground = CreateMesh(-1, 0, .5f, new Vector4(0, 0, 1, 1));
        using var line = new LineNode {
            Geometry = new LineGeometry3D {
                Positions = new Vector3Collection([new(-.8f, 0, -.00001f), new(.8f, 0, -.00001f)]),
                Indices = new IntCollection([0, 1])
            },
            Material = new LineMaterialCore {
                LineColor = new Vector4(1, 0, 0, 1), Thickness = 2, Smoothness = 0,
                FixedSize = true, EnableDistanceFading = false
            }
        };
        var nodes = new FastList<SceneNode> { surface, foreground, line };
        ProjectionCameraCore camera = perspective
            ? new PerspectiveCameraCore { FieldOfView = 60 }
            : new OrthographicCameraCore { Width = 2 };
        camera.Position = new Vector3(0, 0, 2);
        camera.LookDirection = new Vector3(0, 0, -2);
        camera.UpDirection = Vector3.UnitY;
        camera.NearPlaneDistance = 1;
        camera.FarPlaneDistance = 10;
        var view = camera.CreateViewMatrix();
        var projection = camera.CreateProjectionMatrix(1);
        var transforms = new GlobalTransformStruct {
            View = view, Projection = projection, ViewProjection = view * projection,
            Viewport = new Vector4(32, 32, 1f / 32, 1f / 32),
            Resolution = new Vector4(32, 32, 1f / 32, 1f / 32),
            EyePos = camera.Position, IsPerspective = perspective, DpiScale = 1
        };
        var unbiased = Render();
        line.DepthBias = bias;
        line.SlopeScaledDepthBias = slope;
        var biased = Render();
        line.DepthBias = 0;
        line.SlopeScaledDepthBias = 0;
        var restored = Render();

        Assert.True(unbiased[4] < 10);
        Assert.True(biased[4] > 200);
        Assert.True(biased[0] < 10);
        Assert.True(biased[2] > 200);
        Assert.Equal(unbiased, restored);

        /// <summary>Reads the covered and uncovered center-row pixels after one bounded software submission.</summary>
        /// <returns>The two actual RGBA pixels.</returns>
        byte[] Render() {
            var frustum = new BoundingFrustum(transforms.ViewProjection);
            renderer.BeginFrame();
            context.Reset();
            context.Transition(target, ResourceStates.RenderTarget);
            context.ClearRenderTarget(target, targetView, [0, 0, 0, 1]);
            context.ClearDepthStencil(depth, depthView);
            context.SetRenderTargets(targetView, depthView);
            context.SetViewport(32, 32);
            renderer.RenderVisible(context, nodes, catalog.Resolve, in transforms, false, ref frustum);
            context.Transition(target, ResourceStates.CopySource);
            context.CopyTextureToBuffer(readback, target, in footprint);
            context.Close();
            queue.Execute(context);
            fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(10));
            device.ThrowIfDeviceRemoved();
            var rowOffset = footprint.Offset + 16UL * footprint.Footprint.RowPitch;
            var covered = readback.Read(4, rowOffset + 10UL * 4);
            var uncovered = readback.Read(4, rowOffset + 22UL * 4);
            return [..covered, ..uncovered];
        }
    }

    /// <summary>Creates ordinary solid source geometry with no shader or topmost override.</summary>
    /// <param name="left">The left world-space edge.</param>
    /// <param name="right">The right world-space edge.</param>
    /// <param name="z">The world-space surface depth.</param>
    /// <param name="color">The unlit surface color.</param>
    /// <returns>The owned native scene node.</returns>
    private static MeshNode CreateMesh(float left, float right, float z, Vector4 color) => new() {
        Geometry = new MeshGeometry3D {
            Positions = new Vector3Collection([new(left, -1, z), new(right, -1, z),
                new(right, 1, z), new(left, 1, z)]),
            Indices = new IntCollection([0, 1, 2, 0, 2, 3])
        },
        Material = new DiffuseMaterialCore { DiffuseColor = color, EnableUnLit = true },
        CullMode = CullMode.None
    };

    /// <summary>Rejects any device whose actual LUID does not identify Microsoft's software WARP adapter.</summary>
    /// <param name="device">The explicitly requested WARP device, with no hardware fallback.</param>
    private static unsafe void VerifySoftwareAdapter(SilkD3D12Device device) {
        ComPtr<IDXGIFactory4> factory = default;
        ComPtr<IDXGIAdapter1> adapter = default;
        try {
            SilkMarshal.ThrowHResult(SilkD3D12DeviceFactory.DxgiApi.CreateDXGIFactory2(0, out factory));
            var luid = device.NativeDevice.GetAdapterLuid();
            SilkMarshal.ThrowHResult(factory.EnumAdapterByLuid(luid, out adapter));
            AdapterDesc1 description;
            SilkMarshal.ThrowHResult(adapter.GetDesc1(&description));
            Assert.NotEqual(0u, description.Flags & (uint)AdapterFlag.Software);
            Assert.Equal(0x1414u, description.VendorId);
            Assert.Equal(0x8cu, description.DeviceId);
        } finally {
            adapter.Dispose();
            factory.Dispose();
        }
    }
}

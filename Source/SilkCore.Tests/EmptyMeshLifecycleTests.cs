using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Cameras;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using CullMode = HelixToolkit.SharpDX.Core.Native.CullMode;
using Matrix = Silk.NET.Maths.Matrix4X4<float>;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace SilkCore.Tests;

/// <summary>Exercises original mesh eligibility and the productive native empty-to-populated lifecycle.</summary>
/// <remarks>All cases are UNRUN source additions. WARP cases use real headless owners and completed readback.</remarks>
public sealed class EmptyMeshLifecycleTests
{
    /// <summary>Retains original mesh eligibility across attached geometry replacement without a device.</summary>
    /// <param name="emptyKind">Zero for null geometry, one/two for absent positions, three/four for absent indices.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [Trait("Category", "Unit")]
    public void GeometryReplacementPreservesOriginalEligibility(int emptyKind)
    {
        using var context = new RenderContext(new PerspectiveCameraCore(), 16, 16);
        var geometry = CreateTriangle();
        using var node = new MeshNode { Geometry = geometry, Material = new DiffuseMaterialCore() };
        var attached = node.Attach();
        node.Update(context);
        Assert.True(attached);
        Assert.True(node.GeometryValid);
        Assert.True(node.IsRenderable);

        var empty = CreateEmptyGeometry(emptyKind);
        node.Geometry = empty;
        node.Update(context);
        Assert.Same(empty, node.Geometry);
        Assert.True(node.RenderCore.IsAttached);
        Assert.False(node.GeometryValid);
        Assert.False(node.IsRenderable);

        node.Geometry = geometry;
        node.Update(context);
        Assert.Same(geometry, node.Geometry);
        Assert.True(node.GeometryValid);
        Assert.True(node.IsRenderable);
    }

    /// <summary>Revalidates an attached mesh through collection replacement or explicit in-place notification.</summary>
    /// <param name="replaceCollection">Whether to replace the index collection instead of updating it in place.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Unit")]
    public void IndexNotificationsPreserveOriginalEligibility(bool replaceCollection)
    {
        using var context = new RenderContext(new PerspectiveCameraCore(), 16, 16);
        var geometry = CreateTriangle();
        using var node = new MeshNode { Geometry = geometry, Material = new DiffuseMaterialCore() };
        var attached = node.Attach();
        node.Update(context);
        Assert.True(attached);
        Assert.True(node.IsRenderable);

        SetIndices(geometry, false, replaceCollection);
        node.Update(context);
        Assert.False(node.GeometryValid);
        Assert.False(node.IsRenderable);

        SetIndices(geometry, true, replaceCollection);
        node.Update(context);
        Assert.True(node.GeometryValid);
        Assert.True(node.IsRenderable);
    }

    /// <summary>Skips attached empty replacements and restores real draws through both native attachment routes.</summary>
    /// <param name="useEffects">Whether to use the viewport's effects-manager attachment route.</param>
    /// <param name="emptyKind">Zero for null geometry, one/two for absent positions, three/four for absent indices.</param>
    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(false, 3)]
    [InlineData(false, 4)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    [InlineData(true, 3)]
    [InlineData(true, 4)]
    [Trait("Category", "Warp")]
    public void AttachedGeometryReplacementSkipsEmptyAndRestoresDraws(bool useEffects, int emptyKind)
        => VerifySceneLifecycle(useEffects, (node, render) =>
        {
            render(1);
            node.Geometry = CreateEmptyGeometry(emptyKind);
            render(0);
            render(0);
            Assert.False(node.GeometryValid);
            Assert.False(node.IsRenderable);
            node.Geometry = CreateTriangle();
            render(1);
            Assert.True(node.IsRenderable);
        });

    /// <summary>Skips an existing buffer after its last indices disappear and draws again after they return.</summary>
    /// <param name="useEffects">Whether to use the viewport's effects-manager attachment route.</param>
    /// <param name="replaceCollection">Whether to replace the index collection instead of updating it in place.</param>
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Trait("Category", "Warp")]
    public void AttachedIndexChangesSkipEmptyAndRestoreDraws(bool useEffects, bool replaceCollection)
        => VerifySceneLifecycle(useEffects, (node, render) =>
        {
            var geometry = Assert.IsType<MeshGeometry3D>(node.Geometry);
            render(1);
            SetIndices(geometry, false, replaceCollection);
            render(0);
            SetIndices(geometry, true, replaceCollection);
            render(1);
            Assert.Same(geometry, node.Geometry);
        });

    /// <summary>Completes native buffer readiness when the first primitives arrive without replacing the mesh.</summary>
    /// <param name="useEffects">Whether to use the viewport's effects-manager attachment route.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Warp")]
    public void InitiallyUnindexedMeshGainsFirstDraw(bool useEffects)
        => VerifySceneLifecycle(useEffects, (node, render) =>
        {
            var geometry = Assert.IsType<MeshGeometry3D>(node.Geometry);
            SetIndices(geometry, false, false);
            render(0);
            SetIndices(geometry, true, false);
            render(1);
            Assert.Same(geometry, node.Geometry);
            Assert.True(node.IsRenderable);
        });

    /// <summary>Keeps strict native rejection of malformed nonempty indices through the same scene owner.</summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void NonemptyOutOfRangeIndicesStillFailNativeValidation()
        => VerifySceneLifecycle(true, (node, render) =>
        {
            var geometry = Assert.IsType<MeshGeometry3D>(node.Geometry);
            geometry.Indices = [0, 1, 3];
            Assert.True(node.GeometryValid);
            var error = Assert.Throws<ArgumentException>(() => render(1));
            Assert.Equal("source", error.ParamName);
            Assert.Contains("outside the mesh", error.Message);
        });

    /// <summary>Creates one finite triangle covering the center readback pixel.</summary>
    /// <returns>The ordinary indexed mesh used by the real native buffer factory.</returns>
    private static MeshGeometry3D CreateTriangle() => new()
    {
        Positions = [new(-0.75f, -0.75f, 0.5f), new(0.75f, -0.75f, 0.5f), new(0, 0.75f, 0.5f)],
        Normals = [Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ],
        Indices = [0, 1, 2]
    };

    /// <summary>Creates missing-data cases without repairing them or changing any retained fixture.</summary>
    /// <param name="emptyKind">The missing-data case, from null geometry through an empty index collection.</param>
    /// <returns>The requested absent or incomplete geometry.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The case is not defined.</exception>
    private static MeshGeometry3D? CreateEmptyGeometry(int emptyKind) => emptyKind switch
    {
        0 => null,
        1 => new MeshGeometry3D { Indices = [0, 1, 2] },
        2 => new MeshGeometry3D { Positions = [], Indices = [0, 1, 2] },
        3 => new MeshGeometry3D { Positions = CreateTriangle().Positions },
        4 => new MeshGeometry3D { Positions = CreateTriangle().Positions, Indices = [] },
        _ => throw new ArgumentOutOfRangeException(nameof(emptyKind))
    };

    /// <summary>Publishes actual index changes using the geometry's existing notification contract.</summary>
    /// <param name="geometry">The same mesh generation throughout an index lifecycle.</param>
    /// <param name="populated">Whether to publish the triangle's real indices.</param>
    /// <param name="replaceCollection">Whether to replace the collection instead of updating it in place.</param>
    private static void SetIndices(MeshGeometry3D geometry, bool populated, bool replaceCollection)
    {
        if (replaceCollection)
        {
            geometry.Indices = populated ? [0, 1, 2] : [];
            return;
        }

        var indices = geometry.Indices!;
        indices.Clear();
        if (populated)
        {
            indices.Add(0);
            indices.Add(1);
            indices.Add(2);
        }
        geometry.UpdateTriangles();
    }

    /// <summary>Reuses the native device, catalog, scene renderer and fence/readback pattern without a window.</summary>
    /// <param name="useEffects">Whether the actual preparation and catalog receive an effects manager.</param>
    /// <param name="exercise">The lifecycle operating on the real node and a completed-frame assertion.</param>
    private static void VerifySceneLifecycle(bool useEffects, Action<MeshNode, Action<int>> exercise)
    {
        using var device = SilkD3D12DeviceFactory.CreateDefault(driverType: SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var commands = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var effects = useEffects ? new DefaultEffectsManager() : null;
        using var resources = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 2048, true);
        using var samplers = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 128, true);
        using var renderer = new SilkD3D12SceneRenderer(device, resources, samplers);
        using var catalog = new D3D12ScenePassCatalog(device, Format.FormatR8G8B8A8Unorm,
            Format.FormatD32FloatS8X24Uint, effectsManager: effects);
        using var targets = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        using var targetView = targets.Allocate();
        using var target = device.CreateRenderTargetTexture2D(16, 16, Format.FormatR8G8B8A8Unorm);
        using var depths = device.CreateDescriptorHeap(DescriptorHeapType.Dsv, 1);
        using var depthView = depths.Allocate();
        using var depth = device.CreateDepthStencilTexture2D(16, 16, Format.FormatD32FloatS8X24Uint);
        device.CreateRenderTargetView(target, targetView);
        device.CreateDepthStencilView(depth, depthView);
        var footprint = device.GetCopyableFootprint(target, out var bytes);
        using var readback = device.CreateBuffer(bytes, HeapType.Readback);
        using var update = new RenderContext(new PerspectiveCameraCore(), 16, 16);
        using var node = new MeshNode
        {
            Geometry = CreateTriangle(),
            Material = new DiffuseMaterialCore { EnableUnLit = true, DiffuseColor = new(1, 0, 0, 1) },
            IsHitTestVisible = false,
            CullMode = CullMode.None
        };
        var transforms = new GlobalTransformStruct
        {
            View = Matrix.Identity, Projection = Matrix.Identity, ViewProjection = Matrix.Identity,
            Viewport = new Vector4(16, 16, 1f / 16, 1f / 16),
            Resolution = new Vector4(16, 16, 1f / 16, 1f / 16), EyePos = new Vector3(0, 0, 2), DpiScale = 1
        };

        exercise(node, Render);
        renderer.Dispose();
        Assert.False(node.RenderCore.IsAttached);

        /// <summary>Prepares and records a real scene frame, then checks its draw count and center RGBA pixel.</summary>
        /// <param name="expectedDraws">Zero for the blue background or one for the red triangle.</param>
        void Render(int expectedDraws)
        {
            renderer.BeginFrame();
            var candidates = renderer.PrepareScene([node], update, effects);
            commands.Reset();
            commands.Transition(target, ResourceStates.RenderTarget);
            commands.ClearRenderTarget(target, targetView, [0, 0, 1, 1]);
            commands.ClearDepthStencil(depth, depthView);
            commands.SetRenderTargets(targetView, depthView);
            commands.SetViewport(16, 16);
            var frustum = new BoundingFrustum(Matrix.Identity);
            var draws = renderer.RenderVisible(commands, candidates, catalog.Resolve, in transforms, false, ref frustum);
            commands.Transition(target, ResourceStates.CopySource);
            commands.CopyTextureToBuffer(readback, target, in footprint);
            commands.Close();
            queue.Execute(commands);
            fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(10));
            device.ThrowIfDeviceRemoved();
            var pixel = readback.Read(4, footprint.Offset + 8UL * footprint.Footprint.RowPitch + 8UL * 4);
            byte[] expectedPixel = expectedDraws == 0 ? [0, 0, 255, 255] : [255, 0, 0, 255];
            Assert.Equal(expectedDraws, draws);
            Assert.Equal(expectedPixel, pixel);
        }
    }
}

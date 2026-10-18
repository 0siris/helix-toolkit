using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Matrix = Silk.NET.Maths.Matrix4X4<float>;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace SilkCore.Tests;

/// Exercises independent native renderers submitting first-use WARP pipelines concurrently.
public sealed class NativeConcurrentSubmissionTests {
    /// <summary>
    ///     Verifies concurrent queue submissions preserve physical pixels through repeated owner replacement.
    /// </summary>
    /// <returns>A task completing after every independent renderer has been read back and disposed.</returns>
    [Fact]
    [Trait("Category", "Warp")]
    public async Task WarpConcurrentRendererSubmissionTest() {
        using var start = new Barrier(3);
        var workers = Enumerable.Range(0, 3).Select(worker => Task.Factory.StartNew(() => {
            for (var generation = 0; generation < 4; generation++)
                Render(start, worker);
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();
        await Task.WhenAll(workers);
    }

    /// <summary>
    ///     Creates a complete offscreen owner, records original transparent geometry, and reads its fenced result.
    /// </summary>
    /// <param name="start">The barrier aligning first submissions from independent queues.</param>
    /// <param name="worker">The independently asserted material color index.</param>
    /// <exception cref="TimeoutException">The independent owners did not reach submission within thirty seconds.</exception>
    private static void Render(Barrier start, int worker) {
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
        using var target = device.CreateRenderTargetTexture2D(32, 32, Format.FormatR8G8B8A8Unorm);
        using var depthHeap = device.CreateDescriptorHeap(DescriptorHeapType.Dsv, 1);
        using var depthView = depthHeap.Allocate();
        using var depth = device.CreateDepthStencilTexture2D(32, 32, Format.FormatD32FloatS8X24Uint);
        device.CreateRenderTargetView(target, targetView);
        device.CreateDepthStencilView(depth, depthView);
        var footprint = device.GetCopyableFootprint(target, out var byteCount);
        using var readback = device.CreateBuffer(byteCount, HeapType.Readback);
        var color = worker switch {
            0 => new Vector4(1, 0, 0, 0.5f),
            1 => new Vector4(0, 1, 0, 0.5f),
            _ => new Vector4(1, 1, 0, 0.5f)
        };
        using var node = new MeshNode {
            Geometry = new MeshGeometry3D {
                Positions = new Vector3Collection([
                    new(-0.75f, 0.75f, 0.5f), new(0.75f, 0.75f, 0.5f),
                    new(-0.75f, -0.75f, 0.5f), new(0.75f, -0.75f, 0.5f)
                ]),
                Normals = new Vector3Collection([Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ]),
                Indices = new IntCollection([0, 1, 2, 2, 1, 3])
            },
            Material = new DiffuseMaterialCore { DiffuseColor = color, EnableUnLit = true },
            IsTransparent = true,
            CullMode = HelixToolkit.SharpDX.Core.Native.CullMode.None
        };
        var nodes = new FastList<SceneNode> { node };
        var transforms = new GlobalTransformStruct {
            View = Matrix.Identity, Projection = Matrix.Identity, ViewProjection = Matrix.Identity,
            Viewport = new Vector4(32, 32, 1f / 32, 1f / 32),
            Resolution = new Vector4(32, 32, 1f / 32, 1f / 32), EyePos = new Vector3(0, 0, 2), DpiScale = 1
        };
        var frustum = new BoundingFrustum(Matrix.Identity);
        renderer.BeginFrame();
        context.Reset();
        context.ClearRenderTarget(target, targetView, [0, 0, 1, 1]);
        context.ClearDepthStencil(depth, depthView);
        context.SetRenderTargets(targetView, depthView);
        context.SetViewport(32, 32);
        renderer.RenderDepthPeeling(context, nodes, catalog.ResolveDepthPeelingInitialization,
            catalog.ResolveDepthPeeling, catalog.ResolveDepthPeelingComposition(), 4, in transforms,
            false, ref frustum, target, targetView, depthView, 32, 32, null, null,
            catalog.ResolveBoneSkinning, null);
        context.Transition(target, ResourceStates.CopySource);
        context.CopyTextureToBuffer(readback, target, in footprint);
        context.Close();
        if (!start.SignalAndWait(TimeSpan.FromSeconds(30)))
            throw new TimeoutException("Concurrent native owners did not reach submission");
        queue.Execute(context);
        fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(15));
        device.ThrowIfDeviceRemoved();
        var pixels = readback.Read(checked((int) byteCount));
        for (var y = 0; y < 32; y++) {
            for (var x = 0; x < 32; x++) {
                var offset = checked((int) footprint.Offset + y * (int) footprint.Footprint.RowPitch + x * 4);
                var covered = x >= 4 && x < 28 && y >= 4 && y < 28;
                Assert.InRange(pixels[offset], covered && color.X > 0 ? 127 : 0, covered && color.X > 0 ? 128 : 0);
                Assert.InRange(pixels[offset + 1], covered && color.Y > 0 ? 127 : 0, covered && color.Y > 0 ? 128 : 0);
                Assert.InRange(pixels[offset + 2], covered ? 127 : 255, covered ? 128 : 255);
                Assert.Equal(255, pixels[offset + 3]);
            }
        }
        node.Dispose();
        renderer.BeginFrame();
        renderer.Dispose();
        Assert.Equal(0, resources.Count);
        Assert.Equal(0, samplers.Count);
    }
}

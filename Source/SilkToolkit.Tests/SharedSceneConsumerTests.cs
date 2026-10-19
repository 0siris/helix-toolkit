using System.Reflection;
using System.Windows.Threading;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Cameras;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Controls;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Xunit;
using CullMode = HelixToolkit.SharpDX.Core.Native.CullMode;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace SilkToolkit.Tests;

/// <summary>Checks shared root notifications and actual independent native consumers of application-owned nodes.</summary>
[Collection(WpfCollection.Name)]
public sealed class SharedSceneConsumerTests {
    /// <summary>Defers an entire manager replacement while any consumer prepares and coalesces the latest request.</summary>
    /// <returns>The dispatcher-owned manager transition and external ownership verification.</returns>
    [Fact]
    public Task SharedManagerChangeWaitsForEveryConsumerAndUsesLatestValue() => StaThread.RunAsync(async () => {
        using var original = new DefaultEffectsManager(new EffectsManagerConfiguration { EnableSoftwareRendering = true });
        using var intermediate = new DefaultEffectsManager(new EffectsManagerConfiguration { EnableSoftwareRendering = true });
        using var latest = new DefaultEffectsManager(new EffectsManagerConfiguration { EnableSoftwareRendering = true });
        using var first = new Viewport3DX { EffectsManager = original };
        using var second = new Viewport3DX { EffectsManager = original };
        var container = new ModelContainer3DX { EffectsManager = original };
        container.RegisterConsumer(first);
        container.RegisterConsumer(second);
        var preparing = typeof(Viewport3DX).GetField("isD3D12FrameUpdating", BindingFlags.Instance | BindingFlags.NonPublic)!;
        preparing.SetValue(second, true);
        container.EffectsManager = intermediate;
        container.EffectsManager = latest;
        container.RequestSharedResourceRetirement();
        Assert.Same(original, first.EffectsManager);
        Assert.Same(original, second.EffectsManager);
        Assert.True(container.IsSynchronizingEffectsManager);
        preparing.SetValue(second, false);
        await Dispatcher.CurrentDispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        Assert.Same(latest, first.EffectsManager);
        Assert.Same(latest, second.EffectsManager);
        Assert.False(container.IsSynchronizingEffectsManager);
        Assert.False(original.IsDisposed);
        Assert.False(intermediate.IsDisposed);
        Assert.False(latest.IsDisposed);
        container.UnregisterConsumer(first);
        container.UnregisterConsumer(second);
    });

    /// <summary>Retires same-manager resources without replacing externally supplied host managers with a null container value.</summary>
    /// <returns>The dispatcher-owned shared retirement verification.</returns>
    [Fact]
    public Task ForcedRetirementPreservesIndependentlySuppliedManagers() => StaThread.RunAsync(async () => {
        using var firstEffects = new DefaultEffectsManager(new EffectsManagerConfiguration { EnableSoftwareRendering = true });
        using var secondEffects = new DefaultEffectsManager(new EffectsManagerConfiguration { EnableSoftwareRendering = true });
        using var first = new Viewport3DX { EffectsManager = firstEffects };
        using var second = new Viewport3DX { EffectsManager = secondEffects };
        var container = new ModelContainer3DX();
        container.RegisterConsumer(first);
        container.RegisterConsumer(second);
        var preparing = typeof(Viewport3DX).GetField("isD3D12FrameUpdating", BindingFlags.Instance | BindingFlags.NonPublic)!;
        preparing.SetValue(first, true);
        container.RequestSharedResourceRetirement();
        Assert.True(container.IsSynchronizingEffectsManager);
        Assert.Same(firstEffects, first.EffectsManager);
        Assert.Same(secondEffects, second.EffectsManager);
        preparing.SetValue(first, false);
        await Dispatcher.CurrentDispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        Assert.False(container.IsSynchronizingEffectsManager);
        Assert.Same(firstEffects, first.EffectsManager);
        Assert.Same(secondEffects, second.EffectsManager);
        Assert.Null(container.EffectsManager);
        Assert.False(firstEffects.IsDisposed);
        Assert.False(secondEffects.IsDisposed);
        container.UnregisterConsumer(first);
        container.UnregisterConsumer(second);
    });

    /// <summary>Wakes both real host coordinators, updates subscriptions after reset and releases consumers idempotently.</summary>
    /// <returns>The dispatcher-owned notification verification.</returns>
    [Fact]
    public Task DescendantAndCollectionChangesReachEveryRegisteredHost() => StaThread.RunAsync(() => {
        using var first = new Viewport3DX();
        using var second = new Viewport3DX();
        using var group = new GroupModel3D();
        using var child = new MeshGeometryModel3D();
        group.Children.Add(child);
        var container = new ModelContainer3DX();
        container.Items.Add(group);
        container.RegisterConsumer(first);
        container.RegisterConsumer(first);
        container.RegisterConsumer(second);
        Drain(first, second);

        child.SceneNode.Visible = false;
        Assert.True(first.RenderDemand.HasRenderDemand);
        Assert.True(second.RenderDemand.HasRenderDemand);
        Drain(first, second);
        child.SceneNode.RenderOrder = 3;
        Assert.True(first.RenderDemand.HasRenderDemand);
        Assert.True(second.RenderDemand.HasRenderDemand);
        Drain(first, second);

        container.UnregisterConsumer(first);
        container.UnregisterConsumer(first);
        child.SceneNode.InvalidateRender();
        Assert.False(first.RenderDemand.HasRenderDemand);
        Assert.True(second.RenderDemand.HasRenderDemand);
        Drain(second);
        container.Items.Clear();
        Assert.True(second.RenderDemand.HasRenderDemand);
        Drain(second);
        child.SceneNode.InvalidateRender();
        Assert.False(second.RenderDemand.HasRenderDemand);

        container.Items.Add(group);
        Drain(second);
        child.SceneNode.InvalidateRender();
        Assert.True(second.RenderDemand.HasRenderDemand);
        container.UnregisterConsumer(second);
        Drain(second);
        child.SceneNode.InvalidateRender();
        Assert.False(second.RenderDemand.HasRenderDemand);
        Assert.False(child.SceneNode.IsDisposed);
    });

    /// <summary>Renders both native consumers, retires either current or previous host, and verifies surviving pixels and wakeups.</summary>
    /// <param name="disposeLatest">Whether the most recently prepared consumer leaves first.</param>
    /// <returns>The actual WARP rendering, invalidation and lease verification.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Warp")]
    public Task IndependentNativeConsumersRetainSharedMeshUntilLastRelease(bool disposeLatest) => StaThread.RunAsync(() => {
        using var effects = new DefaultEffectsManager(new EffectsManagerConfiguration { EnableSoftwareRendering = true });
        using var firstHost = new Viewport3DX { EffectsManager = effects };
        using var secondHost = new Viewport3DX { EffectsManager = effects };
        using var model = CreateQuad();
        using var group = new GroupModel3D();
        group.Children.Add(model);
        var node = model.SceneNode;
        var container = new ModelContainer3DX { EffectsManager = effects };
        container.Items.Add(group);
        container.RegisterConsumer(firstHost);
        container.RegisterConsumer(secondHost);
        using var first = new NativeConsumer(effects);
        using var second = new NativeConsumer(effects);
        first.Render(group.SceneNode, firstHost);
        second.Render(group.SceneNode, secondHost);
        first.Render(null, firstHost);
        Assert.True(node.RenderCore.IsAttached);
        Assert.Same(secondHost, node.RenderHost);
        second.Render(group.SceneNode, secondHost);
        first.Render(group.SceneNode, firstHost);
        second.Render(group.SceneNode, secondHost);
        Assert.Same(secondHost, node.RenderHost);
        Drain(firstHost, secondHost);
        node.InvalidateRender();
        Assert.True(firstHost.RenderDemand.HasRenderDemand);
        Assert.True(secondHost.RenderDemand.HasRenderDemand);

        var leaving = disposeLatest ? second : first;
        var surviving = disposeLatest ? first : second;
        var leavingHost = disposeLatest ? secondHost : firstHost;
        var survivingHost = disposeLatest ? firstHost : secondHost;
        container.UnregisterConsumer(leavingHost);
        leaving.Dispose();
        leavingHost.Dispose();
        Assert.True(node.IsAttached);
        Assert.True(node.RenderCore.IsAttached);
        Assert.False(node.IsDisposed);
        Assert.Same(survivingHost, node.RenderHost);
        Assert.Same(effects, node.EffectsManager);
        Drain(survivingHost);
        node.InvalidateRender();
        Assert.True(survivingHost.RenderDemand.HasRenderDemand);
        surviving.Render(group.SceneNode, survivingHost);
        surviving.Render(null, survivingHost);
        Assert.False(node.RenderCore.IsAttached);
        Assert.Null(node.RenderHost);
        Drain(survivingHost);
        node.InvalidateRender();
        Assert.True(survivingHost.RenderDemand.HasRenderDemand);
        surviving.Render(group.SceneNode, survivingHost);

        container.UnregisterConsumer(survivingHost);
        surviving.Dispose();
        Assert.False(node.IsAttached);
        Assert.False(node.RenderCore.IsAttached);
        Assert.Null(node.RenderHost);
        Assert.False(node.IsDisposed);
        Assert.False(effects.IsDisposed);

        // A resource generation can change managers after all old consumers have retired.
        using var replacement = new DefaultEffectsManager(new EffectsManagerConfiguration { EnableSoftwareRendering = true });
        survivingHost.EffectsManager = replacement;
        container.EffectsManager = replacement;
        container.RegisterConsumer(survivingHost);
        using var reloaded = new NativeConsumer(replacement);
        reloaded.Render(group.SceneNode, survivingHost);
        Assert.Same(replacement, node.EffectsManager);
        Drain(survivingHost);
        node.InvalidateRender();
        Assert.True(survivingHost.RenderDemand.HasRenderDemand);
        container.UnregisterConsumer(survivingHost);
        reloaded.Dispose();
        Assert.False(replacement.IsDisposed);
    });

    /// <summary>Consumes demand without substituting notification counters for the real viewport coordinator.</summary>
    /// <param name="hosts">The real hosts whose existing budgets are drained.</param>
    private static void Drain(params Viewport3DX[] hosts) {
        foreach (var host in hosts)
            while (host.RenderDemand.TryBeginFrame(out _)) host.RenderDemand.CompleteFrame(true);
    }

    /// <summary>Creates an unlit quad with an independently predictable center pixel.</summary>
    /// <returns>The application-owned WPF mesh wrapper and original node.</returns>
    private static MeshGeometryModel3D CreateQuad() => new() {
        CullMode = CullMode.None,
        Material = new DiffuseMaterial { DiffuseColor = new Vector4(0, 1, 0, 1), EnableUnLit = true },
        Geometry = new MeshGeometry3D {
            Positions = [new(-0.5f, 0.5f, 0), new(0.5f, 0.5f, 0), new(-0.5f, -0.5f, 0), new(0.5f, -0.5f, 0)],
            Normals = [Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ],
            Indices = [0, 1, 2, 2, 1, 3]
        }
    };

    /// <summary>Owns one actual WARP recorder, queue and offscreen target independently of other consumers.</summary>
    private sealed class NativeConsumer : IDisposable {
        /// <summary>The caller-owned native device.</summary>
        private readonly SilkD3D12Device device;
        /// <summary>The consumer's submission queue.</summary>
        private readonly SilkD3D12CommandQueue queue;
        /// <summary>The consumer's retained command context.</summary>
        private readonly SilkD3D12CommandContext context;
        /// <summary>The consumer's completion fence.</summary>
        private readonly SilkD3D12Fence fence;
        /// <summary>The consumer's target descriptor heap.</summary>
        private readonly SilkD3D12DescriptorHeap heap;
        /// <summary>The consumer's offscreen render target view.</summary>
        private readonly SilkD3D12Descriptor view;
        /// <summary>The consumer's offscreen color resource.</summary>
        private readonly SilkD3D12Resource color;
        /// <summary>The productive recorder holding this consumer's native attachment leases.</summary>
        private readonly D3D12FrameRenderer renderer;
        /// <summary>The stable camera shared by this consumer's frames.</summary>
        private readonly OrthographicCameraCore camera = new() {
            Position = new Vector3(0, 0, 2), LookDirection = new Vector3(0, 0, -2),
            UpDirection = Vector3.UnitY, Width = 2, NearPlaneDistance = 0.1f, FarPlaneDistance = 10
        };
        /// <summary>Records whether this consumer has released its native owners.</summary>
        private bool disposed;

        /// <summary>Creates independent rendering resources borrowing the application effects manager.</summary>
        /// <param name="effects">The shared application manager.</param>
        internal NativeConsumer(IEffectsManager effects) {
            device = SilkD3D12DeviceFactory.CreateDefault(driverType: SilkDriverType.Warp);
            queue = device.CreateCommandQueue();
            context = device.CreateCommandContext();
            fence = device.CreateFence();
            heap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
            view = heap.Allocate();
            color = device.CreateRenderTargetTexture2D(32, 24, Format.FormatR8G8B8A8Unorm);
            device.CreateRenderTargetView(color, view);
            renderer = new D3D12FrameRenderer(device, Format.FormatR8G8B8A8Unorm, effects);
        }

        /// <summary>Records a real mesh draw, completes the submission and checks independent green and blue pixels.</summary>
        /// <param name="node">The original shared root, or null to remove this consumer's hierarchy.</param>
        /// <param name="host">The actual viewport receiving node invalidations.</param>
        internal void Render(SceneNode? node, IRenderHost host) {
            var target = new D3D12FrameTarget(color, view, 32, 24, Format.FormatR8G8B8A8Unorm);
            context.Reset();
            SceneNode[] roots = node is null ? [] : [node];
            var draws = renderer.RecordViewportFrame(context, in target, 1, [0, 0, 1, 1], roots, camera,
                false, autoUpdateOctree: false, renderHost: host);
            var footprint = device.GetCopyableFootprint(color, out var bytes);
            using var readback = device.CreateBuffer(bytes, HeapType.Readback);
            context.Transition(color, ResourceStates.CopySource);
            context.CopyTextureToBuffer(readback, color, in footprint);
            context.Close();
            queue.Execute(context);
            fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));
            device.ThrowIfDeviceRemoved();
            Assert.Equal(node is null ? 0 : 1, draws);
            byte[] expectedCenter = node is null ? [0, 0, 255, 255] : [0, 255, 0, 255];
            Assert.Equal(expectedCenter, readback.Read(4,
                footprint.Offset + 12UL * footprint.Footprint.RowPitch + 16UL * 4));
            Assert.Equal(new byte[] { 0, 0, 255, 255 }, readback.Read(4, footprint.Offset));
        }

        /// <summary>Drains this consumer before releasing its leases and its native allocations exactly once.</summary>
        public void Dispose() {
            if (disposed) return;
            fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));
            renderer.Dispose();
            color.Dispose();
            view.Dispose();
            heap.Dispose();
            context.Dispose();
            fence.Dispose();
            queue.Dispose();
            device.Dispose();
            disposed = true;
        }
    }
}

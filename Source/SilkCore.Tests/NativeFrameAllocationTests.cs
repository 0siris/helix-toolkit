using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Cameras;
using HelixToolkit.SharpDX.Core.Model.Scene;
using Silk.NET.Direct3D12;
using Xunit;

namespace HelixToolkit.SharpDX.Core.Native;

/// <summary>Checks actual native preparation reuse, mutation ordering and warmed allocation costs.</summary>
/// <param name="output">Receives measured allocation totals outside the measurement interval.</param>
public sealed class NativeFrameAllocationTests(ITestOutputHelper output) {
    /// <summary>Measures real preparation and completed-frame cleanup without counting skipped frames.</summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarmedPreparationAndCleanupReuseStorage() {
        using var fixture = new Preparation();
        using var root = new ObservedGroup();
        using var child = new ObservedGroup();
        root.AddChildNode(child);
        SceneNode[] roots = [root];
        for (var index = 0; index < 256; index++) {
            fixture.Renderer.BeginFrame();
            fixture.Renderer.PrepareScene(roots, fixture.Context);
        }
        const int iterations = 1000;
        var initialUpdates = root.Updates;
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < iterations; index++) {
            fixture.Renderer.BeginFrame();
            fixture.Renderer.PrepareScene(roots, fixture.Context);
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(iterations, root.Updates - initialUpdates);
        Assert.Equal(root.Updates, child.Updates);
        Assert.Equal(2, fixture.Renderer.PerFrameFlattenedScene.Count);
        output.WriteLine($"Preparation + cleanup: {allocated} bytes / {iterations} actual two-node frames.");
        Assert.Equal(0, allocated);
    }

    /// <summary>Snapshots root membership before updates while observing children inserted by a parent update.</summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void RootSnapshotAndLiveChildrenRetainUpdateOrdering() {
        using var fixture = new Preparation();
        using var root = new ObservedGroup();
        using var insertedChild = new ObservedGroup();
        using var insertedRoot = new ObservedGroup();
        var roots = new List<SceneNode> { root };
        root.AfterUpdate = () => {
            root.AddChildNode(insertedChild);
            roots.Add(insertedRoot);
            root.AfterUpdate = null;
        };
        fixture.Renderer.PrepareScene(roots, fixture.Context);
        Assert.Equal(1, root.Updates);
        Assert.Equal(1, insertedChild.Updates);
        Assert.Equal(0, insertedRoot.Updates);
        Assert.Equal([root, insertedChild], fixture.Renderer.PerFrameFlattenedScene.Select(pair => pair.Value));
        fixture.Renderer.PrepareScene(roots, fixture.Context);
        Assert.Equal(2, insertedChild.Updates);
        Assert.Equal(1, insertedRoot.Updates);
    }

    /// <summary>Releases scratch ownership after failed updates and rejects nested preparation without corruption.</summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void FailedAndNestedUpdatesLeaveReusableStateAvailable() {
        using var fixture = new Preparation();
        using var root = new ObservedGroup();
        SceneNode[] roots = [root];
        root.AfterUpdate = () => throw new InvalidOperationException("Expected update failure.");
        Assert.Throws<InvalidOperationException>(() => fixture.Renderer.PrepareScene(roots, fixture.Context));
        root.AfterUpdate = () => fixture.Renderer.PrepareScene(roots, fixture.Context);
        Assert.Throws<InvalidOperationException>(() => fixture.Renderer.PrepareScene(roots, fixture.Context));
        root.AfterUpdate = null;
        fixture.Renderer.PrepareScene(roots, fixture.Context);
        Assert.Same(root, Assert.Single(fixture.Renderer.PerFrameFlattenedScene).Value);
    }

    /// <summary>Observes genuine node updates without allocating in the measured pass.</summary>
    private sealed class ObservedGroup : GroupNode {
        /// <summary>Gets the number of actual update invocations.</summary>
        internal int Updates { get; private set; }

        /// <summary>Gets or sets an optional mutation or failure callback outside allocation measurements.</summary>
        internal Action? AfterUpdate { get; set; }

        /// <inheritdoc />
        public override void Update(RenderContext context) {
            base.Update(context);
            Updates++;
            AfterUpdate?.Invoke();
        }
    }

    /// <summary>Owns real WARP preparation resources independently of a WPF presentation surface.</summary>
    private sealed class Preparation : IDisposable {
        /// <summary>Owns the native device.</summary>
        private readonly SilkD3D12Device device = SilkD3D12DeviceFactory.CreateDefault(driverType: SilkDriverType.Warp);

        /// <summary>Owns the resource descriptors.</summary>
        private readonly SilkD3D12DescriptorHeap resources;

        /// <summary>Owns the sampler descriptors.</summary>
        private readonly SilkD3D12DescriptorHeap samplers;

        /// <summary>Gets the actual scene preparer and frame-cache owner.</summary>
        internal SilkD3D12SceneRenderer Renderer { get; }

        /// <summary>Gets the stable update context.</summary>
        internal RenderContext Context { get; } = new(new PerspectiveCameraCore(), 64, 64);

        /// <summary>Creates the real native owners before any allocation measurement.</summary>
        internal Preparation() {
            resources = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 1024, true);
            samplers = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 128, true);
            Renderer = new SilkD3D12SceneRenderer(device, resources, samplers);
        }

        /// <inheritdoc />
        public void Dispose() {
            Renderer.Dispose();
            Context.Dispose();
            samplers.Dispose();
            resources.Dispose();
            device.Dispose();
        }
    }
}

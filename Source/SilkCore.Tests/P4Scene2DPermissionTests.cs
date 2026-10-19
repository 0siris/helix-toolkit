using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model.Scene2D;
using HelixToolkit.SharpDX.Core.Native;
using Silk.NET.Direct3D12;
using ValidSphere;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace SilkCore.Tests;

/// <summary>Checks original Scene2D callbacks, resource epochs and retained preparation storage.</summary>
public sealed class P4Scene2DPermissionTests {
    /// <summary>Restores scoped permission after an exception and keeps the original null-permission APIs usable.</summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void ThrowingUpdateRestoresPermissionAndOriginalNativeApisRemainIndependent() {
        using var effects = new EffectsManager();
        using var context = new RenderContext2D(effects.DeviceContext2D, effects, 16, 16);
        using var node = new RectangleNode2D();
        node.Attach(1, static () => false);
        Assert.False(node.IsAttached);
        Assert.Null(node.ScenePreparationPermission);
        node.Attach();
        EventHandler<SceneNode2D.UpdateEventArgs> throwing = static (_, _) =>
            throw new InvalidOperationException("Original update callback failure.");
        node.UpdateRequested += throwing;
        Assert.Throws<InvalidOperationException>(() => node.Update(context, static () => true));
        Assert.Null(node.ScenePreparationPermission);
        node.UpdateRequested -= throwing;
        node.Update(context);
        Assert.True(node.IsAttached);
        Assert.True(node.IsRenderable);
    }

    /// <summary>Stops a shape's own update hook before its dirty stroke style reads a disposed factory.</summary>
    /// <param name="border">Whether to exercise the independent border stroke implementation.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Warp")]
    public void OwnUpdateCallbackStopsDirtyFactoryAccessAndRecovers(bool border) {
        using var fixture = new PreparationFixture();
        using var effects = new EffectsManager();
        using var context = new RenderContext2D(effects.DeviceContext2D, effects, 16, 16);
        using var drawing = new D2DDeviceContext();
        using var brush = new SolidColorBrush(drawing, new Vector4(1, 0, 0, 1));
        using var root = new PanelNode2D();
        using SceneNode2D first = border
            ? new BorderNode2D { Background = brush, Width = 12, Height = 12 }
            : new RectangleNode2D { Fill = brush, Width = 12, Height = 12 };
        using var later = new ObservedRectangle { Fill = brush, Width = 12, Height = 12 };
        root.AddChildNode(first);
        root.AddChildNode(later);
        SceneNode2D[] roots = [root];
        Func<bool> permission = () => effects.Initialized;
        var calls = 0;
        EventHandler<SceneNode2D.UpdateEventArgs> dispose = (_, args) => {
            Assert.Same(context, args.Context);
            calls++;
            effects.DisposeAllResources();
        };
        first.UpdateRequested += dispose;

        Assert.Equal(0, fixture.Renderer.Prepare(roots, 16, 16, 1, context, permission));

        Assert.Equal(1, calls);
        Assert.False(first.IsRenderable);
        Assert.Equal(0, later.Updates);
        Assert.Empty(fixture.Renderer.Draws);
        Assert.Throws<AssertException>(() => { _ = effects.Factory2D; });
        Assert.Null(first.ScenePreparationPermission);
        first.UpdateRequested -= dispose;
        effects.Reinitialize();
        using var restored = new RenderContext2D(effects.DeviceContext2D, effects, 16, 16);
        Assert.Equal(2, fixture.Renderer.Prepare(roots, 16, 16, 1, restored, permission));
        Assert.True(first.IsRenderable);
        Assert.Equal(1, later.Updates);
    }

    /// <summary>Exercises the reported overlay callback with a genuinely new attached and dirty shape.</summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void OverlayUpdateAddsDirtyChildThenDisposesManagerWithoutUpdatingChildren() {
        using var fixture = new PreparationFixture();
        using var effects = new EffectsManager();
        using var context = new RenderContext2D(effects.DeviceContext2D, effects, 16, 16);
        using var root = new OverlayNode2D();
        using var child = new ObservedRectangle { Width = 12, Height = 12 };
        SceneNode2D[] roots = [root];
        Func<bool> permission = () => effects.Initialized;
        var calls = 0;
        EventHandler<SceneNode2D.UpdateEventArgs> dispose = (_, _) => {
            calls++;
            Assert.True(root.AddChildNode(child));
            Assert.True(child.IsAttached);
            effects.DisposeAllResources();
        };
        root.UpdateRequested += dispose;

        Assert.Equal(0, fixture.Renderer.Prepare(roots, 16, 16, 1, context, permission));

        Assert.Equal(1, calls);
        Assert.False(root.IsRenderable);
        Assert.Equal(0, child.Updates);
        Assert.Empty(fixture.Renderer.Draws);
        root.UpdateRequested -= dispose;
        effects.Reinitialize();
        using var restored = new RenderContext2D(effects.DeviceContext2D, effects, 16, 16);
        fixture.Renderer.Prepare(roots, 16, 16, 1, restored, permission);
        Assert.Equal(1, child.Updates);
        Assert.True(child.IsRenderable);
    }

    /// <summary>Stops nested attachment and virtual layout callbacks before another sibling is entered.</summary>
    /// <param name="stage">The original callback that revokes resource permission.</param>
    [Theory]
    [InlineData("attach")]
    [InlineData("attached")]
    [InlineData("measure")]
    [InlineData("arrange")]
    [Trait("Category", "Warp")]
    public void NestedCallbacksStopSubsequentOperationsAndRestoreBorrowedPermission(string stage) {
        using var fixture = new PreparationFixture();
        using var effects = new EffectsManager();
        using var context = new RenderContext2D(effects.DeviceContext2D, effects, 16, 16);
        using var root = new PanelNode2D();
        using var nested = new PanelNode2D();
        using var first = new ObservedRectangle { Width = 12, Height = 12 };
        using var later = new ObservedRectangle { Width = 12, Height = 12 };
        root.AddChildNode(nested);
        nested.AddChildNode(first);
        nested.AddChildNode(later);
        SceneNode2D[] roots = [root];
        Func<bool> permission = () => effects.Initialized;
        var calls = 0;
        first.Callback = current => {
            if (current != stage) return;
            calls++;
            effects.DisposeAllResources();
        };

        Assert.Equal(0, fixture.Renderer.Prepare(roots, 16, 16, 1, context, permission));

        Assert.Equal(1, calls);
        Assert.Equal(0, later.Updates);
        if (stage is "attach" or "attached") Assert.False(later.IsAttached);
        if (stage == "measure") Assert.Equal(0, later.Measures);
        if (stage == "arrange") Assert.Equal(0, later.Arranges);
        Assert.Empty(fixture.Renderer.Draws);
        Assert.Null(root.ScenePreparationPermission);
        Assert.Null(first.ScenePreparationPermission);
        Assert.Null(later.ScenePreparationPermission);
        first.Callback = null;
        root.Detach();
        effects.Reinitialize();
        using var restored = new RenderContext2D(effects.DeviceContext2D, effects, 16, 16);
        fixture.Renderer.Prepare(roots, 16, 16, 1, restored, permission);
        Assert.Equal(1, later.Updates);
        Assert.True(later.IsRenderable);
    }

    /// <summary>Retains exact zero managed allocation with a cached permission and subscribed original update hook.</summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarmPreparationWithPermissionAndUpdateSubscriberAllocatesExactlyZero() {
        Assert.Equal("false", AppContext.GetData("System.GC.Concurrent")?.ToString(), ignoreCase: true);
        using var fixture = new PreparationFixture();
        using var effects = new EffectsManager();
        using var context = new RenderContext2D(effects.DeviceContext2D, effects, 16, 16);
        using var brush = new SolidColorBrush(effects.DeviceContext2D, new Vector4(1, 0, 0, 1));
        using var root = new PanelNode2D();
        using var child = new RectangleNode2D { Fill = brush, Width = 12, Height = 12 };
        root.AddChildNode(child);
        var updates = 0;
        child.UpdateRequested += (_, _) => updates++;
        SceneNode2D[] roots = [root];
        Func<bool> permission = () => effects.Initialized;
        for (var index = 0; index < 128; index++) {
            fixture.Renderer.BeginFrame();
            fixture.Renderer.Prepare(roots, 16, 16, 1, context, permission);
        }
        var before = GC.GetAllocatedBytesForCurrentThread();
        var control = new byte[40];
        GC.KeepAlive(control);
        Assert.True(GC.GetAllocatedBytesForCurrentThread() - before >= control.Length);
        updates = 0;
        before = GC.GetAllocatedBytesForCurrentThread();
        var draws = 0;
        for (var index = 0; index < 1_000; index++) {
            fixture.Renderer.BeginFrame();
            draws += fixture.Renderer.Prepare(roots, 16, 16, 1, context, permission);
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(1_000, updates);
        Assert.Equal(1_000, draws);
        Assert.Equal(0, allocated);
    }

    /// <summary>Observes the actual original node operations without replacing their implementation.</summary>
    private sealed class ObservedRectangle : RectangleNode2D {
        /// <summary>Runs an optional application callback from an original virtual hook.</summary>
        internal Action<string>? Callback;
        /// <summary>Counts entered updates independently of resource creation.</summary>
        internal int Updates;
        /// <summary>Counts entered measurement hooks.</summary>
        internal int Measures;
        /// <summary>Counts entered arrangement hooks.</summary>
        internal int Arranges;

        /// <summary>Observes the original attached notification.</summary>
        internal ObservedRectangle() => Attached += (_, _) => Callback?.Invoke("attached");

        /// <inheritdoc />
        protected override bool OnAttach() {
            var attached = base.OnAttach();
            Callback?.Invoke("attach");
            return attached;
        }

        /// <inheritdoc />
        protected override Size2F MeasureOverride(Size2F availableSize) {
            Measures++;
            var size = base.MeasureOverride(availableSize);
            Callback?.Invoke("measure");
            return size;
        }

        /// <inheritdoc />
        protected override RectangleF ArrangeOverride(RectangleF finalSize) {
            Arranges++;
            var size = base.ArrangeOverride(finalSize);
            Callback?.Invoke("arrange");
            return size;
        }

        /// <inheritdoc />
        public override void Update(RenderContext2D context) {
            Updates++;
            base.Update(context);
        }
    }

    /// <summary>Uses actual WARP renderer resources; these preparation checks make no GPU presentation claim.</summary>
    private sealed class PreparationFixture : IDisposable {
        /// <summary>Owns the real software native device.</summary>
        private readonly SilkD3D12Device device = SilkD3D12DeviceFactory.CreateDefault(driverType: SilkDriverType.Warp);
        /// <summary>Owns the resource descriptor heap.</summary>
        private readonly SilkD3D12DescriptorHeap resources;
        /// <summary>Owns the sampler descriptor heap.</summary>
        private readonly SilkD3D12DescriptorHeap samplers;
        /// <summary>Owns native resource registrations.</summary>
        private readonly SilkD3D12ResourceManager manager;

        /// <summary>Creates genuine renderer resources without opening a presentation surface.</summary>
        internal PreparationFixture() {
            resources = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 512, true);
            samplers = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 40, true);
            manager = new SilkD3D12ResourceManager(device, resources);
            Renderer = new D3D12Scene2DRenderer(device, manager, resources, samplers);
        }

        /// <summary>Gets the production preparation implementation.</summary>
        internal D3D12Scene2DRenderer Renderer { get; }

        /// <summary>Releases the renderer before its borrowed native resource owners.</summary>
        public void Dispose() {
            Renderer.Dispose();
            manager.Dispose();
            samplers.Dispose();
            resources.Dispose();
            device.Dispose();
        }
    }
}

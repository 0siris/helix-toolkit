using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Cameras;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model.Scene2D;
using HelixToolkit.SharpDX.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Matrix = Silk.NET.Maths.Matrix4X4<float>;

namespace SilkCore.Tests;

/// <summary>Checks demand wakeups, inactive cleanup and retained storage without a WPF dependency.</summary>
public sealed class NativeRenderSourceNotificationTests {
    /// <summary>Checks every paint/layout entry point reaches an attached root even when all nodes are dirty.</summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void AlreadyDirtyScene2DNotifiesRootWithoutAllocating() {
        using var root = new PanelNode2D();
        using var parent = new PanelNode2D();
        using var child = new PanelNode2D();
        root.AddChildNode(parent);
        parent.AddChildNode(child);
        root.Attach();
        var counter = new CountingHost();
        root.RenderInvalidated += counter.InvalidateRender;
        for (var index = 0; index < 64; index++) {
            child.InvalidateRender();
            child.InvalidateMeasure();
            child.InvalidateArrange();
            child.InvalidateTransform();
            child.InvalidateAll();
        }
        counter.Requests = 0;
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 1_000; index++) {
            child.InvalidateRender();
            child.InvalidateMeasure();
            child.InvalidateArrange();
            child.InvalidateTransform();
            child.InvalidateAll();
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(5_000, counter.Requests);
        Assert.Equal(0, allocated);
        root.Detach();
        child.InvalidateAll();
        child.InvalidateRender();
        Assert.Equal(5_000, counter.Requests);
    }

    /// <summary>Checks continuous nodes request only while attached, visible and beneath visible ancestors.</summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void ContinuousSourceSettlesWhenHiddenOrDetached() {
        using var root = new GroupNode();
        using var node = new ContinuousRenderNode();
        using var update = new RenderContext(new PerspectiveCameraCore(), 16, 16);
        var host = new CountingHost();
        root.AddChildNode(node);
        root.Attach();
        node.Attach();
        node.RenderHost = host;
        node.Update(update);
        Assert.Equal(1, host.Requests);
        node.Visible = false;
        host.Requests = 0;
        node.Update(update);
        Assert.Equal(0, host.Requests);
        node.Visible = true;
        root.Visible = false;
        host.Requests = 0;
        node.Update(update);
        Assert.Equal(0, host.Requests);
        root.Visible = true;
        node.Detach();
        host.Requests = 0;
        node.Update(update);
        Assert.Equal(0, host.Requests);
    }

    /// <summary>Checks real renderer capture use requests follow-up frames on reset and timeout, then stops hidden capture.</summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void CaptureResetAndTimeoutRemainLiveUntilHidden() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(driverType: SilkDriverType.Warp);
        using var context = device.CreateCommandContext();
        using var resources = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 512, true);
        using var samplers = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 40, true);
        using var renderer = new SilkD3D12SceneRenderer(device, resources, samplers);
        using var catalog = new D3D12ScenePassCatalog(device, Format.FormatR8G8B8A8Unorm,
            Format.FormatD32FloatS8X24Uint);
        using var update = new RenderContext(new PerspectiveCameraCore(), 16, 16);
        var host = new CountingHost();
        var source = new TimeoutSource { ResetNext = true };
        using var node = new CaptureNode(source);
        SceneNode[] roots = [node];
        var transforms = new HelixToolkit.SharpDX.Core.Model.GlobalTransformStruct();
        var frustum = new BoundingFrustum(Matrix.Identity);
        for (var index = 0; index < 3; index++) {
            renderer.BeginFrame();
            var visible = renderer.PrepareScene(roots, update, host: host);
            host.Requests = 0;
            context.Reset();
            Assert.Equal(0, renderer.RenderVisible(context, visible, catalog.Resolve, in transforms, false, ref frustum));
            context.Close();
            Assert.Equal(1, host.Requests);
        }
        Assert.True(((ScreenCloneRenderCore)node.RenderCore).IsCaptureStarted);
        node.Visible = false;
        host.Requests = 0;
        renderer.BeginFrame();
        Assert.Empty(renderer.PrepareScene(roots, update, host: host));
        Assert.False(((ScreenCloneRenderCore)node.RenderCore).IsCaptureStarted);
        Assert.Equal(0, host.Requests);
        Assert.Equal(2, source.Starts);
        Assert.Equal(2, source.Stops);
    }

    /// <summary>Checks pass hits and actual repeatedly prepared 2D geometry retain their managed capacity.</summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarmPassLookupsAndScene2DGeometryRetainStorage() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(driverType: SilkDriverType.Warp);
        using var resources = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 512, true);
        using var samplers = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 40, true);
        using var manager = new SilkD3D12ResourceManager(device, resources);
        using var renderer = new D3D12Scene2DRenderer(device, manager, resources, samplers);
        using var catalog = new D3D12ScenePassCatalog(device, Format.FormatB8G8R8A8Unorm,
            Format.FormatD32FloatS8X24Uint, outputMode: D3D12ColorOutputMode.Premultiplied);
        using var d2d = new D2DDeviceContext();
        using var brush = new SolidColorBrush(d2d, new Silk.NET.Maths.Vector4D<float>(1, 0, 0, 0.5f));
        using var effects = new EffectsManager();
        using var layout = new RenderContext2D(d2d, effects, 16, 16);
        using var rectangle = new RectangleNode2D {
            Fill = brush, Stroke = brush, StrokeThickness = 1, Width = 14, Height = 14
        };
        SceneNode2D[] roots = [rectangle];
        for (var index = 0; index < 64; index++) {
            renderer.BeginFrame();
            renderer.Prepare(roots, 16, 16, 1, layout);
            catalog.ResolveSprite2D();
        }
        var vertexStorage = renderer.Draws[0].Vertices;
        var indexStorage = renderer.Draws[0].Indices;
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 1_000; index++) {
            renderer.BeginFrame();
            renderer.Prepare(roots, 16, 16, 1, layout);
            catalog.ResolveSprite2D();
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, allocated);
        Assert.NotEmpty(renderer.Draws);
        Assert.Same(vertexStorage, renderer.Draws[0].Vertices);
        Assert.Same(indexStorage, renderer.Draws[0].Indices);
        Assert.True(renderer.Draws[0].VertexCount > 0);
    }

    /// <summary>Injects deterministic capture reset/timeout responses through the productive node boundary.</summary>
    private sealed class CaptureNode : ScreenDuplicationNode {
        /// <summary>The retained source returned to the lazily created capture core.</summary>
        private readonly TimeoutSource source;

        /// <summary>Initializes a node backed by one observable source.</summary>
        /// <param name="source">The timeout/reset source.</param>
        internal CaptureNode(TimeoutSource source) => this.source = source;

        /// <inheritdoc />
        protected override RenderCore OnCreateRenderCore() => new ScreenCloneRenderCore(GetSource);

        /// <summary>Returns the retained fake source after each capture restart.</summary>
        /// <returns>The deterministic capture source.</returns>
        private IDesktopCaptureSource GetSource() => source;
    }

    /// <summary>Simulates one capture reset followed by nonblocking capture timeouts.</summary>
    private sealed class TimeoutSource : IDesktopCaptureSource {
        /// <summary>Gets or sets whether the next acquisition resets the capture session.</summary>
        internal bool ResetNext { get; set; }

        /// <summary>Gets the number of starts, including restart after reset.</summary>
        internal int Starts { get; private set; }

        /// <summary>Gets the number of stopped sessions.</summary>
        internal int Stops { get; private set; }

        /// <inheritdoc />
        public void Start(int output) => Starts++;

        /// <inheritdoc />
        public bool TryAcquire(TimeSpan timeout, out ScreenCaptureFrame frame) {
            frame = default;
            if (ResetNext) {
                ResetNext = false;
                throw new ScreenCaptureResetException();
            }
            return false;
        }

        /// <inheritdoc />
        public void Dispose() => Stops++;
    }

    /// <summary>Counts the original host invalidation API without allocating request objects.</summary>
    private sealed class CountingHost : IRenderHost {
        /// <summary>Gets or sets the consumed request count.</summary>
        internal int Requests { get; set; }

        /// <inheritdoc />
        public IViewport3DX Viewport { get; set; } = null!;

        /// <inheritdoc />
        public FastList<KeyValuePair<int, SceneNode>> PerFrameFlattenedScene { get; } = [];

        /// <inheritdoc />
        public RenderContext? RenderContext => null;

        /// <inheritdoc />
        public float DpiScale { get; set; } = 1;

        /// <inheritdoc />
        public IEffectsManager? EffectsManager { get; set; }

        /// <inheritdoc />
        public event EventHandler? Rendered { add { } remove { } }

        /// <inheritdoc />
        public bool UpdateAndRender() => false;

        /// <inheritdoc />
        public void Resize(int width, int height) { }

        /// <inheritdoc />
        public void StartRendering() { }

        /// <inheritdoc />
        public void StopRendering() { }

        /// <inheritdoc />
        public void InvalidateRender() => Requests++;

        /// <inheritdoc />
        public void InvalidateSceneGraph() => Requests++;

        /// <inheritdoc />
        public void InvalidatePerFrameRenderables() => Requests++;
    }
}

using System.Reflection;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Cameras;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model.Scene2D;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.Wpf.SharpDX.Controls;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Xunit;
using CullMode = HelixToolkit.SharpDX.Core.Native.CullMode;
using Matrix3X2 = HelixToolkit.SharpDX.Core.Native.Matrix3X2;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace SilkToolkit.Tests;

/// <summary>Exercises the productive shared recorder against caller-owned offscreen WARP color textures.</summary>
[Collection(WpfCollection.Name)]
public sealed class D3D12FrameRendererTests {
    /// <summary>Draws the preparer's own retained candidates without an intermediate classification copy.</summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpRendersDirectPreparationCandidates() {
        using var frame = new OffscreenFrame();
        using var mesh = CreateGreenQuad();
        var camera = CreateCamera();
        using var update = new RenderContext(camera, 32, 24);
        var candidates = frame.Renderer.SceneRenderer.PrepareScene([mesh], update);
        var target = frame.Target;
        var transforms = D3D12FrameRenderer.CreateViewportTransforms(camera, target.Width, target.Height, 1, 0,
            out var frustum);
        frame.Context.Reset();
        var draws = frame.Renderer.RecordSceneFrame(frame.Context, in target, [0, 0, 1, 1],
            candidates, in transforms, false, ref frustum);
        var pixels = frame.Readback(in target);
        Assert.Equal(1, draws);
        Assert.Same(mesh, Assert.Single(candidates));
        AssertPixel(pixels, target.Width, target.Width / 2, target.Height / 2, 0, 255, 0);
    }

    /// <summary>Records the default viewport pass sequence and reads every pixel without a presentation host.</summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpRecordsDefaultViewportClearIntoOffscreenRgbaTarget() {
        using var frame = new OffscreenFrame();
        var camera = CreateCamera();
        var target = frame.Target;
        frame.Context.Reset();
        var draws = frame.Renderer.RecordViewportFrame(frame.Context, in target, 1,
            [0, 0, 1, 1], Array.Empty<SceneNode>(), camera, false);
        var pixels = frame.Readback(in target);

        Assert.Equal(0, draws);
        Assert.Equal(Format.FormatR8G8B8A8Unorm, target.ColorResource.Description.Format);
        for (uint y = 0; y < target.Height; y++)
            for (uint x = 0; x < target.Width; x++)
                AssertPixel(pixels, target.Width, x, y, 0, 0, 255);
        Assert.Equal(ResourceStates.CopySource, target.ColorResource.State);
        var depth = Assert.IsType<SilkD3D12Resource>(frame.Renderer.DepthStencilBuffer);
        Assert.Equal((ulong)target.Width, depth.Description.Width);
        Assert.Equal(target.Height, depth.Description.Height);

        frame.Renderer.Dispose();
        Assert.True(depth.IsDisposed);
        Assert.False(target.ColorResource.IsDisposed);
        Assert.False(target.RenderTargetView.IsDisposed);
        Assert.False(frame.Context.IsDisposed);
        Assert.False(frame.Device.IsDisposed);
    }

    /// <summary>Proves native mesh pixels, repeated depth clears, and actual context, list, and selector reuse.</summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpRendersMeshAndReusesFrameOwnersAcrossCompletedFrames() {
        using var frame = new OffscreenFrame();
        using var mesh = CreateGreenQuad();
        var camera = CreateCamera();
        var roots = new FastList<SceneNode> { mesh };
        var target = frame.Target;
        var first = frame.Render(camera, roots);
        Assert.Equal(1, first.Draws);
        AssertPixel(first.Pixels, target.Width, target.Width / 2, target.Height / 2, 0, 255, 0);
        AssertPixel(first.Pixels, target.Width, 0, 0, 0, 0, 255);
        Assert.True(mesh.RenderCore.IsAttached);
        var context = frame.Renderer.RenderContext;
        var flattened = frame.Renderer.PerFrameFlattenedScene;
        var depth = frame.Renderer.DepthStencilBuffer;
        var catalog = frame.Renderer.PassCatalog;
        var opaque = Field<FastList<SceneNode>>(frame.Renderer, "opaqueNodes");
        var transparent = Field<FastList<SceneNode>>(frame.Renderer, "transparentNodes");
        var lights = Field<LightsBufferModel>(frame.Renderer, "lights");
        var selector = Field<Func<SceneNode, ShaderPass?>>(frame.Renderer, "resolvePass");
        var boneSelector = Field<Func<SceneNode, ShaderPass?>>(frame.Renderer, "resolveBoneSkinningPass");
        Assert.Same(catalog, selector.Target);
        Assert.Same(catalog, boneSelector.Target);
        Assert.Same(mesh, Assert.Single(opaque));

        // A farther second draw fails if the common recorder retains the preceding frame's depth values.
        mesh.ModelMatrix = SilkMath.Translation(new Vector3(0, 0, -0.5f));
        var second = frame.Render(camera, roots);
        Assert.Equal(1, second.Draws);
        AssertPixel(second.Pixels, target.Width, target.Width / 2, target.Height / 2, 0, 255, 0);
        Assert.Same(context, frame.Renderer.RenderContext);
        Assert.Same(flattened, frame.Renderer.PerFrameFlattenedScene);
        Assert.Same(depth, frame.Renderer.DepthStencilBuffer);
        Assert.Same(opaque, Field<FastList<SceneNode>>(frame.Renderer, "opaqueNodes"));
        Assert.Same(transparent, Field<FastList<SceneNode>>(frame.Renderer, "transparentNodes"));
        Assert.Same(lights, Field<LightsBufferModel>(frame.Renderer, "lights"));
        Assert.Same(selector, Field<Func<SceneNode, ShaderPass?>>(frame.Renderer, "resolvePass"));
        Assert.Same(boneSelector, Field<Func<SceneNode, ShaderPass?>>(frame.Renderer, "resolveBoneSkinningPass"));

        mesh.Visible = false;
        var hidden = frame.Render(camera, roots);
        Assert.Equal(0, hidden.Draws);
        Assert.Empty(opaque);
        AssertPixel(hidden.Pixels, target.Width, target.Width / 2, target.Height / 2, 0, 0, 255);
        Assert.Same(context, frame.Renderer.RenderContext);
    }

    /// <summary>Retains both update contexts until immutable camera, DPI, size, or manager inputs change.</summary>
    /// <returns>The STA context and offscreen rendering verification.</returns>
    [Fact]
    [Trait("Category", "Warp")]
    public Task WarpReusesContextsAndReplacesThemAtCompletedResourceBoundaries() => StaThread.RunAsync(() => {
        using var effects = new DefaultEffectsManager(new EffectsManagerConfiguration { EnableSoftwareRendering = true });
        using var replacementEffects = new DefaultEffectsManager(
            new EffectsManagerConfiguration { EnableSoftwareRendering = true });
        using var frame = new OffscreenFrame(effects);
        var camera = CreateCamera();
        var roots = Array.Empty<SceneNode>();
        var overlays = Array.Empty<SceneNode2D>();
        _ = frame.Render(camera, roots, roots2D: overlays);
        var context = Assert.IsType<RenderContext>(frame.Renderer.RenderContext);
        var context2D = Assert.IsType<RenderContext2D>(frame.Renderer.RenderContext2D);
        _ = frame.Render(camera, roots, roots2D: overlays);
        Assert.Same(context, frame.Renderer.RenderContext);
        Assert.Same(context2D, frame.Renderer.RenderContext2D);
        Assert.Equal(Matrix3X2.Identity, context2D.RelativeTransform);
        Assert.False(context2D.HasTarget);

        _ = frame.Render(camera, roots, dpiScale: 2, roots2D: overlays);
        Assert.NotSame(context, frame.Renderer.RenderContext);
        Assert.Null(context.Camera);
        Assert.True(context2D.IsDisposed);
        Assert.Equal(2, frame.Renderer.RenderContext!.DpiScale);
        Assert.Equal(2, frame.Renderer.RenderContext2D!.DpiScale);

        var depth = Assert.IsType<SilkD3D12Resource>(frame.Renderer.DepthStencilBuffer);
        var resizedContext = frame.Renderer.RenderContext;
        using var resizedColor = frame.Device.CreateRenderTargetTexture2D(48, 16, Format.FormatR8G8B8A8Unorm);
        using var resizedView = frame.TargetHeap.Allocate();
        frame.Device.CreateRenderTargetView(resizedColor, resizedView);
        var resized = new D3D12FrameTarget(resizedColor, resizedView, 48, 16, Format.FormatR8G8B8A8Unorm);
        frame.Context.Reset();
        _ = frame.Renderer.RecordViewportFrame(frame.Context, in resized, 2,
            [1, 0, 0, 1], roots, camera, false, roots2D: overlays);
        var pixels = frame.Readback(in resized);
        AssertPixel(pixels, resized.Width, 0, 0, 255, 0, 0);
        Assert.True(depth.IsDisposed);
        Assert.NotSame(resizedContext, frame.Renderer.RenderContext);
        Assert.Equal(48, frame.Renderer.RenderContext!.ActualWidth);
        Assert.Equal(16, frame.Renderer.RenderContext2D!.ActualHeight);

        var oldCatalog = frame.Renderer.PassCatalog;
        var oldContext2D = frame.Renderer.RenderContext2D;
        frame.Renderer.SetEffectsManager(replacementEffects);
        Assert.True(oldCatalog.IsDisposed);
        Assert.True(oldContext2D!.IsDisposed);
        Assert.Null(frame.Renderer.RenderContext2D);
        var selector = Field<Func<SceneNode, ShaderPass?>>(frame.Renderer, "resolvePass");
        Assert.Same(frame.Renderer.PassCatalog, selector.Target);
        _ = frame.Render(camera, roots, roots2D: overlays);
        Assert.Same(replacementEffects, frame.Renderer.RenderContext2D!.DeviceResources);
        frame.Renderer.Dispose();
        Assert.False(effects.IsDisposed);
        Assert.False(replacementEffects.IsDisposed);
    });

    /// <summary>Discards mutable context stacks after a failed original Scene2D update and renders again.</summary>
    /// <returns>The STA exception and recovery verification.</returns>
    [Fact]
    [Trait("Category", "Warp")]
    public Task WarpDoesNotReuseFailedScene2DContextState() => StaThread.RunAsync(() => {
        using var effects = new DefaultEffectsManager(new EffectsManagerConfiguration { EnableSoftwareRendering = true });
        using var frame = new OffscreenFrame(effects);
        using var overlay = new ThrowingOverlay();
        var target = frame.Target;
        var camera = CreateCamera();
        frame.Context.Reset();
        Assert.Throws<InvalidOperationException>(() => frame.Renderer.RecordViewportFrame(frame.Context,
            in target, 1, [0, 0, 1, 1], Array.Empty<SceneNode>(), camera, false, roots2D: [overlay]));
        var failed = Assert.IsType<RenderContext2D>(overlay.LastContext);
        Assert.True(failed.IsDisposed);
        Assert.Null(frame.Renderer.RenderContext2D);
        var recovered = frame.Render(camera, Array.Empty<SceneNode>(), roots2D: Array.Empty<SceneNode2D>());
        var current = Assert.IsType<RenderContext2D>(frame.Renderer.RenderContext2D);
        Assert.NotSame(failed, current);
        Assert.Equal(Matrix3X2.Identity, current.RelativeTransform);
        Assert.False(current.HasTarget);
        AssertPixel(recovered.Pixels, target.Width, 0, 0, 0, 0, 255);
    });

    /// <summary>Rejects format, size, and DPI mismatches before changing the actual retained frame owners.</summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpRejectsIncompatibleBorrowedTargetMetadataBeforeMutation() {
        using var frame = new OffscreenFrame();
        var camera = CreateCamera();
        var roots = Array.Empty<SceneNode>();
        _ = frame.Render(camera, roots);
        var context = frame.Renderer.RenderContext;
        var depth = frame.Renderer.DepthStencilBuffer;
        var target = frame.Target;
        var badFormat = target with { Format = Format.FormatB8G8R8A8Unorm };
        var badSize = target with { Width = target.Width + 1 };
        var zeroSize = target with { Height = 0 };
        frame.Context.Reset();
        Assert.Throws<ArgumentException>(() => frame.Renderer.RecordViewportFrame(frame.Context,
            in badFormat, 1, [0, 0, 0, 1], roots, camera, false));
        Assert.Throws<ArgumentException>(() => frame.Renderer.RecordViewportFrame(frame.Context,
            in badSize, 1, [0, 0, 0, 1], roots, camera, false));
        Assert.Throws<ArgumentOutOfRangeException>(() => frame.Renderer.RecordViewportFrame(frame.Context,
            in zeroSize, 1, [0, 0, 0, 1], roots, camera, false));
        Assert.Throws<ArgumentOutOfRangeException>(() => frame.Renderer.RecordViewportFrame(frame.Context,
            in target, float.NaN, [0, 0, 0, 1], roots, camera, false));
        using var differentColor = frame.Device.CreateRenderTargetTexture2D(32, 24, Format.FormatB8G8R8A8Unorm);
        using var differentView = frame.TargetHeap.Allocate();
        frame.Device.CreateRenderTargetView(differentColor, differentView);
        var mismatchedResource = new D3D12FrameTarget(differentColor, differentView, 32, 24,
            Format.FormatR8G8B8A8Unorm);
        Assert.Throws<ArgumentException>(() => frame.Renderer.RecordViewportFrame(frame.Context,
            in mismatchedResource, 1, [0, 0, 0, 1], roots, camera, false));
        Assert.Same(context, frame.Renderer.RenderContext);
        Assert.Same(depth, frame.Renderer.DepthStencilBuffer);
        Assert.False(depth!.IsDisposed);
        Assert.Equal(ResourceStates.CopySource, target.ColorResource.State);
    }

    /// <summary>Creates a camera whose center and background samples have independent pixel predictions.</summary>
    /// <returns>The fixed orthographic camera.</returns>
    private static OrthographicCameraCore CreateCamera() => new() {
        Position = new Vector3(0, 0, 2), LookDirection = new Vector3(0, 0, -2),
        UpDirection = Vector3.UnitY, Width = 2, NearPlaneDistance = 0.1f, FarPlaneDistance = 10
    };

    /// <summary>Creates a centered green mesh with culling disabled for a platform-independent pixel oracle.</summary>
    /// <returns>The original productive mesh node.</returns>
    private static MeshNode CreateGreenQuad() => new() {
        Material = new ColorMaterialCore(), CullMode = CullMode.None,
        Geometry = new MeshGeometry3D {
            Positions = new Vector3Collection([
                new(-0.5f, 0.5f, 0), new(0.5f, 0.5f, 0), new(-0.5f, -0.5f, 0), new(0.5f, -0.5f, 0)
            ]),
            Colors = new Color4Collection([
                new Vector4(0, 1, 0, 1), new Vector4(0, 1, 0, 1),
                new Vector4(0, 1, 0, 1), new Vector4(0, 1, 0, 1)
            ]),
            Indices = new IntCollection([0, 1, 2, 2, 1, 3])
        }
    };

    /// <summary>Checks a physical RGBA pixel against its independent expected color.</summary>
    /// <param name="pixels">The tightly packed completed readback.</param>
    /// <param name="width">The physical image width.</param>
    /// <param name="x">The physical column.</param>
    /// <param name="y">The physical row.</param>
    /// <param name="red">The expected red channel.</param>
    /// <param name="green">The expected green channel.</param>
    /// <param name="blue">The expected blue channel.</param>
    private static void AssertPixel(byte[] pixels, uint width, uint x, uint y, byte red, byte green, byte blue) {
        var offset = checked((int)((y * width + x) * 4));
        Assert.Equal(red, pixels[offset]);
        Assert.Equal(green, pixels[offset + 1]);
        Assert.Equal(blue, pixels[offset + 2]);
        Assert.Equal(255, pixels[offset + 3]);
    }

    /// <summary>Reads an actual retained owner to verify that completed frames reuse its identity.</summary>
    /// <typeparam name="T">The expected owner type.</typeparam>
    /// <param name="renderer">The productive shared recorder.</param>
    /// <param name="name">The actual owner field.</param>
    /// <returns>The retained field value.</returns>
    private static T Field<T>(D3D12FrameRenderer renderer, string name) where T : class =>
        Assert.IsType<T>(typeof(D3D12FrameRenderer).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(renderer));

    /// <summary>Models an application update hook that throws after mutating its frame context.</summary>
    private sealed class ThrowingOverlay : RectangleNode2D {
        /// <summary>Gets the actual context received by the failing update.</summary>
        internal RenderContext2D? LastContext { get; private set; }

        /// <summary>Leaves a relative transform on the context stack before throwing an application failure.</summary>
        /// <param name="context">The retained common Scene2D update context.</param>
        public override void Update(RenderContext2D context) {
            LastContext = context;
            context.PushRelativeTransform(new Matrix3X2(1, 0, 0, 1, 5, 7));
            throw new InvalidOperationException("Injected Scene2D update failure.");
        }
    }

    /// <summary>Owns offscreen test submission and synchronous readback independently of the shared recorder.</summary>
    private sealed class OffscreenFrame : IDisposable {
        /// <summary>The caller-owned queue used only by explicit test submissions.</summary>
        private readonly SilkD3D12CommandQueue queue;

        /// <summary>The caller-owned completion fence.</summary>
        private readonly SilkD3D12Fence fence;

        /// <summary>The borrowed-target RTV owned by this test harness.</summary>
        private readonly SilkD3D12Descriptor view;

        /// <summary>The offscreen RGBA allocation owned by this test harness.</summary>
        private readonly SilkD3D12Resource color;

        /// <summary>Gets the caller-owned WARP device.</summary>
        internal SilkD3D12Device Device { get; }

        /// <summary>Gets the caller-owned recording context.</summary>
        internal SilkD3D12CommandContext Context { get; }

        /// <summary>Gets the caller-owned RTV heap with room for a replacement target.</summary>
        internal SilkD3D12DescriptorHeap TargetHeap { get; }

        /// <summary>Gets the productive common recorder.</summary>
        internal D3D12FrameRenderer Renderer { get; }

        /// <summary>Gets borrowed metadata for the offscreen texture.</summary>
        internal D3D12FrameTarget Target => new(color, view, 32, 24, Format.FormatR8G8B8A8Unorm);

        /// <summary>Creates a window-independent WARP target and its explicit submission owners.</summary>
        /// <param name="effectsManager">The optional borrowed scene manager.</param>
        internal OffscreenFrame(HelixToolkit.SharpDX.Core.Interface.IEffectsManager? effectsManager = null) {
            Device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
            queue = Device.CreateCommandQueue();
            Context = Device.CreateCommandContext();
            fence = Device.CreateFence();
            TargetHeap = Device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 2);
            view = TargetHeap.Allocate();
            color = Device.CreateRenderTargetTexture2D(32, 24, Format.FormatR8G8B8A8Unorm);
            Device.CreateRenderTargetView(color, view);
            Renderer = new D3D12FrameRenderer(Device, Format.FormatR8G8B8A8Unorm, effectsManager);
        }

        /// <summary>Records the productive viewport path and completes an explicit test readback.</summary>
        /// <param name="camera">The active camera.</param>
        /// <param name="roots">The stable original scene roots.</param>
        /// <param name="dpiScale">The explicit physical-pixel scale.</param>
        /// <param name="roots2D">The optional stable overlay roots.</param>
        /// <returns>The actual native draw count and tightly packed completed RGBA pixels.</returns>
        internal (int Draws, byte[] Pixels) Render(CameraCore camera, IEnumerable<SceneNode> roots,
            float dpiScale = 1, IEnumerable<SceneNode2D>? roots2D = null) {
            var target = Target;
            Context.Reset();
            var draws = Renderer.RecordViewportFrame(Context, in target, dpiScale,
                [0, 0, 1, 1], roots, camera, false, roots2D: roots2D, autoUpdateOctree: false);
            return (draws, Readback(in target));
        }

        /// <summary>Copies an already recorded target and waits for its finite test completion deadline.</summary>
        /// <param name="target">The actual offscreen target used for the current recording.</param>
        /// <returns>Every physical RGBA pixel with native row padding removed.</returns>
        internal byte[] Readback(in D3D12FrameTarget target) {
            var footprint = Device.GetCopyableFootprint(target.ColorResource, out var bytes);
            using var readback = Device.CreateBuffer(bytes, HeapType.Readback);
            Context.Transition(target.ColorResource, ResourceStates.CopySource);
            Context.CopyTextureToBuffer(readback,target.ColorResource, in footprint);
            Context.Close();
            queue.Execute(Context);
            fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));
            Device.ThrowIfDeviceRemoved();
            var rowBytes = checked((int)target.Width * 4);
            var pixels = new byte[checked(rowBytes * (int)target.Height)];
            for (var row = 0; row < target.Height; row++) {
                var source = footprint.Offset + (ulong)row * footprint.Footprint.RowPitch;
                readback.Read(rowBytes, source).CopyTo(pixels, checked((int)row * rowBytes));
            }
            return pixels;
        }

        /// <summary>Completes test submissions before releasing their native owners in dependency order.</summary>
        public void Dispose() {
            fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));
            Renderer.Dispose();
            color.Dispose();
            view.Dispose();
            TargetHeap.Dispose();
            Context.Dispose();
            fence.Dispose();
            queue.Dispose();
            Device.Dispose();
        }
    }
}

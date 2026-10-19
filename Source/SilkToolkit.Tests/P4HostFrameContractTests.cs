using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Controls;
using Xunit;
using Color = System.Windows.Media.Color;
using DiffuseMaterial = HelixToolkit.Wpf.SharpDX.DiffuseMaterial;
using MeshGeometry3D = HelixToolkit.SharpDX.Core.MeshGeometry3D;
using CullMode = HelixToolkit.SharpDX.Core.Native.CullMode;
using OrthographicCamera = HelixToolkit.Wpf.SharpDX.OrthographicCamera;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace SilkToolkit.Tests;

/// <summary>Checks Phase 4 host contracts through both explicitly selected, productive viewport templates.</summary>
[Collection(WpfCollection.Name)]
public sealed class P4HostFrameContractTests {
    /// <summary>Publishes the real host, camera, dimensions and original roots, including accepted zero-draw frames.</summary>
    /// <param name="hwnd">Whether the viewport explicitly selects the HWND backend.</param>
    /// <returns>The dispatcher-owned hardware verification.</returns>
    [Theory(Explicit = true)]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task ActualHostContextAndRootsFollowAcceptedFrames(bool hwnd) => StaThread.RunAsync(async () => {
        using var effects = new DefaultEffectsManager();
        await using var driver = new P4PresentationDriver(hwnd, effects);
        var model = P4PresentationDriver.CreateMesh();
        using var group = new GroupModel3D();
        group.Children.Add(model);
        driver.Viewport.Items.Add(group);
        await driver.WarmAsync();

        var host = driver.Viewport.RenderHost;
        var context = Assert.IsType<RenderContext>(host.RenderContext);
        Assert.Same(driver.Viewport, host);
        Assert.Same(driver.Viewport, host.Viewport);
        Assert.Same(driver.Context, context);
        Assert.Same(driver.Camera.CameraInternal, context.Camera);
        Assert.Equal(64f, context.ActualWidth);
        Assert.Equal(48f, context.ActualHeight);
        Assert.Equal(64f, context.Viewport.Width);
        Assert.Equal(48f, context.Viewport.Height);
        Assert.Same(host, model.SceneNode.RenderHost);
        Assert.Same(context, ((P4ObservedMeshNode)model.SceneNode).LastContext);
        Assert.Same(driver.FlattenedScene, host.PerFrameFlattenedScene);
        Assert.Contains(host.PerFrameFlattenedScene, entry => ReferenceEquals(entry.Value, group.SceneNode));
        Assert.Contains(host.PerFrameFlattenedScene, entry => ReferenceEquals(entry.Value, model.SceneNode));

        driver.Camera.Position = new Point3D(0.25, 0, 5);
        driver.Camera.LookDirection = new Vector3D(-0.25, 0, -5);
        await driver.DrainAsync();
        Assert.Equal(new Vector3(0.25f, 0, 5), driver.Context!.CameraParams.Position);

        driver.Viewport.Items.Clear();
        driver.Viewport.InvalidateSceneGraph();
        await driver.DrainAsync();
        Assert.DoesNotContain(host.PerFrameFlattenedScene, entry => ReferenceEquals(entry.Value, model.SceneNode));
        var before = driver.Counts;
        driver.Viewport.InvalidateRender();
        Assert.True(host.UpdateAndRender(), "An accepted empty scene is a real host frame.");
        await driver.FinishTransportAsync();
        driver.AssertAdvanced(before, 1);
        Assert.Same(context, host.RenderContext);
        Assert.False(group.SceneNode.IsDisposed);
        driver.AssertIdleOrHidden();
    });

    /// <summary>Skips actual busy GPU frames without replacing snapshots or duplicating completion event pairs.</summary>
    /// <param name="hwnd">Whether the viewport explicitly selects the HWND backend.</param>
    /// <returns>The real queue-fence and completion verification.</returns>
    [Theory(Explicit = true)]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task PendingGpuFrameKeepsSnapshotAndCompletesExactlyOnce(bool hwnd) => StaThread.RunAsync(async () => {
        using var effects = new DefaultEffectsManager();
        await using var driver = new P4PresentationDriver(hwnd, effects);
        driver.Viewport.Items.Add(P4PresentationDriver.CreateMesh());
        await driver.WarmAsync();
        var before = driver.Counts;
        using (var gate = new P4NativeQueueGate(driver)) {
            driver.Viewport.InvalidateRender();
            _ = driver.Tick();
            Assert.Equal(before.Submitted + 1, driver.Submitted);
            Assert.Equal(before.Completed, driver.Completed);
            Assert.Equal(before.Rendered, driver.RenderedCount);
            var context = driver.Context;
            var snapshot = driver.Viewport.PerFrameFlattenedScene.ToArray();
            using var added = new GroupModel3D();
            driver.Viewport.Items.Add(added);
            driver.Camera.Position = new Point3D(0.25, 0, 5);
            for (var tick = 0; tick < 4; tick++) Assert.Equal(0, driver.Tick());
            Assert.Same(context, driver.Context);
            Assert.Equal(snapshot, driver.Viewport.PerFrameFlattenedScene.ToArray());
            Assert.Equal(before.Submitted + 1, driver.Submitted);
            Assert.Equal(before.OnRendered, driver.OnRenderedCount);
            gate.Release();
            await driver.DrainAsync();
            Assert.Contains(driver.Viewport.PerFrameFlattenedScene, entry => ReferenceEquals(entry.Value, added.SceneNode));
            Assert.Equal(new Vector3(0.25f, 0, 5), driver.Context!.CameraParams.Position);
            driver.Viewport.Items.Remove(added);
        }

        await driver.DrainAsync();
        Assert.Equal(driver.Submitted, driver.Completed);
        Assert.Equal((ulong)driver.RenderedCount, driver.Completed);
        Assert.Equal(driver.RenderedCount, driver.OnRenderedCount);
        driver.AssertIdleOrHidden();
        var retired = driver.Counts;
        driver.Viewport.RetireSceneResourcesForManagerChange();
        driver.Viewport.RetireSceneResourcesForManagerChange();
        Assert.Equal(retired, driver.Counts);
    });

    /// <summary>Requires new same-owner captures with straight BGRA channels and coverage rather than another host's pixels.</summary>
    /// <param name="hwnd">Whether the capturing viewport explicitly selects the HWND backend.</param>
    /// <returns>The independent-owner, readback and pixel verification.</returns>
    [Theory(Explicit = true)]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task CaptureReturnsNewSameOwnerFramesWithExplicitPixelRepresentation(bool hwnd) => StaThread.RunAsync(async () => {
        using var effects = new DefaultEffectsManager();
        await using var driver = new P4PresentationDriver(hwnd, effects);
        await using var other = new P4PresentationDriver(!hwnd, effects);
        await driver.WarmAsync();
        await other.WarmAsync();
        other.Viewport.BackgroundColor = Colors.Lime;
        var otherCapture = await other.CaptureAsync();
        P4PresentationDriver.AssertPixel(otherCapture, 2, 2, 0, 255, 0, 255);
        byte[]? precedingPixels = null;
        foreach (var alpha in new byte[] { 0, 128, 255 }) {
            driver.Viewport.BackgroundColor = Color.FromArgb(alpha, 224, 64, 32);
            await driver.DrainAsync();
            var before = driver.Counts;
            var captured = await driver.CaptureAsync();
            driver.AssertAdvanced(before, 1);
            Assert.NotSame(precedingPixels, captured.Pixels);
            Assert.NotSame(otherCapture.Pixels, captured.Pixels);
            Assert.Equal(64u, captured.Width);
            Assert.Equal(48u, captured.Height);
            Assert.Equal(64 * 48 * 4, captured.Pixels.Length);
            var transparentWpf = !hwnd && alpha == 0;
            P4PresentationDriver.AssertPixel(captured, 2, 2,
                transparentWpf ? 0 : 32, transparentWpf ? 0 : 64, transparentWpf ? 0 : 224, alpha, 3);
            precedingPixels = captured.Pixels;
        }
        Assert.NotSame(driver.Context, other.Context);
        P4PresentationDriver.AssertPixel(await other.CaptureAsync(), 2, 2, 0, 255, 0, 255);
    });

    /// <summary>Rejects stale readback after hidden entry, canceled recording and a nested capture from a real mesh callback.</summary>
    /// <param name="hwnd">Whether the viewport explicitly selects the HWND backend.</param>
    /// <returns>The fresh-submission regression verification.</returns>
    [Theory(Explicit = true)]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task CaptureRejectsSkippedCancelledAndReentrantRequests(bool hwnd) => StaThread.RunAsync(async () => {
        using var effects = new DefaultEffectsManager();
        await using var driver = new P4PresentationDriver(hwnd, effects);
        var mesh = P4PresentationDriver.CreateMesh();
        driver.Viewport.Items.Add(mesh);
        var node = (P4ObservedMeshNode)mesh.SceneNode;
        await driver.WarmAsync();
        var oldCapture = await driver.CaptureAsync();
        var before = driver.Counts;
        driver.Viewport.Visibility = Visibility.Hidden;
        Assert.Throws<InvalidOperationException>(() => driver.Viewport.CaptureD3D12Frame());
        Assert.Equal(before, driver.Counts);
        driver.Viewport.Visibility = Visibility.Visible;
        await driver.DrainAsync();

        oldCapture = await driver.CaptureAsync();
        before = driver.Counts;
        var cancellationCount = 0;
        node.OnNextUpdate = _ => {
            cancellationCount++;
            Assert.True(driver.Viewport.IsPreparingPresentationFrame);
            effects.DisposeAllResources();
        };
        Assert.Throws<InvalidOperationException>(() => driver.Viewport.CaptureD3D12Frame());
        Assert.Equal(1, cancellationCount);
        Assert.Equal(before, driver.Counts);
        effects.Reinitialize();
        await driver.DrainAsync();
        P4PresentationDriver.SetMeshColor(mesh, new Vector4(1, 0, 0, 1));
        var fresh = await driver.CaptureAsync();
        Assert.NotSame(oldCapture.Pixels, fresh.Pixels);
        P4PresentationDriver.AssertPixel(fresh, 32, 24, 0, 0, 255, 255);

        before = driver.Counts;
        var nestedCount = 0;
        node.OnNextUpdate = _ => {
            nestedCount++;
            Assert.Throws<InvalidOperationException>(() => driver.Viewport.CaptureD3D12Frame());
        };
        driver.Viewport.InvalidateRender();
        _ = driver.Tick(wait: true);
        await driver.FinishTransportAsync();
        Assert.Equal(1, nestedCount);
        driver.AssertAdvanced(before, 1);
    });
}

/// <summary>Drives genuine viewport templates on a hidden WPF source without substituting lifetime or submission state.</summary>
internal sealed class P4PresentationDriver : IAsyncDisposable {
    /// <summary>Bounds fixture polling and explicit lifecycle retries.</summary>
    internal static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    /// <summary>Owns the actual HWND needed by either productive presentation backend.</summary>
    private readonly HwndSource source;

    /// <summary>Prevents duplicate cleanup after explicit disposal regressions.</summary>
    private bool disposed;

    /// <summary>Loads the shipped template and explicitly selects its backend before native initialization.</summary>
    /// <param name="hwnd">Whether to select HWND rather than WPF composition.</param>
    /// <param name="effects">The application-owned manager borrowed by the viewport.</param>
    internal P4PresentationDriver(bool hwnd, IEffectsManager effects) {
        var resources = new ResourceDictionary {
            Source = new Uri("/SilkToolkit;component/Themes/Generic.xaml", UriKind.Relative)
        };
        Camera = new P4ObservedCamera {
            Position = new Point3D(0, 0, 5), LookDirection = new Vector3D(0, 0, -5),
            UpDirection = new Vector3D(0, 1, 0), Width = 2
        };
        Viewport = new Viewport3DX {
            EnableSwapChainRendering = hwnd,
            EffectsManager = effects,
            EnableDpiScale = false,
            BelongsToParentWindow = false,
            EnableAutoOctreeUpdate = false,
            EnableRenderFrustum = false,
            EnableD2DRendering = false,
            ShowViewCube = false,
            ShowCoordinateSystem = false,
            MinimumUpdateCount = 1,
            FxaaLevel = FXAALevel.None,
            BackgroundColor = Colors.Blue,
            Camera = Camera,
            Width = 64, Height = 48,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Style = (Style)resources[typeof(Viewport3DX)]
        };
        Viewport.StopRendering();
        Viewport.Rendered += HandleRendered;
        Viewport.OnRendered += HandleOnRendered;
        Root.Children.Add(Viewport);
        source = new HwndSource(new HwndSourceParameters(nameof(P4PresentationDriver)) {
            Width = 128, Height = 96, WindowStyle = unchecked((int)0x80000000)
        });
        source.RootVisual = Root;
        Root.UpdateLayout();
        Assert.True(Viewport.ApplyTemplate() || Viewport.Template is not null);
        Root.UpdateLayout();
        var template = Assert.IsType<ControlTemplate>(Viewport.Template);
        var presenter = Assert.IsType<ContentPresenter>(template.FindName("PART_Canvas", Viewport));
        if (hwnd)
            Native = Assert.IsType<D3D12PresentationSurface>(presenter.Content);
        else {
            Canvas = Assert.IsType<DPFCanvas>(presenter.Content);
            Wpf = ReadField<D3D12WpfPresentation>(Viewport, "wpfPresentation");
        }
    }

    /// <summary>Gets the loaded root used for genuine unload and rejoin operations.</summary>
    internal Grid Root { get; } = new();

    /// <summary>Gets the actual application viewport and render host.</summary>
    internal Viewport3DX Viewport { get; }

    /// <summary>Gets the actual camera whose time-step callback is exercised by the controller.</summary>
    internal P4ObservedCamera Camera { get; }

    /// <summary>Gets the selected native owner, when HWND rendering was explicitly requested.</summary>
    internal D3D12PresentationSurface? Native { get; }

    /// <summary>Gets the selected composited owner, when WPF rendering was explicitly requested.</summary>
    internal D3D12WpfPresentation? Wpf { get; }

    /// <summary>Gets the real template canvas and its bound D3DImage.</summary>
    internal DPFCanvas? Canvas { get; }

    /// <summary>Gets the actual recorder context exposed by the selected owner.</summary>
    internal RenderContext? Context => Wpf?.RenderContext ?? Native?.RenderContext;

    /// <summary>Gets the actual recorder's retained flattened scene.</summary>
    internal FastList<KeyValuePair<int, SceneNode>>? FlattenedScene => Wpf?.PerFrameFlattenedScene ?? Native?.PerFrameFlattenedScene;

    /// <summary>Gets accepted submissions, including zero-draw frames.</summary>
    internal ulong Submitted => Wpf?.SubmittedFrames ?? Native!.PresentedFrames;

    /// <summary>Gets observed real fence completions, independently of event delivery.</summary>
    internal ulong Completed => Wpf?.CompletedFrames ?? ReadField<D3D12FrameSubmission>(Native!, "_frameSubmission").CompletedFrames;

    /// <summary>Gets the number of actual host completion callbacks.</summary>
    internal int RenderedCount { get; private set; }

    /// <summary>Gets the number of actual viewport completion callbacks.</summary>
    internal int OnRenderedCount { get; private set; }

    /// <summary>Gets a stable value snapshot for submission, completion and both public events.</summary>
    internal (ulong Submitted, ulong Completed, int Rendered, int OnRendered) Counts =>
        (Submitted, Completed, RenderedCount, OnRenderedCount);

    /// <summary>Initializes and settles the productive presentation after real WPF loading.</summary>
    /// <returns>The bounded dispatcher-owned initialization.</returns>
    internal async Task WarmAsync() {
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        Assert.True(Viewport.IsLoaded);
        await DrainAsync();
        Assert.NotNull(Context);
        Assert.Equal(Submitted, Completed);
        Assert.True(Submitted > 0);
        if (Canvas is not null) Assert.NotEqual(nint.Zero, Canvas.ImageSource.BoundSurface);
    }

    /// <summary>Submits or polls the genuine viewport consumer without changing its backend's demand policy.</summary>
    /// <param name="wait">Whether an explicit caller permits a bounded GPU wait.</param>
    /// <returns>The productive recorder's draw count.</returns>
    internal int Tick(bool wait = false) => Viewport.RenderD3D12Frame(TimeSpan.FromSeconds(1), waitForCompletion: wait);

    /// <summary>Drains composited demand, or renders one explicit frame on the historically continuous HWND backend.</summary>
    /// <returns>The bounded dispatcher-owned frame and transport drain.</returns>
    internal Task DrainAsync() {
        if (Wpf is null) {
            return PollAsync(() => {
                if (!Viewport.CanSubmitNativeScene) return false;
                var before = Submitted;
                _ = Tick(wait: true);
                return Submitted > before && Submitted == Completed;
            }, "native lifecycle and accepted frame completion");
        }
        return PollAsync(() => {
            _ = Tick();
            var demand = Viewport.RenderDemand;
            return !Wpf.HasPendingTransport && !demand.HasRenderDemand &&
                !demand.HasRecoveryDemand && !demand.HasPublicationDemand;
        }, "viewport scene and WPF transport drain");
    }

    /// <summary>Finishes accepted transport while excluding fresh scene submissions.</summary>
    /// <returns>The bounded transport-only completion.</returns>
    internal async Task FinishTransportAsync() {
        if (Wpf is not null) {
            await PollAsync(() => {
                _ = Wpf.RenderFrame(TimeSpan.FromSeconds(1), automaticFrame: true);
                return !Wpf.HasPendingTransport;
            }, "accepted WPF transport");
        }
    }

    /// <summary>Reads a new explicit frame after pending presentation transport has completed.</summary>
    /// <returns>The new straight BGRA readback.</returns>
    internal async Task<D3D12CapturedFrame> CaptureAsync() {
        await FinishTransportAsync();
        var captured = Viewport.CaptureD3D12Frame();
        await FinishTransportAsync();
        return captured;
    }

    /// <summary>Checks the exact accepted-frame and completion-event delta.</summary>
    /// <param name="before">The actual counters before the isolated operation.</param>
    /// <param name="frames">The number of accepted scene submissions.</param>
    internal void AssertAdvanced((ulong Submitted, ulong Completed, int Rendered, int OnRendered) before, int frames) {
        Assert.Equal(before.Submitted + (ulong)frames, Submitted);
        Assert.Equal(before.Completed + (ulong)frames, Completed);
        Assert.Equal(before.Rendered + frames, RenderedCount);
        Assert.Equal(before.OnRendered + frames, OnRenderedCount);
    }

    /// <summary>Checks repeated idle WPF polls or ineligible native entry without changing native continuous behavior.</summary>
    internal void AssertIdleOrHidden() {
        if (Native is not null) Viewport.Visibility = Visibility.Hidden;
        var before = Counts;
        for (var tick = 0; tick < 8; tick++) Assert.False(Viewport.UpdateAndRender());
        Assert.Equal(before, Counts);
    }

    /// <summary>Snapshots actual WPF-bound pixels without using CPU pixels for presentation.</summary>
    /// <returns>The real image's diagnostic premultiplied BGRA snapshot.</returns>
    internal D3D12CapturedFrame Snapshot() {
        var canvas = Assert.IsType<DPFCanvas>(Canvas);
        var image = Assert.IsType<Image>(Assert.Single(canvas.Children.Cast<UIElement>()));
        image.UpdateLayout();
        var bitmap = new RenderTargetBitmap(64, 48, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(image);
        var pixels = new byte[64 * 48 * 4];
        bitmap.CopyPixels(pixels, 64 * 4, 0);
        return new D3D12CapturedFrame(64, 48, pixels);
    }

    /// <summary>Counts a genuine host completion and validates its sender.</summary>
    /// <param name="sender">The actual viewport host.</param>
    /// <param name="args">The productive completion arguments.</param>
    private void HandleRendered(object? sender, EventArgs args) {
        Assert.Same(Viewport, sender);
        RenderedCount++;
    }

    /// <summary>Counts the matching genuine viewport completion and validates its presentation owner.</summary>
    /// <param name="sender">The selected canvas or HWND surface.</param>
    /// <param name="args">The productive completion arguments.</param>
    private void HandleOnRendered(object? sender, EventArgs args) {
        Assert.Same((object?)Canvas ?? Native, sender);
        OnRenderedCount++;
    }

    /// <summary>Retries only bounded WPF detachment before releasing the native source; never disposes a borrowed manager.</summary>
    /// <returns>The dispatcher-owned fixture teardown.</returns>
    public async ValueTask DisposeAsync() {
        if (disposed) return;
        await PollAsync(() => {
            try { Viewport.Dispose(); return true; } catch (TimeoutException) { return false; }
        }, "viewport detachment and disposal");
        source.RootVisual = null;
        source.Dispose();
        disposed = true;
    }

    /// <summary>Creates an ordinary unlit mesh whose center has an unambiguous green pixel.</summary>
    /// <returns>The application-owned, productive mesh wrapper.</returns>
    internal static P4ObservedMeshModel CreateMesh() => new() {
        CullMode = CullMode.None,
        Material = new DiffuseMaterial { DiffuseColor = new Vector4(0, 1, 0, 1), EnableUnLit = true },
        Geometry = new MeshGeometry3D {
            Positions = [new(-0.5f, 0.5f, 0), new(0.5f, 0.5f, 0), new(-0.5f, -0.5f, 0), new(0.5f, -0.5f, 0)],
            Normals = [Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ],
            Indices = [0, 1, 2, 2, 1, 3]
        }
    };

    /// <summary>Mutates an actual material through the public WPF model path.</summary>
    /// <param name="mesh">The application-owned mesh.</param>
    /// <param name="color">The new straight material color.</param>
    internal static void SetMeshColor(MeshGeometryModel3D mesh, Vector4 color) =>
        Assert.IsType<DiffuseMaterial>(mesh.Material).DiffuseColor = color;

    /// <summary>Checks separate BGRA channels at an actual readback or WPF snapshot location.</summary>
    /// <param name="frame">The actual tightly packed frame.</param>
    /// <param name="x">The physical column.</param>
    /// <param name="y">The physical row.</param>
    /// <param name="blue">The expected blue byte.</param>
    /// <param name="green">The expected green byte.</param>
    /// <param name="red">The expected red byte.</param>
    /// <param name="alpha">The expected coverage byte.</param>
    /// <param name="tolerance">The allowed byte quantization error.</param>
    internal static void AssertPixel(D3D12CapturedFrame frame, int x, int y, int blue, int green, int red, int alpha,
        int tolerance = 2) {
        var offset = checked((y * (int)frame.Width + x) * 4);
        int[] expected = [blue, green, red, alpha];
        for (var channel = 0; channel < 4; channel++)
            Assert.InRange((int)frame.Pixels[offset + channel], Math.Max(0, expected[channel] - tolerance),
                Math.Min(255, expected[channel] + tolerance));
    }

    /// <summary>Yields only the bounded test driver while real dispatcher and GPU work progresses.</summary>
    /// <param name="completed">The real operation completion predicate.</param>
    /// <param name="operation">The operation named by a timeout.</param>
    /// <returns>The fixture-only polling task.</returns>
    /// <exception cref="TimeoutException">The actual operation exceeds the fixture deadline.</exception>
    internal static async Task PollAsync(Func<bool> completed, string operation) {
        var started = Stopwatch.GetTimestamp();
        while (!completed()) {
            if (Stopwatch.GetElapsedTime(started) >= Timeout) throw new TimeoutException(operation);
            await Task.Delay(1);
        }
    }

    /// <summary>Reads real native ownership or counters without modifying product scheduling or private state.</summary>
    /// <typeparam name="T">The actual field type.</typeparam>
    /// <param name="owner">The product owner.</param>
    /// <param name="name">The existing ownership field.</param>
    /// <returns>The actual retained value.</returns>
    internal static T ReadField<T>(object owner, string name) =>
        (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;
}

/// <summary>Retains normal camera behavior with a one-shot observer in its actual controller time step.</summary>
internal sealed class P4ObservedCamera : OrthographicCamera {
    /// <summary>Gets or sets a one-shot action run by the actual camera controller.</summary>
    internal Action? OnNextTimeStep { get; set; }

    /// <summary>Runs the normal camera time step and then the selected one-shot application callback.</summary>
    /// <returns>Whether the original camera still requests animation work.</returns>
    public override bool OnTimeStep() {
        var result = base.OnTimeStep();
        var callback = OnNextTimeStep;
        OnNextTimeStep = null;
        callback?.Invoke();
        return result;
    }
}

/// <summary>Creates a normal rendering mesh with a bounded application-update observer.</summary>
internal sealed class P4ObservedMeshModel : MeshGeometryModel3D {
    /// <summary>Creates a real mesh scene node without replacing native rendering behavior.</summary>
    /// <returns>The productive observed mesh node.</returns>
    protected override SceneNode OnCreateSceneNode() => new P4ObservedMeshNode();
}

/// <summary>Observes normal mesh updates without manufacturing GPU completion or attachment state.</summary>
internal sealed class P4ObservedMeshNode : MeshNode {
    /// <summary>Gets the actual context most recently passed to the productive scene update.</summary>
    internal RenderContext? LastContext { get; private set; }

    /// <summary>Gets or sets a one-shot callback on the next actual scene update.</summary>
    internal Action<RenderContext>? OnNextUpdate { get; set; }

    /// <summary>Runs normal mesh preparation before a one-shot application callback.</summary>
    /// <param name="context">The real native frame context.</param>
    public override void Update(RenderContext context) {
        LastContext = context;
        base.Update(context);
        var callback = OnNextUpdate;
        OnNextUpdate = null;
        callback?.Invoke(context);
    }
}

/// <summary>Blocks the actual selected presentation queue with a native fence rather than fake pending flags.</summary>
internal sealed class P4NativeQueueGate : IDisposable {
    /// <summary>Owns the CPU-signaled fence that gates productive commands.</summary>
    private readonly SilkD3D12Fence gate;

    /// <summary>Retains the native CPU fence-signal function.</summary>
    private readonly SignalFence signal;

    /// <summary>Ensures gate release is idempotent across worker and fixture cleanup.</summary>
    private int released;

    /// <summary>Inserts a genuine queue wait and creates an independent fence for quiescence evidence.</summary>
    /// <param name="driver">The initialized actual presentation driver.</param>
    internal P4NativeQueueGate(P4PresentationDriver driver) {
        var owner = (object?)driver.Wpf ?? driver.Native!;
        var device = P4PresentationDriver.ReadField<SilkD3D12Device>(owner, "device");
        Queue = P4PresentationDriver.ReadField<SilkD3D12CommandQueue>(owner, driver.Wpf is null ? "commandQueue" : "queue");
        gate = device.CreateFence();
        Probe = device.CreateFence();
        var wait = NativeMethod<WaitQueue>(Queue.NativePointer, 15);
        signal = NativeMethod<SignalFence>(gate.NativePointer, 10);
        Marshal.ThrowExceptionForHR(wait(Queue.NativePointer, gate.NativePointer, 1));
    }

    /// <summary>Gets the actual borrowed productive queue.</summary>
    internal SilkD3D12CommandQueue Queue { get; }

    /// <summary>Gets an independently owned native fence that survives productive generation retirement.</summary>
    internal SilkD3D12Fence Probe { get; }

    /// <summary>Releases productive queue work from the CPU exactly once.</summary>
    internal void Release() {
        if (Interlocked.Exchange(ref released, 1) != 0) return;
        Marshal.ThrowExceptionForHR(signal(gate.NativePointer, 1));
    }

    /// <summary>Releases the gate and drains its final use before freeing test-only native fences.</summary>
    public void Dispose() {
        if (gate.IsDisposed) return;
        Release();
        if (!Queue.IsDisposed) Probe.Wait(Queue.Signal(Probe), P4PresentationDriver.Timeout);
        Probe.Dispose();
        gate.Dispose();
    }

    /// <summary>Reads a stable COM entry point without enabling unsafe test project settings.</summary>
    /// <typeparam name="T">The exact native delegate type.</typeparam>
    /// <param name="instance">The real COM object.</param>
    /// <param name="slot">The stable COM vtable slot.</param>
    /// <returns>The native entry point.</returns>
    private static T NativeMethod<T>(nint instance, int slot) where T : Delegate =>
        Marshal.GetDelegateForFunctionPointer<T>(Marshal.ReadIntPtr(Marshal.ReadIntPtr(instance), slot * IntPtr.Size));

    /// <summary>Calls the native ID3D12CommandQueue wait method.</summary>
    /// <param name="queue">The actual queue.</param>
    /// <param name="fence">The actual gate fence.</param>
    /// <param name="value">The required gate value.</param>
    /// <returns>The native HRESULT.</returns>
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int WaitQueue(nint queue, nint fence, ulong value);

    /// <summary>Calls the native CPU fence-signal method.</summary>
    /// <param name="fence">The actual fence.</param>
    /// <param name="value">The signaled completion value.</param>
    /// <returns>The native HRESULT.</returns>
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int SignalFence(nint fence, ulong value);
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Controls;
using Xunit;
using Vector3 = Silk.NET.Maths.Vector3D<float>;

namespace SilkToolkit.Tests;

/// <summary>Checks public frame admission through real presentation owners and application callbacks.</summary>
[Collection(WpfCollection.Name)]
[Trait("Category", "Hardware")]
[Trait("Category", "DX12")]
public sealed class ViewportFrameAdmissionTests {
    /// <summary>Requires the initialized default WPF owner to supply readiness and its actual allocation.</summary>
    /// <returns>The dispatcher-owned hardware verification.</returns>
    [Fact(Explicit = true)]
    public Task DefaultWpfOwnerSuppliesReadyAndBridgeMetricsTest() => StaThread.RunAsync(async () => {
        using var effects = new DefaultEffectsManager();
        await using var driver = new P4PresentationDriver(false, effects);
        await FirstFrameAsync(driver);
        Assert.False(driver.Viewport.EnableSwapChainRendering);
        Assert.Null(driver.Viewport.D3D12Surface);
        Assert.True(driver.Wpf!.IsInitialized);
        Assert.True(driver.Canvas!.IsActive);
        Assert.True(driver.Viewport.IsRenderReady);
        AssertBridgeMetrics(driver, 1f);
    });

    /// <summary>Checks fractional render DPI and disabled scaling against the real bridge without getter side effects.</summary>
    /// <param name="dpi">The public requested render scale.</param>
    /// <param name="enabled">Whether physical render scaling is enabled.</param>
    /// <returns>The dispatcher-owned allocation and passive-observation verification.</returns>
    [Theory(Explicit = true)]
    [InlineData(1.25, true)]
    [InlineData(1.5, true)]
    [InlineData(1.5, false)]
    public Task WpfMetricsAtConfiguredDpiRemainPassiveTest(double dpi, bool enabled) => StaThread.RunAsync(async () => {
        using var effects = new DefaultEffectsManager();
        await using var driver = new P4PresentationDriver(false, effects);
        driver.Viewport.Width = 65;
        driver.Viewport.Height = 49;
        driver.Viewport.DpiScale = dpi;
        driver.Viewport.EnableDpiScale = enabled;
        driver.Root.UpdateLayout();
        await FirstFrameAsync(driver);
        var scale = enabled ? (float) dpi : 1f;
        AssertBridgeMetrics(driver, scale);
    });

    /// <summary>Rejects layout-ahead metrics while a genuine queue fence retains the old allocation and scale.</summary>
    /// <returns>The old-allocation observation and actual replacement-generation verification.</returns>
    [Fact(Explicit = true)]
    public Task PendingWpfResizeReportsOldAllocationOrZeroTest() => StaThread.RunAsync(async () => {
        using var effects = new DefaultEffectsManager();
        await using var driver = new P4PresentationDriver(false, effects);
        await LoadAsync(driver);
        var owner = driver.Wpf!;
        var before = driver.Counts;
        var callbacks = 0;
        var generation = 0UL;
        P4NativeQueueGate? gate = null;
        EventHandler? listener = null;
        listener = (_, _) => {
            driver.Viewport.OnRendering -= listener;
            callbacks++;
            Assert.True(owner.IsInitialized);
            gate = new P4NativeQueueGate(driver);
        };
        driver.Viewport.OnRendering += listener;
        try {
            driver.Viewport.InvalidateRender();
            _ = driver.Tick();
            Assert.Equal(1, callbacks);
            Assert.Equal(before.Submitted + 1, driver.Submitted);
            Assert.Equal(before.Completed, driver.Completed);
            var actualGate = Assert.IsType<P4NativeQueueGate>(gate);
            var bridge = P4PresentationDriver.ReadField<D3D12ImageInteropBridge>(owner, "bridge");
            var target = bridge.Target;
            generation = owner.Generation;
            driver.Viewport.Width = 83;
            driver.Viewport.Height = 57;
            driver.Viewport.EnableDpiScale = true;
            driver.Viewport.DpiScale = 1.25;
            driver.Root.UpdateLayout();
            Assert.NotEqual(target.Width, driver.Canvas!.PixelWidth);
            _ = driver.Tick();
            Assert.Equal(1, callbacks);
            var state = WpfState(driver);
            for (var read = 0; read < 16; read++) {
                _ = driver.Viewport.IsRenderReady;
                var available = driver.Viewport.TryGetPresentationMetrics(out var width, out var height, out var scale);
                if (available) {
                    Assert.Equal(target.Width, width);
                    Assert.Equal(target.Height, height);
                    Assert.Equal(1f, scale);
                } else {
                    Assert.Equal(0u, width);
                    Assert.Equal(0u, height);
                    Assert.Equal(0f, scale);
                }
            }
            Assert.Equal(state, WpfState(driver));
            Assert.Same(bridge, P4PresentationDriver.ReadField<D3D12ImageInteropBridge>(owner, "bridge"));
            Assert.Equal(generation, owner.Generation);
            actualGate.Release();
        } finally {
            driver.Viewport.OnRendering -= listener;
            gate?.Dispose();
        }
        await driver.FinishTransportAsync();
        Assert.False(owner.IsInitialized);
        _ = driver.Tick(wait: true);
        Assert.True(owner.Generation > generation);
        AssertBridgeMetrics(driver, 1.25f);
    });

    /// <summary>Orders the real pre-frame event before camera and original node update, then genuine GPU completion.</summary>
    /// <param name="hwnd">Whether the existing driver selects the HWND backend.</param>
    /// <returns>The single-submission event and recorder verification.</returns>
    [Theory(Explicit = true)]
    [InlineData(false)]
    [InlineData(true)]
    public Task BeforeFramePrecedesCameraNodeAndGpuCompletionTest(bool hwnd) => StaThread.RunAsync(async () => {
        using var effects = new DefaultEffectsManager();
        await using var driver = new P4PresentationDriver(hwnd, effects);
        var mesh = P4PresentationDriver.CreateMesh();
        driver.Viewport.Items.Add(mesh);
        var node = (P4ObservedMeshNode) mesh.SceneNode;
        await LoadAsync(driver);
        var before = driver.Counts;
        var order = new List<string>();
        driver.Viewport.OnRendering += (_, _) => {
            order.Add("before");
            Assert.True(driver.Viewport.IsRenderReady);
            Assert.True(driver.Viewport.IsPreparingPresentationFrame);
            Assert.Equal(before, driver.Counts);
        };
        driver.Camera.OnNextTimeStep = () => order.Add("camera");
        node.OnNextUpdate = _ => order.Add("node");
        driver.Viewport.Rendered += (_, _) => order.Add("host");
        driver.Viewport.OnRendered += (_, _) => order.Add("after");
        driver.Viewport.InvalidateRender();
        _ = driver.Tick(wait: true);
        driver.AssertAdvanced(before, 1);
        Assert.Equal(new[] { "before", "camera", "node", "host", "after" }, order);
        Assert.Same(driver.Context, node.LastContext);
        Assert.Same(driver.Camera.CameraInternal, node.LastContext!.Camera);
    });

    /// <summary>Allows callbacks only for actual scene admission, excluding idle, busy and stopped transport polls.</summary>
    /// <returns>The real queue-fence, transport and retained-demand verification.</returns>
    [Fact(Explicit = true)]
    public Task WpfIdleBusyAndTransportPollsDoNotRaiseBeforeFrameTest() => StaThread.RunAsync(async () => {
        using var effects = new DefaultEffectsManager();
        await using var driver = new P4PresentationDriver(false, effects);
        await driver.WarmAsync();
        var callbacks = 0;
        driver.Viewport.OnRendering += (_, _) => callbacks++;
        var idle = WpfState(driver);
        for (var poll = 0; poll < 4; poll++) _ = driver.Tick();
        Assert.Equal(0, callbacks);
        Assert.Equal(idle, WpfState(driver));
        var before = driver.Counts;
        var published = driver.Wpf!.PublishedFrames;
        using (var gate = new P4NativeQueueGate(driver)) {
            driver.Viewport.InvalidateRender();
            _ = driver.Tick();
            Assert.Equal(1, callbacks);
            Assert.Equal(before.Submitted + 1, driver.Submitted);
            Assert.Equal(before.Completed, driver.Completed);
            driver.Viewport.InvalidateRender();
            for (var poll = 0; poll < 4; poll++) _ = driver.Tick();
            Assert.Equal(1, callbacks);
            Assert.Equal(before.Submitted + 1, driver.Submitted);
            Assert.Equal(before.Completed, driver.Completed);
            Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);
            gate.Release();
            await driver.FinishTransportAsync();
            Assert.Equal(1, callbacks);
            driver.AssertAdvanced(before, 1);
            Assert.Equal(published + 1, driver.Wpf.PublishedFrames);
            Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);
        }
        await driver.DrainAsync();
        Assert.Equal(2, callbacks);
        driver.AssertAdvanced(before, 2);
        idle = WpfState(driver);
        for (var poll = 0; poll < 4; poll++) _ = driver.Tick();
        Assert.Equal(2, callbacks);
        Assert.Equal(idle, WpfState(driver));
    });

    /// <summary>Uses the camera installed by the real pre-frame listener in both camera advancement and scene recording.</summary>
    /// <param name="hwnd">Whether the existing driver selects the HWND backend.</param>
    /// <returns>The current-camera identity and recorder-position verification.</returns>
    [Theory(Explicit = true)]
    [InlineData(false)]
    [InlineData(true)]
    public Task BeforeFrameCameraReplacementReachesRecorderTest(bool hwnd) => StaThread.RunAsync(async () => {
        using var effects = new DefaultEffectsManager();
        await using var driver = new P4PresentationDriver(hwnd, effects);
        var mesh = P4PresentationDriver.CreateMesh();
        driver.Viewport.Items.Add(mesh);
        var node = (P4ObservedMeshNode) mesh.SceneNode;
        await LoadAsync(driver);
        var before = driver.Counts;
        var oldSteps = 0;
        var newSteps = 0;
        var callbacks = 0;
        var replacement = new P4ObservedCamera {
            Position = new Point3D(0.25, 0, 5), LookDirection = new Vector3D(-0.25, 0, -5),
            UpDirection = new Vector3D(0, 1, 0), Width = 2, OnNextTimeStep = () => newSteps++
        };
        driver.Camera.OnNextTimeStep = () => oldSteps++;
        EventHandler? listener = null;
        listener = (_, _) => {
            driver.Viewport.OnRendering -= listener;
            callbacks++;
            driver.Viewport.Camera = replacement;
        };
        driver.Viewport.OnRendering += listener;
        driver.Viewport.InvalidateRender();
        _ = driver.Tick(wait: true);
        Assert.Equal(1, callbacks);
        Assert.Equal(0, oldSteps);
        Assert.Equal(1, newSteps);
        driver.AssertAdvanced(before, 1);
        Assert.Same(replacement.CameraInternal, driver.Context!.Camera);
        Assert.Same(replacement.CameraInternal, node.LastContext!.Camera);
        Assert.Equal(new Vector3(0.25f, 0, 5), driver.Context.CameraParams.Position);
    });

    /// <summary>Rechecks live admission after a real listener withdraws visibility, automatic permission or the manager.</summary>
    /// <param name="transition">The actual public lifetime transition made inside the listener.</param>
    /// <returns>The canceled submission, preserved demand and successful later retry verification.</returns>
    [Theory(Explicit = true)]
    [InlineData("hide")]
    [InlineData("stop")]
    [InlineData("manager")]
    public Task BeforeFrameWithdrawalCancelsSubmissionAndRetainsDemandTest(string transition) =>
        StaThread.RunAsync(async () => {
            using var effects = new DefaultEffectsManager();
            using var replacement = new DefaultEffectsManager();
            await using var driver = new P4PresentationDriver(false, effects);
            var mesh = P4PresentationDriver.CreateMesh();
            driver.Viewport.Items.Add(mesh);
            var node = (P4ObservedMeshNode) mesh.SceneNode;
            await LoadAsync(driver);
            var before = driver.Counts;
            var callbacks = 0;
            var updates = 0;
            node.OnNextUpdate = _ => updates++;
            EventHandler? listener = null;
            listener = (_, _) => {
                driver.Viewport.OnRendering -= listener;
                callbacks++;
                Assert.True(driver.Viewport.IsPreparingPresentationFrame);
                switch (transition) {
                    case "hide": driver.Viewport.Visibility = Visibility.Hidden; break;
                    case "stop": driver.Viewport.StopRendering(); break;
                    case "manager": driver.Viewport.EffectsManager = replacement; break;
                }
                Assert.False(node.IsDisposed);
            };
            driver.Viewport.OnRendering += listener;
            driver.Viewport.InvalidateRender();
            driver.Viewport.StartRendering();
            try {
                _ = driver.Wpf!.RenderFrame(TimeSpan.FromSeconds(2), automaticFrame: true);
            } finally {
                driver.Viewport.StopRendering();
            }
            Assert.Equal(1, callbacks);
            Assert.Equal(0, updates);
            Assert.Equal(before, driver.Counts);
            Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);
            driver.Viewport.Visibility = Visibility.Visible;
            driver.Root.UpdateLayout();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            await P4PresentationDriver.PollAsync(() => {
                _ = driver.Tick(wait: true);
                return driver.Submitted > before.Submitted && driver.Submitted == driver.Completed;
            }, "first frame after admission withdrawal");
            Assert.True(driver.Submitted > before.Submitted);
            Assert.Equal(driver.Submitted, driver.Completed);
            Assert.Equal(1, updates);
            Assert.False(node.IsDisposed);
            if (transition == "manager") Assert.Same(replacement, node.EffectsManager);
        });

    /// <summary>Defers disposal requested by the real pre-frame listener until borrowed owners and nodes are safe.</summary>
    /// <param name="hwnd">Whether the existing driver selects the HWND backend.</param>
    /// <returns>The real callback unwind, no-submission and passive-disposal verification.</returns>
    [Theory(Explicit = true)]
    [InlineData(false)]
    [InlineData(true)]
    public Task BeforeFrameDisposeDefersLifetimeAndSubmitsNothingTest(bool hwnd) => StaThread.RunAsync(async () => {
        using var effects = new DefaultEffectsManager();
        await using var driver = new P4PresentationDriver(hwnd, effects);
        var mesh = P4PresentationDriver.CreateMesh();
        driver.Viewport.Items.Add(mesh);
        var node = (P4ObservedMeshNode) mesh.SceneNode;
        await LoadAsync(driver);
        var before = driver.Counts;
        D3D12ImageInteropBridge? bridge = null;
        var callbacks = 0;
        var disposals = 0;
        driver.Viewport.Disposing += (_, _) => {
            disposals++;
            var state = driver.Counts;
            AssertUnavailable(driver.Viewport);
            Assert.Equal(state, driver.Counts);
        };
        driver.Viewport.OnRendering += (_, _) => {
            callbacks++;
            Assert.True(driver.Viewport.IsPreparingPresentationFrame);
            if (hwnd) {
                Assert.True(driver.Native!.IsInitialized);
            } else {
                Assert.True(driver.Wpf!.IsInitialized);
                bridge = P4PresentationDriver.ReadField<D3D12ImageInteropBridge>(driver.Wpf, "bridge");
            }
            driver.Viewport.Dispose();
            Assert.False(driver.Viewport.IsDisposed);
            Assert.False(node.IsDisposed);
            if (bridge is not null) {
                Assert.False(bridge.IsDisposed);
                Assert.False(bridge.Target.ColorResource.IsDisposed);
                Assert.Same(bridge, P4PresentationDriver.ReadField<D3D12ImageInteropBridge>(driver.Wpf!, "bridge"));
            }
        };
        driver.Viewport.InvalidateRender();
        _ = driver.Tick();
        Assert.Equal(1, callbacks);
        Assert.Equal(before, driver.Counts);
        Assert.False(driver.Viewport.IsDisposed);
        await P4PresentationDriver.PollAsync(() => driver.Viewport.IsDisposed, "pre-frame deferred disposal");
        Assert.Equal(1, disposals);
        Assert.True(node.IsDisposed);
        Assert.False(effects.IsDisposed);
        if (bridge is not null) {
            Assert.True(bridge.IsDisposed);
            Assert.Equal(nint.Zero, driver.Canvas!.ImageSource.BoundSurface);
        }
        AssertUnavailable(driver.Viewport);
        _ = driver.Tick();
        driver.Viewport.Dispose();
        Assert.Equal(1, callbacks);
        Assert.Equal(1, disposals);
        Assert.Equal(before, driver.Counts);
    });

    /// <summary>Rejects template replacement and a second recording round from the actual pre-frame callback.</summary>
    /// <param name="hwnd">Whether the existing driver selects the HWND backend.</param>
    /// <returns>The original-owner survival, single-round and retained-follow-up-demand verification.</returns>
    [Theory(Explicit = true)]
    [InlineData(false)]
    [InlineData(true)]
    public Task BeforeFrameNestedRenderAndTemplateReplacementAreGuardedTest(bool hwnd) => StaThread.RunAsync(async () => {
        using var effects = new DefaultEffectsManager();
        await using var driver = new P4PresentationDriver(hwnd, effects);
        var mesh = P4PresentationDriver.CreateMesh();
        driver.Viewport.Items.Add(mesh);
        var node = (P4ObservedMeshNode) mesh.SceneNode;
        await LoadAsync(driver);
        var updates = 0;
        node.OnNextUpdate = _ => updates++;
        var before = driver.Counts;
        var template = driver.Viewport.Template;
        var callbacks = 0;
        EventHandler? listener = null;
        listener = (_, _) => {
            driver.Viewport.OnRendering -= listener;
            callbacks++;
            Assert.True(driver.Viewport.IsPreparingPresentationFrame);
            var nestedDraws = driver.Tick();
            Assert.Equal(0, nestedDraws);
            Assert.Equal(0, updates);
            Assert.Equal(before, driver.Counts);
            var replacement = new ControlTemplate(typeof(Viewport3DX));
            Assert.Throws<InvalidOperationException>(() => driver.Viewport.Template = replacement);
            Assert.Throws<InvalidOperationException>(() => driver.Viewport.OnApplyTemplate());
            Assert.Same(template, driver.Viewport.Template);
            if (driver.Wpf is not null)
                Assert.Same(driver.Wpf, P4PresentationDriver.ReadField<D3D12WpfPresentation>(driver.Viewport, "wpfPresentation"));
            else
                Assert.Same(driver.Native, driver.Viewport.D3D12Surface);
        };
        driver.Viewport.OnRendering += listener;
        driver.Viewport.InvalidateRender();
        _ = driver.Tick(wait: true);
        Assert.Equal(1, callbacks);
        Assert.Equal(1, updates);
        driver.AssertAdvanced(before, 1);
        Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);
        Assert.Same(template, driver.Viewport.Template);
    });

    /// <summary>Observes real unload, retained-image reload and initialized disposal without getter-driven native work.</summary>
    /// <returns>The passive lifetime and transport-only rebind verification.</returns>
    [Fact(Explicit = true)]
    public Task WpfGettersFollowActualUnloadReloadAndDisposingTest() => StaThread.RunAsync(async () => {
        using var effects = new DefaultEffectsManager();
        await using var driver = new P4PresentationDriver(false, effects);
        await driver.WarmAsync();
        var callbacks = 0;
        driver.Viewport.OnRendering += (_, _) => callbacks++;
        var before = driver.Counts;
        var generation = driver.Wpf!.Generation;
        var published = driver.Wpf.PublishedFrames;
        var rebound = driver.Wpf.ReboundFrames;
        driver.Root.Children.Remove(driver.Viewport);
        await P4PresentationDriver.PollAsync(() => !driver.Viewport.IsLoaded && !driver.Canvas!.IsActive, "real viewport unload");
        await driver.FinishTransportAsync();
        var unloaded = WpfState(driver);
        AssertUnavailable(driver.Viewport);
        Assert.Equal(unloaded, WpfState(driver));
        Assert.Equal(nint.Zero, driver.Canvas!.ImageSource.BoundSurface);
        driver.Root.Children.Add(driver.Viewport);
        driver.Root.UpdateLayout();
        await P4PresentationDriver.PollAsync(() => driver.Viewport.IsLoaded && driver.Canvas.IsActive, "real viewport reload");
        await driver.DrainAsync();
        Assert.Equal(before, driver.Counts);
        Assert.Equal(generation, driver.Wpf.Generation);
        Assert.Equal(published, driver.Wpf.PublishedFrames);
        Assert.True(driver.Wpf.ReboundFrames > rebound);
        Assert.Equal(0, callbacks);
        Assert.True(driver.Viewport.IsRenderReady);
        AssertBridgeMetrics(driver, 1f);
        var disposals = 0;
        driver.Viewport.Disposing += (_, _) => {
            disposals++;
            var disposing = WpfState(driver);
            AssertUnavailable(driver.Viewport);
            Assert.Equal(disposing, WpfState(driver));
            Assert.Null(P4PresentationDriver.ReadField<D3D12ImageInteropBridge?>(driver.Wpf, "bridge"));
        };
        await driver.DisposeAsync();
        Assert.Equal(1, disposals);
        var disposed = WpfState(driver);
        AssertUnavailable(driver.Viewport);
        Assert.Equal(disposed, WpfState(driver));
        Assert.Equal(0, callbacks);
    });

    /// <summary>Awaits genuine WPF loading without requiring native publication or consuming a scene frame.</summary>
    /// <param name="driver">The existing hidden-source driver.</param>
    /// <returns>The dispatcher-owned load observation.</returns>
    private static async Task LoadAsync(P4PresentationDriver driver) {
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        Assert.True(driver.Viewport.IsLoaded);
        if (driver.Canvas is not null) Assert.True(driver.Canvas.IsActive);
    }

    /// <summary>Completes the first real scene submission without requiring successful D3DImage publication.</summary>
    /// <param name="driver">The actual loaded presentation driver.</param>
    /// <returns>The initialized-owner and genuine GPU-completion verification.</returns>
    private static async Task FirstFrameAsync(P4PresentationDriver driver) {
        await LoadAsync(driver);
        var before = driver.Counts;
        _ = driver.Tick(wait: true);
        driver.AssertAdvanced(before, 1);
        Assert.NotNull(driver.Context);
    }

    /// <summary>Compares public observations with the live bridge target while retaining all productive counters.</summary>
    /// <param name="driver">The initialized actual WPF presentation driver.</param>
    /// <param name="expectedScale">The allocation-coherent effective render scale.</param>
    private static void AssertBridgeMetrics(P4PresentationDriver driver, float expectedScale) {
        var owner = driver.Wpf!;
        var bridge = P4PresentationDriver.ReadField<D3D12ImageInteropBridge>(owner, "bridge");
        var target = bridge.Target;
        var before = WpfState(driver);
        for (var read = 0; read < 16; read++) {
            _ = driver.Viewport.IsRenderReady;
            var available = driver.Viewport.TryGetPresentationMetrics(out var width, out var height, out var scale);
            Assert.True(available);
            Assert.Equal(target.Width, width);
            Assert.Equal(target.Height, height);
            Assert.Equal(expectedScale, scale);
            Assert.Equal((uint) Math.Ceiling(driver.Canvas!.ActualWidth * scale), width);
            Assert.Equal((uint) Math.Ceiling(driver.Canvas.ActualHeight * scale), height);
        }
        Assert.Equal(before, WpfState(driver));
        Assert.Same(bridge, P4PresentationDriver.ReadField<D3D12ImageInteropBridge>(owner, "bridge"));
    }

    /// <summary>Checks failure outputs repeatedly without driving any rendering or transport operation.</summary>
    /// <param name="viewport">The actually inactive or disposing viewport.</param>
    private static void AssertUnavailable(Viewport3DX viewport) {
        for (var read = 0; read < 16; read++) {
            Assert.False(viewport.IsRenderReady);
            var available = viewport.TryGetPresentationMetrics(out var width, out var height, out var scale);
            Assert.False(available);
            Assert.Equal(0u, width);
            Assert.Equal(0u, height);
            Assert.Equal(0f, scale);
        }
    }

    /// <summary>Reads only retained managed counters, including publication and generation, without a GPU health poll.</summary>
    /// <param name="driver">The actual WPF driver, including after native retirement.</param>
    /// <returns>The generation, transport and public-completion counter snapshot.</returns>
    private static (ulong Generation, ulong Submitted, ulong Completed, ulong Copied, ulong Published,
        ulong Rebound, int Rendered, int OnRendered) WpfState(P4PresentationDriver driver) =>
        (driver.Wpf!.Generation, driver.Submitted, driver.Completed, driver.Wpf.CopiedFrames,
            driver.Wpf.PublishedFrames, driver.Wpf.ReboundFrames, driver.RenderedCount, driver.OnRenderedCount);
}
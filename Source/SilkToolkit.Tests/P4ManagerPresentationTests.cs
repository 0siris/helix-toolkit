using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX;
using Xunit;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace SilkToolkit.Tests;

/// <summary>Exercises real manager transitions, GPU quiescence and shared viewport pixel survival in Phase 4.</summary>
[Collection(WpfCollection.Name)]
public sealed class P4ManagerPresentationTests {
    /// <summary>Defers manager replacement or same-instance epochs from actual node, camera and completion callbacks.</summary>
    /// <param name="hwnd">Whether to explicitly select the HWND backend.</param>
    /// <param name="callback">The real application callback that changes manager resources.</param>
    /// <param name="replace">Whether to replace identity rather than dispose and reinitialize the same manager.</param>
    /// <returns>The productive callback, cancellation and fresh-pixel verification.</returns>
    [Theory(Explicit = true)]
    [InlineData(false, "node", false)]
    [InlineData(false, "node", true)]
    [InlineData(false, "camera", false)]
    [InlineData(false, "camera", true)]
    [InlineData(false, "rendered", false)]
    [InlineData(false, "rendered", true)]
    [InlineData(true, "node", false)]
    [InlineData(true, "node", true)]
    [InlineData(true, "camera", false)]
    [InlineData(true, "camera", true)]
    [InlineData(true, "rendered", false)]
    [InlineData(true, "rendered", true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task ManagerTransitionsFromRealCallbacksRetireAfterUnwind(bool hwnd, string callback, bool replace) =>
        StaThread.RunAsync(async () => {
            using var original = new DefaultEffectsManager();
            using var replacement = new DefaultEffectsManager();
            await using var driver = new P4PresentationDriver(hwnd, original);
            var mesh = P4PresentationDriver.CreateMesh();
            driver.Viewport.Items.Add(mesh);
            var node = (P4ObservedMeshNode)mesh.SceneNode;
            await driver.WarmAsync();
            var context = driver.Context!;
            var before = driver.Counts;
            var callbacks = 0;
            Action transition = () => {
                callbacks++;
                Assert.True(driver.Viewport.IsPreparingPresentationFrame);
                Assert.Same(context, driver.Context);
                Assert.NotNull(context.Camera);
                if (replace)
                    driver.Viewport.EffectsManager = replacement;
                else {
                    original.DisposeAllResources();
                    Assert.False(driver.Viewport.CanSubmitNativeScene);
                    original.Reinitialize();
                }
                Assert.NotNull(context.Camera);
                Assert.True(node.IsAttached);
                Assert.False(original.IsDisposed);
                Assert.False(replacement.IsDisposed);
            };
            EventHandler? rendered = null;
            switch (callback) {
                case "node": node.OnNextUpdate = _ => transition(); break;
                case "camera": driver.Camera.OnNextTimeStep = transition; break;
                case "rendered":
                    rendered = (_, _) => {
                        driver.Viewport.Rendered -= rendered;
                        transition();
                    };
                    driver.Viewport.Rendered += rendered;
                    break;
            }

            driver.Viewport.InvalidateRender();
            _ = driver.Tick(wait: true);
            Assert.Equal(1, callbacks);
            driver.AssertAdvanced(before, callback == "rendered" ? 1 : 0);
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            await driver.DrainAsync();
            Assert.Null(context.Camera);
            Assert.NotSame(context, driver.Context);
            var selected = replace ? replacement : original;
            Assert.Same(selected, driver.Viewport.EffectsManager);
            Assert.Same(selected, node.EffectsManager);
            Assert.Same(driver.Viewport, node.RenderHost);
            Assert.True(original.Initialized);
            Assert.True(replacement.Initialized);
            P4PresentationDriver.SetMeshColor(mesh, new Vector4(1, 0, 0, 1));
            await driver.DrainAsync();
            P4PresentationDriver.AssertPixel(await driver.CaptureAsync(), 32, 24, 0, 0, 255, 255);
            Assert.Equal(driver.Submitted, driver.Completed);
            Assert.Equal((ulong)driver.RenderedCount, driver.Completed);
            Assert.Equal(driver.RenderedCount, driver.OnRenderedCount);
        });

    /// <summary>Requires manager disposal to drain genuine queued GPU use before freeing borrowed resources.</summary>
    /// <param name="hwnd">Whether to explicitly select the HWND backend.</param>
    /// <returns>The independently fenced resource-lifetime verification.</returns>
    [Theory(Explicit = true)]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task BorrowedManagerResourcesWaitForActualGpuQuiescence(bool hwnd) => StaThread.RunAsync(async () => {
        using var disposalStarted = new ManualResetEventSlim();
        using var effects = new DefaultEffectsManager();
        effects.DisposingResources += (_, _) => disposalStarted.Set();
        await using var driver = new P4PresentationDriver(hwnd, effects);
        driver.Viewport.Items.Add(P4PresentationDriver.CreateMesh());
        await driver.WarmAsync();
        var context = driver.Context!;
        using (var gate = new P4NativeQueueGate(driver)) {
            var before = driver.Counts;
            driver.Viewport.InvalidateRender();
            _ = driver.Tick();
            var probeValue = gate.Queue.Signal(gate.Probe);
            Assert.Equal(before.Submitted + 1, driver.Submitted);
            Assert.Equal(before.Completed, driver.Completed);
            Assert.True(gate.Probe.CompletedValue < probeValue);
            Assert.NotNull(context.Camera);
            var release = Task.Run(async () => {
                Assert.True(disposalStarted.Wait(P4PresentationDriver.Timeout));
                await Task.Delay(50);
                gate.Release();
            });
            try {
                effects.DisposeAllResources();
                Assert.True(gate.Probe.CompletedValue >= probeValue,
                    "Resource disposal returned before work on the actual borrowed queue was quiescent.");
            } finally {
                gate.Release();
                await release;
            }
            Assert.False(effects.IsDisposed);
            Assert.False(effects.Initialized);
            Assert.False(driver.Viewport.CanSubmitNativeScene);
            Assert.Null(context.Camera);
            Assert.Equal(before.Submitted + 1, driver.Submitted);
            driver.AssertAdvanced(before, 1);
        }

        effects.Reinitialize();
        await driver.DrainAsync();
        P4PresentationDriver.AssertPixel(await driver.CaptureAsync(), 32, 24, 0, 255, 0, 255);
        await driver.DisposeAsync();
        Assert.True(effects.Initialized);
        Assert.False(effects.IsDisposed);
    });

    /// <summary>Preflights every shared resource owner without blocking identity assignment on actual GPU use.</summary>
    /// <param name="secondHwnd">Whether the busy second consumer explicitly selects the HWND backend.</param>
    /// <returns>The genuine queue gate, retained barrier and automatic latest-value fanout verification.</returns>
    [Theory(Explicit = true)]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task SharedManagerIdentityChangeDoesNotWaitForBusyPeer(bool secondHwnd) => StaThread.RunAsync(async () => {
        using var original = new DefaultEffectsManager();
        using var intermediate = new DefaultEffectsManager();
        using var latest = new DefaultEffectsManager();
        using var mesh = P4PresentationDriver.CreateMesh();
        var container = new ModelContainer3DX { EffectsManager = original };
        container.Items.Add(mesh);
        await using var first = new P4PresentationDriver(false, original);
        await using var second = new P4PresentationDriver(secondHwnd, original);
        foreach (var driver in new[] { first, second }) {
            driver.Viewport.EnableSharedModelMode = true;
            driver.Viewport.SharedModelContainer = container;
            await driver.WarmAsync();
        }
        var firstContext = first.Context!;
        var secondContext = second.Context!;
        using (var gate = new P4NativeQueueGate(second)) {
            second.Viewport.InvalidateRender();
            _ = second.Tick();
            var probeValue = gate.Queue.Signal(gate.Probe);
            Assert.True(gate.Probe.CompletedValue < probeValue);
            var started = Stopwatch.GetTimestamp();
            container.EffectsManager = intermediate;
            container.EffectsManager = latest;
            Assert.True(Stopwatch.GetElapsedTime(started) < TimeSpan.FromSeconds(1),
                "Ordinary shared manager assignment waited for the gated native queue.");
            Assert.True(container.IsSynchronizingEffectsManager);
            Assert.True(P4PresentationDriver.ReadField<DispatcherTimer>(container, "managerRetryTimer").IsEnabled);
            Assert.False(first.Viewport.CanSubmitNativeScene);
            Assert.False(second.Viewport.CanSubmitNativeScene);
            var firstBefore = first.Counts;
            var secondBefore = second.Counts;
            _ = first.Tick();
            _ = second.Tick();
            await Task.Delay(150);
            Assert.Equal(firstBefore, first.Counts);
            Assert.Equal(secondBefore, second.Counts);
            Assert.Same(original, first.Viewport.EffectsManager);
            Assert.Same(original, second.Viewport.EffectsManager);
            Assert.Same(firstContext, first.Context);
            Assert.Same(secondContext, second.Context);
            Assert.NotNull(firstContext.Camera);
            Assert.NotNull(secondContext.Camera);
            Assert.True(mesh.SceneNode.RenderCore.IsAttached);
            Assert.Equal(2, P4PresentationDriver.ReadField<Viewport3DX[]>(container, "resourceConsumers").Length);
            gate.Release();
            // The retained timer must recover without another render tick or scheduling request.
            await P4PresentationDriver.PollAsync(() => !container.IsSynchronizingEffectsManager,
                "automatic shared manager retirement after GPU gate release");
            Assert.True(gate.Probe.CompletedValue >= probeValue);
        }
        Assert.Same(latest, first.Viewport.EffectsManager);
        Assert.Same(latest, second.Viewport.EffectsManager);
        Assert.Null(firstContext.Camera);
        Assert.Null(secondContext.Camera);
        Assert.Empty(P4PresentationDriver.ReadField<Viewport3DX[]>(container, "resourceConsumers"));
        Assert.False(P4PresentationDriver.ReadField<DispatcherTimer>(container, "managerRetryTimer").IsEnabled);
        await first.DrainAsync();
        await second.DrainAsync();
        await AssertPresentationPixelAsync(first, 0, 255, 0);
        await AssertPresentationPixelAsync(second, 0, 255, 0);
        Assert.Same(latest, mesh.SceneNode.EffectsManager);
        Assert.False(original.IsDisposed);
        Assert.False(intermediate.IsDisposed);
        Assert.False(latest.IsDisposed);
    });

    /// <summary>Drains an unloaded peer's actual queued GPU use before a shared manager frees its resources.</summary>
    /// <returns>The synchronous disposal and retained inactive-owner verification.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task SharedManagerDisposalDrainsUnloadedPeerGpuUse() => StaThread.RunAsync(async () => {
        using var disposalStarted = new ManualResetEventSlim();
        using var effects = new DefaultEffectsManager();
        effects.DisposingResources += (_, _) => disposalStarted.Set();
        using var mesh = P4PresentationDriver.CreateMesh();
        var container = new ModelContainer3DX { EffectsManager = effects };
        container.Items.Add(mesh);
        await using var first = new P4PresentationDriver(false, effects);
        await using var second = new P4PresentationDriver(false, effects);
        foreach (var driver in new[] { first, second }) {
            driver.Viewport.EnableSharedModelMode = true;
            driver.Viewport.SharedModelContainer = container;
            await driver.WarmAsync();
        }
        using (var gate = new P4NativeQueueGate(second)) {
            second.Viewport.InvalidateRender();
            _ = second.Tick();
            var probeValue = gate.Queue.Signal(gate.Probe);
            second.Root.Children.Remove(second.Viewport);
            await P4PresentationDriver.PollAsync(() => !second.Viewport.IsLoaded, "shared peer unload");
            Assert.True(gate.Probe.CompletedValue < probeValue);
            Assert.Null(P4PresentationDriver.ReadField<ModelContainer3DX?>(second.Viewport, "registeredModelContainer"));
            Assert.Same(container,
                P4PresentationDriver.ReadField<ModelContainer3DX?>(second.Viewport, "resourceModelContainer"));
            var release = Task.Run(async () => {
                Assert.True(disposalStarted.Wait(P4PresentationDriver.Timeout));
                await Task.Delay(50);
                gate.Release();
            });
            try {
                effects.DisposeAllResources();
                Assert.True(gate.Probe.CompletedValue >= probeValue,
                    "Shared manager disposal returned before the unloaded peer's actual queue was quiescent.");
            } finally {
                gate.Release();
                await release;
            }
        }
        Assert.Empty(P4PresentationDriver.ReadField<Viewport3DX[]>(container, "resourceConsumers"));
        Assert.False(first.Viewport.CanSubmitNativeScene);
        Assert.False(second.Viewport.CanSubmitNativeScene);
        Assert.False(mesh.SceneNode.RenderCore.IsAttached);
        effects.Reinitialize();
        await first.DrainAsync();
        await AssertPresentationPixelAsync(first, 0, 255, 0);
        Assert.False(effects.IsDisposed);
    });

    /// <summary>Rebinds an unchanged shared WPF generation across real unload without scene or copy work.</summary>
    /// <returns>The actual retained context, generation, image and final registration release verification.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task UnchangedSharedReloadRebindsRetainedTransportWithoutSceneWork() => StaThread.RunAsync(async () => {
        using var effects = new DefaultEffectsManager();
        using var mesh = P4PresentationDriver.CreateMesh();
        var container = new ModelContainer3DX { EffectsManager = effects };
        container.Items.Add(mesh);
        await using var driver = new P4PresentationDriver(false, effects);
        driver.Viewport.EnableSharedModelMode = true;
        driver.Viewport.SharedModelContainer = container;
        await driver.WarmAsync();
        var owner = driver.Wpf!;
        var context = driver.Context;
        var before = driver.Counts;
        var copied = owner.CopiedFrames;
        var published = owner.PublishedFrames;
        var rebound = owner.ReboundFrames;
        var generation = owner.Generation;
        driver.Root.Children.Remove(driver.Viewport);
        await P4PresentationDriver.PollAsync(() => !driver.Viewport.IsLoaded, "unchanged shared viewport unload");
        await driver.FinishTransportAsync();
        Assert.Equal(nint.Zero, driver.Canvas!.ImageSource.BoundSurface);
        Assert.Same(context, owner.RenderContext);
        Assert.Contains(driver.Viewport, P4PresentationDriver.ReadField<Viewport3DX[]>(container, "resourceConsumers"));
        Assert.False(driver.Viewport.RenderDemand.HasRenderDemand);
        driver.Root.Children.Add(driver.Viewport);
        driver.Root.UpdateLayout();
        await P4PresentationDriver.PollAsync(() => driver.Viewport.IsLoaded, "unchanged shared viewport reload");
        await driver.DrainAsync();
        Assert.Equal(before, driver.Counts);
        Assert.Equal(copied, owner.CopiedFrames);
        Assert.Equal(published, owner.PublishedFrames);
        Assert.True(owner.ReboundFrames > rebound);
        Assert.Equal(generation, owner.Generation);
        Assert.Same(context, owner.RenderContext);
        Assert.NotEqual(nint.Zero, driver.Canvas.ImageSource.BoundSurface);
        P4PresentationDriver.AssertPixel(driver.Snapshot(), 32, 24, 0, 255, 0, 255);
        await driver.DisposeAsync();
        Assert.Empty(P4PresentationDriver.ReadField<Viewport3DX[]>(container, "resourceConsumers"));
        Assert.Null(P4PresentationDriver.ReadField<ModelContainer3DX?>(driver.Viewport, "resourceModelContainer"));
        Assert.False(mesh.SceneNode.IsDisposed);
        Assert.False(effects.IsDisposed);
    });

    /// <summary>Prevents a real retirement-completion listener from resubscribing a viewport it has just disposed.</summary>
    /// <param name="hwnd">Whether to explicitly select the HWND backend.</param>
    /// <returns>The reentrant final-disposal subscription regression.</returns>
    [Theory(Explicit = true)]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task RetirementCompletionDisposalDoesNotResubscribeBorrowedManager(bool hwnd) => StaThread.RunAsync(async () => {
        using var original = new DefaultEffectsManager();
        using var replacement = new DefaultEffectsManager();
        await using var driver = new P4PresentationDriver(hwnd, original);
        await driver.WarmAsync();
        using (var gate = new P4NativeQueueGate(driver)) {
            driver.Viewport.InvalidateRender();
            _ = driver.Tick();
            var calls = 0;
            driver.Viewport.Rendered += (_, _) => {
                calls++;
                driver.Viewport.Dispose();
            };
            gate.Release();
            driver.Viewport.EffectsManager = replacement;
            await P4PresentationDriver.PollAsync(() => driver.Viewport.IsDisposed, "retirement completion disposal");
            Assert.Equal(1, calls);
            Assert.True(driver.Viewport.IsDisposed);
            Assert.Null(P4PresentationDriver.ReadField<IEffectsManager?>(driver.Viewport, "subscribedEffectsManager"));
            var before = driver.Counts;
            original.RaiseInvalidateRender();
            replacement.RaiseInvalidateRender();
            replacement.DisposeAllResources();
            replacement.Reinitialize();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.Equal(before, driver.Counts);
            Assert.Null(P4PresentationDriver.ReadField<IEffectsManager?>(driver.Viewport, "subscribedEffectsManager"));
            Assert.False(original.IsDisposed);
            Assert.False(replacement.IsDisposed);
        }
    });

    /// <summary>Preserves actual shared mesh pixels after either consumer unloads or closes, then rejoins under a new manager.</summary>
    /// <param name="secondHwnd">Whether the second viewport explicitly selects HWND, producing a mixed pair.</param>
    /// <param name="leaveSecond">Whether the second rather than first actual consumer leaves.</param>
    /// <param name="dispose">Whether to finally dispose rather than temporarily unload the leaving viewport.</param>
    /// <returns>The real two-viewport invalidation, publication and ownership verification.</returns>
    [Theory(Explicit = true)]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task SharedViewportSurvivesPeerDepartureMutationAndManagerRejoin(bool secondHwnd, bool leaveSecond, bool dispose) =>
        StaThread.RunAsync(async () => {
            using var effects = new DefaultEffectsManager();
            using var replacement = new DefaultEffectsManager();
            using var mesh = P4PresentationDriver.CreateMesh();
            using var nested = new GroupModel3D();
            using var root = new GroupModel3D();
            nested.Children.Add(mesh);
            root.Children.Add(nested);
            var container = new ModelContainer3DX { EffectsManager = effects };
            container.Items.Add(root);
            await using var first = new P4PresentationDriver(false, effects);
            await using var second = new P4PresentationDriver(secondHwnd, effects);
            foreach (var driver in new[] { first, second }) {
                driver.Viewport.EnableSharedModelMode = true;
                driver.Viewport.SharedModelContainer = container;
                await driver.WarmAsync();
            }
            P4PresentationDriver.AssertPixel(await first.CaptureAsync(), 32, 24, 0, 255, 0, 255);
            P4PresentationDriver.AssertPixel(await second.CaptureAsync(), 32, 24, 0, 255, 0, 255);
            var firstBefore = first.Submitted;
            var secondBefore = second.Submitted;
            P4PresentationDriver.SetMeshColor(mesh, new Vector4(1, 0, 0, 1));
            Assert.True(first.Viewport.RenderDemand.HasRenderDemand);
            Assert.True(second.Viewport.RenderDemand.HasRenderDemand);
            await first.DrainAsync();
            await second.DrainAsync();
            Assert.True(first.Submitted > firstBefore);
            Assert.True(second.Submitted > secondBefore);
            AssertFreshRedPresentation(first);
            AssertFreshRedPresentation(second);
            await AssertPresentationPixelAsync(first, 0, 0, 255);
            await AssertPresentationPixelAsync(second, 0, 0, 255);
            effects.DisposeAllResources();
            Assert.False(first.Viewport.CanSubmitNativeScene);
            Assert.False(second.Viewport.CanSubmitNativeScene);
            effects.Reinitialize();
            await first.DrainAsync();
            await second.DrainAsync();
            await AssertPresentationPixelAsync(first, 0, 0, 255);
            await AssertPresentationPixelAsync(second, 0, 0, 255);

            var leaving = leaveSecond ? second : first;
            var survivor = leaveSecond ? first : second;
            var leavingContext = leaving.Context;
            if (dispose) await leaving.DisposeAsync();
            else {
                leaving.Root.Children.Remove(leaving.Viewport);
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                Assert.False(leaving.Viewport.IsLoaded);
                Assert.Null(P4PresentationDriver.ReadField<ModelContainer3DX?>(leaving.Viewport, "registeredModelContainer"));
                if (leaving.Wpf is not null) {
                    Assert.Same(leavingContext, leaving.Context);
                    Assert.NotNull(leavingContext!.Camera);
                    Assert.Same(container,
                        P4PresentationDriver.ReadField<ModelContainer3DX?>(leaving.Viewport, "resourceModelContainer"));
                    Assert.Contains(leaving.Viewport,
                        P4PresentationDriver.ReadField<Viewport3DX[]>(container, "resourceConsumers"));
                }
            }
            Assert.True(mesh.SceneNode.IsAttached);
            Assert.False(mesh.SceneNode.IsDisposed);
            Assert.Same(effects, mesh.SceneNode.EffectsManager);
            Assert.False(effects.IsDisposed);

            var survivorBefore = survivor.Submitted;
            mesh.Visibility = Visibility.Hidden;
            Assert.True(survivor.Viewport.RenderDemand.HasRenderDemand);
            await survivor.DrainAsync();
            Assert.True(survivor.Submitted > survivorBefore);
            await AssertPresentationPixelAsync(survivor, 255, 0, 0);
            mesh.Visibility = Visibility.Visible;
            P4PresentationDriver.SetMeshColor(mesh, new Vector4(0, 1, 0, 1));
            await survivor.DrainAsync();
            await AssertPresentationPixelAsync(survivor, 0, 255, 0);

            nested.Children.Remove(mesh);
            Assert.True(survivor.Viewport.RenderDemand.HasRenderDemand);
            await survivor.DrainAsync();
            await AssertPresentationPixelAsync(survivor, 255, 0, 0);
            nested.Children.Add(mesh);
            await survivor.DrainAsync();
            await AssertPresentationPixelAsync(survivor, 0, 255, 0);

            container.Items.Clear();
            Assert.True(survivor.Viewport.RenderDemand.HasRenderDemand);
            await survivor.DrainAsync();
            await AssertPresentationPixelAsync(survivor, 255, 0, 0);
            container.Items.Add(root);
            await survivor.DrainAsync();
            await AssertPresentationPixelAsync(survivor, 0, 255, 0);
            container.EffectsManager = replacement;
            await P4PresentationDriver.PollAsync(() => !container.IsSynchronizingEffectsManager,
                "shared manager replacement after peer departure");
            Assert.Empty(P4PresentationDriver.ReadField<Viewport3DX[]>(container, "resourceConsumers"));
            Assert.Null(P4PresentationDriver.ReadField<ModelContainer3DX?>(leaving.Viewport, "resourceModelContainer"));
            Assert.Null(leavingContext!.Camera);
            Assert.False(mesh.SceneNode.RenderCore.IsAttached);
            await survivor.DrainAsync();
            Assert.Same(replacement, survivor.Viewport.EffectsManager);
            Assert.Same(replacement, mesh.SceneNode.EffectsManager);
            await AssertPresentationPixelAsync(survivor, 0, 255, 0);

            if (dispose) {
                await using var rejoined = new P4PresentationDriver(leaving.Viewport.EnableSwapChainRendering, replacement);
                rejoined.Viewport.EnableSharedModelMode = true;
                rejoined.Viewport.SharedModelContainer = container;
                await rejoined.WarmAsync();
                await AssertPresentationPixelAsync(rejoined, 0, 255, 0);
                await VerifyRejoinedMutationAsync(rejoined, survivor, mesh);
            } else {
                leaving.Root.Children.Add(leaving.Viewport);
                leaving.Root.UpdateLayout();
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                await leaving.DrainAsync();
                Assert.True(leaving.Viewport.IsLoaded);
                Assert.Same(replacement, leaving.Viewport.EffectsManager);
                await AssertPresentationPixelAsync(leaving, 0, 255, 0);
                await VerifyRejoinedMutationAsync(leaving, survivor, mesh);
            }
            Assert.False(effects.IsDisposed);
            Assert.False(replacement.IsDisposed);
            Assert.False(mesh.SceneNode.IsDisposed);
        });

    /// <summary>Checks the current composited image after shared descendant invalidation.</summary>
    /// <param name="driver">The actual viewport presentation.</param>
    private static void AssertFreshRedPresentation(P4PresentationDriver driver) {
        if (driver.Wpf is not null) P4PresentationDriver.AssertPixel(driver.Snapshot(), 32, 24, 0, 0, 255, 255);
    }

    /// <summary>Checks new actual submission and capture pixels, including the bound WPF image where available.</summary>
    /// <param name="driver">The real surviving or rejoined viewport.</param>
    /// <param name="blue">The expected blue byte.</param>
    /// <param name="green">The expected green byte.</param>
    /// <param name="red">The expected red byte.</param>
    /// <returns>The bounded capture and publication verification.</returns>
    private static async Task AssertPresentationPixelAsync(P4PresentationDriver driver, int blue, int green, int red) {
        var before = driver.Submitted;
        var captured = await driver.CaptureAsync();
        Assert.True(driver.Submitted > before);
        P4PresentationDriver.AssertPixel(captured, 32, 24, blue, green, red, 255);
        if (driver.Wpf is not null) P4PresentationDriver.AssertPixel(driver.Snapshot(), 32, 24, blue, green, red, 255);
    }

    /// <summary>Requires both actual consumers to wake and publish a new descendant color after manager rejoin.</summary>
    /// <param name="rejoined">The actual reloaded or replacement viewport.</param>
    /// <param name="survivor">The viewport that remained active throughout.</param>
    /// <param name="mesh">The original shared application mesh.</param>
    /// <returns>The fresh-pixel verification for both consumers.</returns>
    private static async Task VerifyRejoinedMutationAsync(P4PresentationDriver rejoined, P4PresentationDriver survivor,
        MeshGeometryModel3D mesh) {
        P4PresentationDriver.SetMeshColor(mesh, new Vector4(1, 0, 0, 1));
        Assert.True(rejoined.Viewport.RenderDemand.HasRenderDemand);
        Assert.True(survivor.Viewport.RenderDemand.HasRenderDemand);
        await rejoined.DrainAsync();
        await survivor.DrainAsync();
        await AssertPresentationPixelAsync(rejoined, 0, 0, 255);
        await AssertPresentationPixelAsync(survivor, 0, 0, 255);
    }
}

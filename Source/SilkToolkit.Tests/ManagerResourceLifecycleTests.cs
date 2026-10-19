using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Controls;
using Xunit;

namespace SilkToolkit.Tests;

/// <summary>Checks real viewport subscriptions and shared notification membership without native initialization.</summary>
[Collection(WpfCollection.Name)]
public sealed class ManagerResourceLifecycleTests {
    /// <summary>Resumes existing and late consumers when a later disposal observer rejects the resource transition.</summary>
    /// <param name="shared">Whether both viewports participate in a shared retirement barrier.</param>
    /// <param name="preparing">Whether an active frame defers retirement until after the rejected notification.</param>
    /// <returns>The managed verification of resource rollback, retirement barriers and invalidation wakeups.</returns>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public Task RejectedResourceDisposalResumesExistingAndLateConsumers(bool shared, bool preparing) =>
        StaThread.RunAsync(async () => {
                using var manager = new DefaultEffectsManager();
                var context = manager.DeviceContext2D;
                var geometry = manager.GeometryBufferManager;
                var container = new ModelContainer3DX { EffectsManager = manager };
                var observed = false;
                EventHandler<EventArgs> observing = (_, _) => {
                    Assert.True(manager.Initialized);
                    Assert.False(manager.ResourcesAvailable);
                    observed = true;
                };
                manager.DisposingResources += observing;
                using var existing = new Viewport3DX {
                    EffectsManager = manager, EnableSharedModelMode = shared, SharedModelContainer = container
                };
                existing.PrepareHostFrameContracts();
                var updating = typeof(Viewport3DX).GetField("isD3D12FrameUpdating", BindingFlags.Instance | BindingFlags.NonPublic)!;
                updating.SetValue(existing, preparing);
                Viewport3DX? late = null;
                var rejection = new TimeoutException("Injected rejection after the viewport disposal callback.");
                EventHandler<EventArgs> rejecting = (_, _) => {
                    Assert.True(observed);
                    Assert.False(existing.CanSubmitNativeScene);
                    Assert.Equal(shared && preparing, container.IsSynchronizingEffectsManager);
                    late = new Viewport3DX { EffectsManager = manager, EnableSharedModelMode = shared, SharedModelContainer = container };
                    late.PrepareHostFrameContracts();
                    Assert.False(late.CanSubmitNativeScene);
                    throw rejection;
                };
                manager.DisposingResources += rejecting;
                try {
                    Assert.Same(rejection, Assert.Throws<TimeoutException>(manager.DisposeAllResources));
                    Assert.NotNull(late);
                    Assert.True(manager.Initialized);
                    Assert.True(manager.ResourcesAvailable);
                    Assert.False(manager.IsDisposed);
                    Assert.Same(context, manager.DeviceContext2D);
                    Assert.False(context.IsDisposed);
                    Assert.Same(geometry, manager.GeometryBufferManager);
                    manager.Reinitialize();
                    Assert.Same(context, manager.DeviceContext2D);
                    Assert.Equal(!preparing, existing.CanSubmitNativeScene);
                    Assert.Equal(!(shared && preparing), late.CanSubmitNativeScene);

                    updating.SetValue(existing, false);
                    existing.ScheduleManagerLifecycle();
                    await Dispatcher.CurrentDispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                    Assert.False(container.IsSynchronizingEffectsManager);
                    existing.PrepareHostFrameContracts();
                    late.PrepareHostFrameContracts();
                    Assert.True(existing.CanSubmitNativeScene);
                    Assert.True(late.CanSubmitNativeScene);
                    Assert.True(existing.RenderDemand.HasRenderDemand);
                    Assert.True(late.RenderDemand.HasRenderDemand);
                    Drain(existing.RenderDemand);
                    Drain(late.RenderDemand);
                    Assert.False(existing.RenderDemand.HasRenderDemand);
                    Assert.False(late.RenderDemand.HasRenderDemand);
                    manager.RaiseInvalidateRender();
                    Assert.True(existing.RenderDemand.HasRenderDemand);
                    Assert.True(late.RenderDemand.HasRenderDemand);
                } finally {
                    manager.DisposingResources -= observing;
                    manager.DisposingResources -= rejecting;
                    updating.SetValue(existing, false);
                    late?.Dispose();
                }
            }
        );

    /// <summary>Blocks a consumer created inside a disposal notification until the manager publishes fresh resources.</summary>
    /// <param name="shared">Whether the late viewport also joins an active shared container.</param>
    /// <returns>The real manager-event subscription and preparation-availability verification.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task ConsumerJoiningDuringResourceDisposalWaitsForReinitialization(bool shared) => StaThread.RunAsync(() => {
            using var manager = new DefaultEffectsManager();
            var container = new ModelContainer3DX { EffectsManager = manager };
            Viewport3DX? late = null;
            EventHandler<EventArgs> joining = (_, _) => {
                Assert.True(manager.Initialized);
                Assert.False(manager.ResourcesAvailable);
                late = new Viewport3DX { EffectsManager = manager, EnableSharedModelMode = shared, SharedModelContainer = container };
                late.PrepareHostFrameContracts();
                Assert.False(late.CanSubmitNativeScene);
                Assert.False(late.UpdateAndRender());
                Assert.Null(
                    typeof(Viewport3DX).GetField("resourceModelContainer", BindingFlags.Instance | BindingFlags.NonPublic)!
                        .GetValue(late)
                );
            };
            manager.DisposingResources += joining;
            using var existing = new Viewport3DX {
                EffectsManager = manager, EnableSharedModelMode = shared, SharedModelContainer = container
            };
            existing.PrepareHostFrameContracts();
            try {
                manager.DisposeAllResources();
                manager.DisposingResources -= joining;
                Assert.NotNull(late);
                Assert.False(manager.ResourcesAvailable);
                Assert.False(late.CanSubmitNativeScene);
                Assert.False(existing.CanSubmitNativeScene);
                manager.Reinitialize();
                Assert.True(manager.ResourcesAvailable);
                Assert.True(late.CanSubmitNativeScene);
                Assert.True(existing.CanSubmitNativeScene);
                Assert.True(late.RenderDemand.HasRenderDemand);
            } finally {
                manager.DisposingResources -= joining;
                late?.Dispose();
            }
        }
    );

    /// <summary>Keeps manager handlers detached when completion delivery disposes a retiring viewport.</summary>
    /// <returns>The managed dispatcher verification of reentrant retirement.</returns>
    [Fact]
    public Task CompletionDisposalDoesNotRestoreManagerSubscriptions() => StaThread.RunAsync(() => {
            using var first = new DefaultEffectsManager();
            using var second = new DefaultEffectsManager();
            using var viewport = new Viewport3DX { EnableSwapChainRendering = true, EffectsManager = first };
            using var surface = new D3D12PresentationSurface { RenderHost = viewport };
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(Viewport3DX).GetField("d3d12Surface", flags)!.SetValue(viewport, surface);
            var submission = (D3D12FrameSubmission) typeof(D3D12PresentationSurface)
                .GetField("_frameSubmission", flags)!.GetValue(surface)!;
            submission.Submit(1, TimeSpan.Zero);
            Assert.True(submission.TryComplete(1, TimeSpan.Zero));
            viewport.Rendered += (_, _) => viewport.Dispose();
            viewport.EffectsManager = second;
            Assert.True(viewport.IsDisposed);
            Assert.Null(typeof(Viewport3DX).GetField("subscribedEffectsManager", flags)!.GetValue(viewport));
            Assert.True(first.Initialized);
            Assert.True(second.Initialized);
        }
    );

    /// <summary>Transfers invalidation subscriptions and keeps both externally supplied managers alive.</summary>
    /// <returns>The dispatcher-owned lifecycle verification.</returns>
    [Fact]
    public Task ManagerReplacementUnsubscribesThePreviousBorrowedOwner() => StaThread.RunAsync(() => {
            using var first = new DefaultEffectsManager();
            using var second = new DefaultEffectsManager();
            using var viewport = new Viewport3DX { EffectsManager = first };
            Drain(viewport.RenderDemand);
            first.RaiseInvalidateRender();
            Assert.True(viewport.RenderDemand.HasRenderDemand);
            viewport.EffectsManager = second;
            Drain(viewport.RenderDemand);
            first.RaiseInvalidateRender();
            Assert.False(viewport.RenderDemand.HasRenderDemand);
            second.RaiseInvalidateRender();
            Assert.True(viewport.RenderDemand.HasRenderDemand);
            viewport.Dispose();
            Assert.True(first.Initialized);
            Assert.True(second.Initialized);
        }
    );

    /// <summary>Suspends two consumers across same-instance resource disposal and resumes them after reinitialization.</summary>
    /// <returns>The real manager-event verification.</returns>
    [Fact]
    public Task SharedManagerResourceEpochSuspendsAndWakesBothViewports() => StaThread.RunAsync(() => {
            using var manager = new DefaultEffectsManager();
            using var first = new Viewport3DX { EffectsManager = manager };
            using var second = new Viewport3DX { EffectsManager = manager };
            manager.DisposeAllResources();
            Assert.False(first.CanSubmitNativeScene);
            Assert.False(second.CanSubmitNativeScene);
            manager.Reinitialize();
            Assert.True(first.CanSubmitNativeScene);
            Assert.True(second.CanSubmitNativeScene);
            Assert.True(first.RenderDemand.HasRenderDemand);
            Assert.True(second.RenderDemand.HasRenderDemand);
            first.Dispose();
            Assert.True(manager.Initialized);
            Drain(second.RenderDemand);
            manager.RaiseInvalidateRender();
            Assert.True(second.RenderDemand.HasRenderDemand);
        }
    );

    /// <summary>Marshals worker resource events before they return while preserving immediate worker invalidation demand.</summary>
    /// <returns>The asynchronous dispatcher verification.</returns>
    [Fact]
    public Task WorkerManagerEventsPreserveResourceOrdering() => StaThread.RunAsync(async () => {
            using var manager = new DefaultEffectsManager();
            using var viewport = new Viewport3DX { EffectsManager = manager };
            Drain(viewport.RenderDemand);
            await Task.Run(manager.RaiseInvalidateRender);
            Assert.True(viewport.RenderDemand.HasRenderDemand);
            await Task.Run(manager.DisposeAllResources);
            Assert.False(viewport.CanSubmitNativeScene);
            await Task.Run(manager.Reinitialize);
            Assert.True(viewport.CanSubmitNativeScene);
            Assert.True(viewport.RenderDemand.HasRenderDemand);
        }
    );

    /// <summary>Connects the actual frame-entry registration to unload and final disposal without disposing shared nodes.</summary>
    /// <returns>The dispatcher-owned shared membership verification.</returns>
    [Fact]
    public Task ActualViewportMembershipStopsOnUnloadAndReturnsForExplicitRendering() => StaThread.RunAsync(() => {
            var node = new GroupModel3D();
            var container = new ModelContainer3DX();
            container.Items.Add(node);
            using var first = new Viewport3DX { EnableSharedModelMode = true, SharedModelContainer = container };
            using var second = new Viewport3DX { EnableSharedModelMode = true, SharedModelContainer = container };
            first.PrepareHostFrameContracts();
            second.PrepareHostFrameContracts();
            Drain(first.RenderDemand);
            Drain(second.RenderDemand);
            node.Visibility = Visibility.Hidden;
            Assert.True(first.RenderDemand.HasRenderDemand);
            Assert.True(second.RenderDemand.HasRenderDemand);
            first.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
            Drain(first.RenderDemand);
            Drain(second.RenderDemand);
            node.Visibility = Visibility.Visible;
            Assert.False(first.RenderDemand.HasRenderDemand);
            Assert.True(second.RenderDemand.HasRenderDemand);
            first.PrepareHostFrameContracts();
            first.Dispose();
            Drain(second.RenderDemand);
            node.Visibility = Visibility.Hidden;
            Assert.True(second.RenderDemand.HasRenderDemand);
            Assert.False(node.SceneNode.IsDisposed);
            second.Dispose();
            node.Dispose();
        }
    );

    /// <summary>Consumes only already requested work, leaving no synthetic invalidation in the measured state.</summary>
    /// <param name="demand">The viewport's actual bounded demand coordinator.</param>
    private static void Drain(D3D12RenderDemand demand) {
        while (demand.TryBeginFrame(out _)) demand.CompleteFrame(true);
    }
}
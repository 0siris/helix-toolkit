using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;
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
using Point3D = System.Windows.Media.Media3D.Point3D;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector3D = System.Windows.Media.Media3D.Vector3D;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace SilkToolkit.Tests;

/// <summary>Checks shared root notifications and actual independent native consumers of application-owned nodes.</summary>
[Collection(WpfCollection.Name)]
public sealed class SharedSceneConsumerTests {
    /// <summary>Retains mutation observation across notification unload and restores only genuinely stale scene demand.</summary>
    /// <returns>The worker-thread revision, stable reload and retained pending-demand verification.</returns>
    [Fact]
    public Task RetainedSharedOwnerObservesWorkerInvalidationWithoutReloadRedraw() => StaThread.RunAsync(async () => {
            using var viewport = new Viewport3DX { EnableSharedModelMode = true };
            using var root = new GroupModel3D();
            using var child = new MeshGeometryModel3D();
            root.Children.Add(child);
            var container = new ModelContainer3DX();
            container.Items.Add(root);
            viewport.SharedModelContainer = container;
            viewport.PrepareHostFrameContracts();
            Drain(viewport);
            viewport.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
            var revision = container.SceneRevision;
            Assert.Null(PrivateField<ModelContainer3DX?>(viewport, "registeredModelContainer"));
            Assert.Same(container, PrivateField<ModelContainer3DX?>(viewport, "resourceModelContainer"));
            viewport.PrepareHostFrameContracts();
            Assert.False(viewport.RenderDemand.HasRenderDemand);
            Assert.Equal(revision, container.SceneRevision);

            viewport.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
            viewport.InvalidateRender();
            viewport.PrepareHostFrameContracts();
            Assert.True(viewport.RenderDemand.HasRenderDemand);
            Drain(viewport);
            viewport.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
            await Task.Run(child.SceneNode.InvalidateRender);
            Assert.NotEqual(revision, container.SceneRevision);
            Assert.False(viewport.RenderDemand.HasRenderDemand);
            viewport.PrepareHostFrameContracts();
            Assert.True(viewport.RenderDemand.HasRenderDemand);
            viewport.Dispose();
            Assert.Empty(PrivateField<Viewport3DX[]>(container, "resourceConsumers"));
            Assert.Null(PrivateField<ModelContainer3DX?>(viewport, "resourceModelContainer"));
            revision = container.SceneRevision;
            child.SceneNode.InvalidateRender();
            Assert.Equal(revision, container.SceneRevision);
        }
    );

    /// <summary>Preserves an old container's resource membership until deferred selection retirement actually succeeds.</summary>
    /// <param name="disableSharedMode">Whether to disable sharing rather than replace the selected container.</param>
    /// <returns>The retained preparation barrier, old-reference release and new-owner registration verification.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task SharedSelectionChangeRetiresOldResourceMembership(bool disableSharedMode) => StaThread.RunAsync(async () => {
            using var viewport = new Viewport3DX { EnableSharedModelMode = true };
            var original = new ModelContainer3DX();
            var replacement = new ModelContainer3DX();
            viewport.SharedModelContainer = original;
            viewport.PrepareHostFrameContracts();
            viewport.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
            var preparing = typeof(Viewport3DX).GetField("isD3D12FrameUpdating", BindingFlags.Instance | BindingFlags.NonPublic)!;
            try {
                preparing.SetValue(viewport, true);
                if (disableSharedMode) viewport.EnableSharedModelMode = false;
                else viewport.SharedModelContainer = replacement;
                Assert.Same(original, PrivateField<ModelContainer3DX?>(viewport, "resourceModelContainer"));
                Assert.Contains(viewport, PrivateField<Viewport3DX[]>(original, "resourceConsumers"));
                Assert.False(viewport.CanSubmitNativeScene);
                preparing.SetValue(viewport, false);
                viewport.ScheduleManagerLifecycle();
                await PollAsync(() => viewport.CanSubmitNativeScene);
                Assert.Empty(PrivateField<Viewport3DX[]>(original, "resourceConsumers"));
                Assert.Null(PrivateField<ModelContainer3DX?>(viewport, "resourceModelContainer"));
                viewport.PrepareHostFrameContracts();
                if (disableSharedMode)
                    Assert.Null(PrivateField<ModelContainer3DX?>(viewport, "resourceModelContainer"));
                else
                    Assert.Same(replacement, PrivateField<ModelContainer3DX?>(viewport, "resourceModelContainer"));
                viewport.Dispose();
                Assert.Empty(PrivateField<Viewport3DX[]>(original, "resourceConsumers"));
                Assert.Empty(PrivateField<Viewport3DX[]>(replacement, "resourceConsumers"));
            } finally {
                preparing.SetValue(viewport, false);
            }
        }
    );

    /// <summary>Waits for an unloaded resource owner's frame exit despite its absent notification membership.</summary>
    /// <returns>The resource-snapshot preparation barrier and one-pass retirement completion verification.</returns>
    [Fact]
    public Task PreparingUnloadedResourceOwnerStillBlocksSharedManagerChange() => StaThread.RunAsync(async () => {
            using var original = new DefaultEffectsManager(new EffectsManagerConfiguration { EnableSoftwareRendering = true });
            using var replacement = new DefaultEffectsManager(new EffectsManagerConfiguration { EnableSoftwareRendering = true });
            using var active = new Viewport3DX { EnableSharedModelMode = true, EffectsManager = original };
            using var unloaded = new Viewport3DX { EnableSharedModelMode = true, EffectsManager = original };
            var container = new ModelContainer3DX { EffectsManager = original };
            foreach (var viewport in new[] { active, unloaded }) {
                viewport.SharedModelContainer = container;
                viewport.PrepareHostFrameContracts();
            }

            unloaded.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
            var preparing = typeof(Viewport3DX).GetField("isD3D12FrameUpdating", BindingFlags.Instance | BindingFlags.NonPublic)!;
            try {
                preparing.SetValue(unloaded, true);
                container.EffectsManager = replacement;
                container.SchedulePendingEffectsManagerSynchronization();
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                Assert.True(container.IsSynchronizingEffectsManager);
                Assert.Same(original, active.EffectsManager);
                Assert.Same(original, unloaded.EffectsManager);
                Assert.Equal(
                    2, PrivateField<Viewport3DX[]>(container, "resourceConsumers")
                        .Length
                );
                Assert.False(PrivateField<bool>(container, "managerChangeQueued"));
                preparing.SetValue(unloaded, false);
                unloaded.ScheduleManagerLifecycle();
                await PollAsync(() => !container.IsSynchronizingEffectsManager);
                Assert.Same(replacement, active.EffectsManager);
                Assert.Same(original, unloaded.EffectsManager);
                Assert.Empty(PrivateField<Viewport3DX[]>(container, "resourceConsumers"));
                Assert.Null(PrivateField<ModelContainer3DX?>(unloaded, "resourceModelContainer"));
                Assert.False(PrivateField<bool>(container, "managerChangeQueued"));
                Assert.False(original.IsDisposed);
                Assert.False(replacement.IsDisposed);
            } finally {
                preparing.SetValue(unloaded, false);
            }
        }
    );

    /// <summary>Retains real bound native resources across lock timeouts and automatically completes the latest fanout.</summary>
    /// <returns>The genuine WPF write-lock failure, contained callback timeout and automatic timer recovery verification.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task SharedRetirementAutomaticallyRetriesRealImageLockTimeout() => StaThread.RunAsync(async () => {
            using var original = new DefaultEffectsManager();
            using var intermediate = new DefaultEffectsManager();
            using var latest = new DefaultEffectsManager();
            using var first = new Viewport3DX {
                EffectsManager = original,
                BelongsToParentWindow = false,
                EnableSharedModelMode = true,
                Camera = new HelixToolkit.Wpf.SharpDX.OrthographicCamera {
                    Position = new Point3D(0, 0, 2), LookDirection = new Vector3D(0, 0, -2), UpDirection = new Vector3D(0, 1, 0), Width = 2
                }
            };
            using var second = new Viewport3DX { EffectsManager = original };
            using var model = CreateQuad();
            var container = new ModelContainer3DX { EffectsManager = original };
            container.Items.Add(model);
            first.SharedModelContainer = container;
            container.RegisterConsumer(first);
            container.RegisterConsumer(second);
            using var canvas = new DPFCanvas { Width = 32, Height = 24 };
            using var source = new HwndSource(
                new HwndSourceParameters(nameof(SharedSceneConsumerTests)) {
                    Width = 32, Height = 24, WindowStyle = unchecked((int) 0x80000000)
                }
            );
            var owner = first.InitializeWpfPresentation(canvas);
            first.StopRendering();
            source.RootVisual = canvas;
            canvas.UpdateLayout();
            try {
                await PollAsync(() => {
                        _ = owner.RenderFrame(TimeSpan.Zero, waitForCompletion: true);
                        return owner.IsInitialized && !owner.HasPendingTransport && !first.RenderDemand.HasRenderDemand &&
                               canvas.ImageSource.BoundSurface != 0;
                    }
                );
                var bridge = PrivateField<D3D12ImageInteropBridge>(owner, "bridge");
                var device = PrivateField<SilkD3D12Device>(owner, "device");
                var renderer = owner.RenderContext;
                var bound = canvas.ImageSource.BoundSurface;
                Assert.True(model.SceneNode.RenderCore.IsAttached);
                var field = typeof(D3DImage).GetField("_canWriteEvent", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(field);
                var canWrite = Assert.IsAssignableFrom<EventWaitHandle>(field.GetValue(canvas.ImageSource.NativeImage));
                try {
                    canWrite.Reset();
                    AssertImageWriteLockFails(canvas.ImageSource.NativeImage);
                    Assert.Throws<TimeoutException>(container.RequestSharedResourceRetirement);
                    Assert.Throws<TimeoutException>(() => container.EffectsManager = intermediate);
                    Assert.Throws<TimeoutException>(() => container.EffectsManager = latest);
                    container.UnregisterConsumer(second);
                    // Execute the actual queued callback without pumping WPF while its borrowed write event is reset.
                    var callback = PrivateField<Action>(container, "synchronizeEffectsManagerCallback");
                    callback();
                    AssertImageWriteLockFails(canvas.ImageSource.NativeImage);
                    Assert.True(container.IsSynchronizingEffectsManager);
                    Assert.True(
                        PrivateField<DispatcherTimer>(container, "managerRetryTimer")
                            .IsEnabled
                    );
                    Assert.Same(original, first.EffectsManager);
                    Assert.Same(original, second.EffectsManager);
                    Assert.Same(bridge, PrivateField<D3D12ImageInteropBridge>(owner, "bridge"));
                    Assert.Same(device, PrivateField<SilkD3D12Device>(owner, "device"));
                    Assert.Same(renderer, owner.RenderContext);
                    Assert.Equal(bound, canvas.ImageSource.BoundSurface);
                    Assert.True(model.SceneNode.RenderCore.IsAttached);
                    Assert.False(bridge.IsDisposed);
                    Assert.False(device.IsDisposed);
                } finally {
                    canWrite.Set();
                }

                // No new render, manager request or scheduling hook triggers recovery after the real lock is released.
                await PollAsync(() => !container.IsSynchronizingEffectsManager);
                Assert.Same(latest, first.EffectsManager);
                Assert.Same(original, second.EffectsManager);
                Assert.False(
                    PrivateField<DispatcherTimer>(container, "managerRetryTimer")
                        .IsEnabled
                );
                Assert.True(bridge.IsDisposed);
                Assert.False(model.SceneNode.RenderCore.IsAttached);
                Assert.False(model.SceneNode.IsDisposed);
                Assert.False(original.IsDisposed);
                Assert.False(intermediate.IsDisposed);
                Assert.False(latest.IsDisposed);
            } finally {
                container.UnregisterConsumer(first);
                container.UnregisterConsumer(second);
                await PollAsync(() => {
                        try {
                            first.Dispose();
                            return true;
                        } catch (TimeoutException) {
                            return false;
                        }
                    }
                );
                source.RootVisual = null;
            }
        }
    );

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
            container.SchedulePendingEffectsManagerSynchronization();
            await Dispatcher.CurrentDispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            Assert.Same(latest, first.EffectsManager);
            Assert.Same(latest, second.EffectsManager);
            Assert.False(container.IsSynchronizingEffectsManager);
            Assert.False(original.IsDisposed);
            Assert.False(intermediate.IsDisposed);
            Assert.False(latest.IsDisposed);
            container.UnregisterConsumer(first);
            container.UnregisterConsumer(second);
        }
    );

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
            container.SchedulePendingEffectsManagerSynchronization();
            await Dispatcher.CurrentDispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            Assert.False(container.IsSynchronizingEffectsManager);
            Assert.Same(firstEffects, first.EffectsManager);
            Assert.Same(secondEffects, second.EffectsManager);
            Assert.Null(container.EffectsManager);
            Assert.False(firstEffects.IsDisposed);
            Assert.False(secondEffects.IsDisposed);
            container.UnregisterConsumer(first);
            container.UnregisterConsumer(second);
        }
    );

    /// <summary>Waits for frame exit without reposting inside a nested dispatcher pump or allocating repeated scheduling checks.</summary>
    /// <returns>The bounded nested pump, warmed allocation and eventual barrier-release verification.</returns>
    [Fact]
    public Task PreparingConsumerDoesNotRepostDuringNestedPump() => StaThread.RunAsync(async () => {
            using var host = new Viewport3DX();
            var container = new ModelContainer3DX();
            container.RegisterConsumer(host);
            var dispatcher = Dispatcher.CurrentDispatcher;
            await dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            var preparing = typeof(Viewport3DX).GetField("isD3D12FrameUpdating", BindingFlags.Instance | BindingFlags.NonPublic)!;
            preparing.SetValue(host, true);
            var posted = 0;
            DispatcherHookEventHandler count = (_, args) => {
                if (args.Operation.Priority == DispatcherPriority.Background) posted++;
            };
            dispatcher.Hooks.OperationPosted += count;
            try {
                container.RequestSharedResourceRetirement();
                var frame = new DispatcherFrame();
                var exit = new DispatcherTimer(DispatcherPriority.Send, dispatcher) { Interval = TimeSpan.FromMilliseconds(30) };
                exit.Tick += (_, _) => {
                    exit.Stop();
                    frame.Continue = false;
                };
                exit.Start();
                Dispatcher.PushFrame(frame);
                Assert.Equal(0, posted);
                Assert.True(container.IsSynchronizingEffectsManager);
                for (var index = 0; index < 64; index++) container.SchedulePendingEffectsManagerSynchronization();
                var before = GC.GetAllocatedBytesForCurrentThread();
                for (var index = 0; index < 1_000; index++) container.SchedulePendingEffectsManagerSynchronization();
                var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                Assert.Equal(0, allocated);
                Assert.Equal(0, posted);

                preparing.SetValue(host, false);
                for (var index = 0; index < 1_000; index++) container.SchedulePendingEffectsManagerSynchronization();
                Assert.Equal(1, posted);
                await dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                Assert.False(container.IsSynchronizingEffectsManager);
                Assert.Equal(1, posted);
                before = GC.GetAllocatedBytesForCurrentThread();
                for (var index = 0; index < 1_000; index++) container.SchedulePendingEffectsManagerSynchronization();
                allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                Assert.Equal(0, allocated);
            } finally {
                preparing.SetValue(host, false);
                dispatcher.Hooks.OperationPosted -= count;
                container.UnregisterConsumer(host);
            }
        }
    );

    /// <summary>Excludes a consumer that leaves during preparation from the eventual latest-manager fanout.</summary>
    /// <returns>The dispatcher-owned membership and stale-manager verification.</returns>
    [Fact]
    public Task DeferredManagerFanoutSkipsUnregisteredConsumer() => StaThread.RunAsync(async () => {
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
            container.UnregisterConsumer(second);
            await Dispatcher.CurrentDispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            Assert.Same(latest, first.EffectsManager);
            Assert.Same(original, second.EffectsManager);
            Assert.False(container.IsSynchronizingEffectsManager);
            preparing.SetValue(second, false);
            container.SchedulePendingEffectsManagerSynchronization();
            await Dispatcher.CurrentDispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            Assert.Same(original, second.EffectsManager);
            container.UnregisterConsumer(first);
        }
    );

    /// <summary>Wakes every real viewport demand coordinator when a null-manager native hierarchy loses children.</summary>
    /// <param name="clear">Whether to clear instead of removing the single child.</param>
    /// <returns>The dispatcher-owned hierarchy mutation and renderer-lease verification.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task NullManagerLeasedRemovalWakesEveryViewport(bool clear) => StaThread.RunAsync(() => {
            using var first = new Viewport3DX();
            using var second = new Viewport3DX();
            using var group = new GroupModel3D();
            using var child = new GroupNode();
            var root = Assert.IsAssignableFrom<GroupNodeBase>(group.SceneNode);
            root.AddChildNode(child);
            var container = new ModelContainer3DX();
            container.Items.Add(group);
            container.RegisterConsumer(first);
            container.RegisterConsumer(second);
            var owner = new object();
            root.RetainNativeAttachment(owner, second);
            child.RetainNativeAttachment(owner, second);
            Assert.True(root.Attach());
            Assert.True(child.Attach());
            Assert.False(root.IsAttached);
            Assert.False(child.IsAttached);
            Drain(first, second);

            if (clear) root.Clear();
            else Assert.True(root.RemoveChildNode(child));

            Assert.True(first.RenderDemand.HasRenderDemand);
            Assert.True(second.RenderDemand.HasRenderDemand);
            Assert.True(child.RenderCore.IsAttached);
            Assert.Null(child.Parent);
            child.ReleaseNativeAttachment(owner);
            root.ReleaseNativeAttachment(owner);
            Assert.False(child.RenderCore.IsAttached);
            container.UnregisterConsumer(first);
            container.UnregisterConsumer(second);
        }
    );

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
        }
    );

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

            var leaving = disposeLatest
                ? second
                : first;
            var surviving = disposeLatest
                ? first
                : second;
            var leavingHost = disposeLatest
                ? secondHost
                : firstHost;
            var survivingHost = disposeLatest
                ? firstHost
                : secondHost;
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
        }
    );

    /// <summary>Proves genuine image write contention while balancing the framework's normally returned lock count.</summary>
    /// <param name="image">The real bound image with its borrowed write event reset.</param>
    private static void AssertImageWriteLockFails(D3DImage image) {
        Assert.True(image.IsFrontBufferAvailable);
        var locked = image.TryLock(new Duration(TimeSpan.Zero));
        try {
            Assert.False(locked);
        } finally {
            image.Unlock();
        }
    }

    /// <summary>Reads retained productive owners without substituting fake retirement behavior.</summary>
    /// <typeparam name="T">The existing field type.</typeparam>
    /// <param name="owner">The productive owner.</param>
    /// <param name="name">The private field name.</param>
    /// <returns>The actual retained field value.</returns>
    private static T PrivateField<T>(object owner, string name) =>
        (T) owner.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;

    /// <summary>Bounds test-only dispatcher continuations while waiting for genuine transport or retirement progress.</summary>
    /// <param name="completed">The real progress predicate.</param>
    /// <returns>The test-only dispatcher wait.</returns>
    /// <exception cref="TimeoutException">The productive operation did not complete within five seconds.</exception>
    private static async Task PollAsync(Func<bool> completed) {
        var clock = Stopwatch.StartNew();
        while (!completed()) {
            if (clock.Elapsed >= TimeSpan.FromSeconds(5))
                throw new TimeoutException("Shared scene retirement did not complete within five seconds.");
            await Task.Delay(1);
        }
    }

    /// <summary>Consumes demand without substituting notification counters for the real viewport coordinator.</summary>
    /// <param name="hosts">The real hosts whose existing budgets are drained.</param>
    private static void Drain(params Viewport3DX[] hosts) {
        foreach (var host in hosts)
            while (host.RenderDemand.TryBeginFrame(out _))
                host.RenderDemand.CompleteFrame(true);
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
            Position = new Vector3(0, 0, 2),
            LookDirection = new Vector3(0, 0, -2),
            UpDirection = Vector3.UnitY,
            Width = 2,
            NearPlaneDistance = 0.1f,
            FarPlaneDistance = 10
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
            SceneNode[] roots = node is null
                ? []
                : [node];
            var draws = renderer.RecordViewportFrame(
                context, in target, 1, [0, 0, 1, 1], roots, camera,
                false, autoUpdateOctree: false, renderHost: host
            );
            var footprint = device.GetCopyableFootprint(color, out var bytes);
            using var readback = device.CreateBuffer(bytes, HeapType.Readback);
            context.Transition(color, ResourceStates.CopySource);
            context.CopyTextureToBuffer(readback, color, in footprint);
            context.Close();
            queue.Execute(context);
            fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));
            device.ThrowIfDeviceRemoved();
            Assert.Equal(
                node is null
                    ? 0
                    : 1, draws
            );
            byte[] expectedCenter = node is null
                ? [0, 0, 255, 255]
                : [0, 255, 0, 255];
            Assert.Equal(
                expectedCenter, readback.Read(
                    4,
                    footprint.Offset + 12UL * footprint.Footprint.RowPitch + 16UL * 4
                )
            );
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
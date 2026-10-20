using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Utilities;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Controls;
using Silk.NET.Direct3D12;
using Xunit;

namespace SilkToolkit.Tests;

/// <summary>Qualifies queued native recovery exceptions through the viewport's established rendering error boundary.</summary>
/// <remarks>Uses actual process-device removal and real scene detachment, without resetting the physical adapter.</remarks>
[Collection(WpfCollection.Name)]
public sealed class P5QueuedRecoveryFailureTests {
    /// <summary>Preserves synchronous caller-visible retirement exceptions instead of reporting them as queued failures.</summary>
    /// <returns>The managed dispatcher-owned callback contract verification.</returns>
    [Fact]
    public Task SynchronousRetirementPreservesItsExceptionAndOwnershipBarrier() => StaThread.RunAsync(() => {
            var failure = new InvalidOperationException("Synchronous retirement failure.");
            var queuedCalls = 0;
            Action retire = () => throw failure;
            Action queued = () => queuedCalls++;
            var registration = new D3D12PresentationDeviceRecovery(404, Dispatcher.CurrentDispatcher, retire, queued);
            try {
                Assert.Same(failure, Assert.Throws<InvalidOperationException>(registration.RequestRemoval));
                Assert.Equal(0, queuedCalls);
                Assert.True(registration.IsRemovalRequested);
                Assert.False(registration.CanRecreate);
            } finally {
                registration.Unregister();
                GC.KeepAlive(retire);
                GC.KeepAlive(queued);
            }
        }
    );

    /// <summary>Reports a queued probe's real detach failure and retains ownership if the boundary's immediate retry fails too.</summary>
    /// <param name="hwnd">Whether the failing owner uses the native HWND transport.</param>
    /// <param name="failRetry">Whether a second real node fails during the boundary's single teardown retry.</param>
    /// <returns>The real removal, handled error, native ownership retry and fresh output qualification.</returns>
    [Theory(Explicit = true)]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task QueuedProbeReportsDetachFailureWithoutDispatcherEscape(bool hwnd, bool failRetry) =>
        StaThread.RunAsync(async () => {
                using var effects = new DefaultEffectsManager();
                await using var driver = new P4PresentationDriver(hwnd, effects);
                using var firstMesh = P4PresentationDriver.CreateMesh();
                using var secondMesh = P4PresentationDriver.CreateMesh();
                driver.Viewport.Items.Add(firstMesh);
                if (failRetry) driver.Viewport.Items.Add(secondMesh);
                await driver.WarmAsync();
                await SuspendAsync(driver);
                var device = Device(driver);
                await AssertQueuedFailureAsync(
                    driver, [firstMesh.SceneNode, secondMesh.SceneNode], failRetry, () => {
                        RemoveDevice(device);
                        D3D12PresentationDeviceRecovery.ProbeForRemoval();
                    }
                );
                Assert.False(effects.IsDisposed);
                Assert.False(firstMesh.SceneNode.IsDisposed);
                await AssertFreshPixelsAsync(hwnd, effects);
            }
        );

    /// <summary>Reports real detachment failure from another dispatcher's queued removal notification.</summary>
    /// <param name="hwnd">Whether the receiving owner uses the HWND transport.</param>
    /// <param name="failRetry">Whether the error boundary's immediate teardown also fails.</param>
    /// <returns>The nonblocking cross-dispatcher notification, retained ownership and controlled retry qualification.</returns>
    [Theory(Explicit = true)]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task CrossDispatcherRetirementReportsDetachFailureWithoutEscape(bool hwnd, bool failRetry) =>
        StaThread.RunAsync(async () => {
                using var effects = new DefaultEffectsManager();
                await using var initiator = new P4PresentationDriver(false, effects);
                using var mesh = P4PresentationDriver.CreateMesh();
                initiator.Viewport.Items.Add(mesh);
                await initiator.WarmAsync();
                var device = Device(initiator);
                var before = initiator.Counts;
                var ready = new TaskCompletionSource<SilkD3D12Device>(TaskCreationOptions.RunContinuationsAsynchronously);
                var peerTask = StaThread.RunAsync(async () => {
                        try {
                            using var peerEffects = new DefaultEffectsManager();
                            await using var peer = new P4PresentationDriver(hwnd, peerEffects);
                            using var firstMesh = P4PresentationDriver.CreateMesh();
                            using var secondMesh = P4PresentationDriver.CreateMesh();
                            peer.Viewport.Items.Add(firstMesh);
                            if (failRetry) peer.Viewport.Items.Add(secondMesh);
                            await peer.WarmAsync();
                            await SuspendAsync(peer);
                            await AssertQueuedFailureAsync(
                                peer, [firstMesh.SceneNode, secondMesh.SceneNode], failRetry,
                                () => ready.SetResult(Device(peer))
                            );
                            Assert.False(peerEffects.IsDisposed);
                            Assert.False(firstMesh.SceneNode.IsDisposed);
                            await AssertFreshPixelsAsync(hwnd, peerEffects);
                        } catch (Exception exception) {
                            ready.TrySetException(exception);
                            throw;
                        }
                    }
                );
                try {
                    var peerDevice = await ready.Task.WaitAsync(P4PresentationDriver.Timeout);
                    Assert.Equal(device.NativePointer, peerDevice.NativePointer);
                    RemoveDevice(device);
                    Assert.Equal(0, initiator.Tick());
                    Assert.Equal(before, initiator.Counts);
                    await peerTask;
                    Assert.True(device.IsDisposed);
                    Assert.True(peerDevice.IsDisposed);
                    await initiator.DrainAsync();
                    P4PresentationDriver.AssertPixel(await initiator.CaptureAsync(), 32, 24, 0, 255, 0, 255);
                } finally {
                    await peerTask;
                }
            }
        );

    /// <summary>Contains two teardown failures after actual removal inside a productive CPU scene update.</summary>
    /// <param name="hwnd">Whether to exercise HWND deferred retirement instead of WPF's automatic rendering boundary.</param>
    /// <returns>The canceled recording, bounded automatic failure and successful explicit ownership retry verification.</returns>
    [Theory(Explicit = true)]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task CpuBorrowRemovalContainsDeferredTeardownFailures(bool hwnd) => StaThread.RunAsync(async () => {
            using var effects = new DefaultEffectsManager();
            await using var driver = new P4PresentationDriver(hwnd, effects);
            using var firstMesh = P4PresentationDriver.CreateMesh();
            using var secondMesh = P4PresentationDriver.CreateMesh();
            driver.Viewport.Items.Add(firstMesh);
            driver.Viewport.Items.Add(secondMesh);
            await driver.WarmAsync();
            var first = Assert.IsType<P4ObservedMeshNode>(firstMesh.SceneNode);
            var second = Assert.IsType<P4ObservedMeshNode>(secondMesh.SceneNode);
            var owner = (object?) driver.Wpf ?? driver.Native!;
            var device = Device(driver);
            var commands = P4PresentationDriver.ReadField<SilkD3D12CommandContext>(
                owner, hwnd
                    ? "commandContext"
                    : "commands"
            );
            var registration = P4PresentationDriver.ReadField<D3D12PresentationDeviceRecovery>(owner, "recoveryRegistration");
            var context = driver.Context!;
            var before = driver.Counts;
            var callbackReturned = false;
            EventHandler detachedAfterCallback = (_, _) => Assert.True(callbackReturned);
            first.Detached += detachedAfterCallback;
            second.Detached += detachedAfterCallback;
            first.OnNextUpdate = current => {
                Assert.Same(context, current);
                RemoveDevice(device);
                if (hwnd) Assert.False(driver.Native!.CompletePendingFrame());
                else Assert.True(driver.Wpf!.HasPendingGpuWork);
                Assert.True(registration.IsRemovalRequested);
                Assert.False(registration.CanRecreate);
                Assert.False(device.IsDisposed);
                Assert.False(commands.IsDisposed);
                Assert.NotNull(current.Camera);
                Assert.True(first.RenderCore.IsAttached);
                Assert.True(second.RenderCore.IsAttached);
                Assert.Same(
                    registration,
                    P4PresentationDriver.ReadField<D3D12PresentationDeviceRecovery>(owner, "recoveryRegistration")
                );
                Assert.Equal(before, driver.Counts);
                callbackReturned = true;
            };
            try {
                await AssertQueuedFailureAsync(
                    driver, [first, second], true, () => {
                        if (hwnd) {
                            driver.Viewport.InvalidateRender();
                            Assert.Equal(0, driver.Tick());
                            Assert.True(callbackReturned);
                            Assert.False(device.IsDisposed);
                            Assert.Equal(
                                DispatcherOperationStatus.Pending,
                                P4PresentationDriver.ReadField<DispatcherOperation>(driver.Native!, "retirementOperation")
                                    .Status
                            );
                        } else {
                            var automaticTick = typeof(D3D12WpfPresentation)
                                .GetMethod("OnRendering", BindingFlags.Instance | BindingFlags.NonPublic)!
                                .CreateDelegate<EventHandler<RenderingEventArgs>>(driver.Wpf!);
                            driver.Viewport.StartRendering();
                            driver.Viewport.InvalidateRender();
                            automaticTick(driver.Canvas, CreateRenderingArgs(TimeSpan.FromSeconds(2)));
                        }

                        Assert.True(callbackReturned);
                        Assert.Equal(before, driver.Counts);
                    }, () => {
                        Assert.True(callbackReturned);
                        Assert.False(device.IsDisposed);
                        Assert.False(commands.IsDisposed);
                        Assert.NotNull(context.Camera);
                        Assert.Same(
                            registration,
                            P4PresentationDriver.ReadField<D3D12PresentationDeviceRecovery>(owner, "recoveryRegistration")
                        );
                        Assert.False(registration.CanRecreate);
                        Assert.Equal(before, driver.Counts);
                        if (hwnd)
                            Assert.False(
                                P4PresentationDriver.ReadField<D3D12FrameSubmission>(driver.Native!, "_frameSubmission")
                                    .IsPending
                            );
                    }
                );
                Assert.True(callbackReturned);
                Assert.True(commands.IsDisposed);
                Assert.False(effects.IsDisposed);
                Assert.False(first.IsDisposed);
                Assert.False(second.IsDisposed);
                await AssertFreshPixelsAsync(hwnd, effects);
            } finally {
                first.OnNextUpdate = null;
                first.Detached -= detachedAfterCallback;
                second.Detached -= detachedAfterCallback;
            }
        }
    );

    /// <summary>Contains two teardown failures when actual native removal precedes an automatic HWND composition tick.</summary>
    /// <returns>The genuine automatic entry, retained failed generation and successful explicit retry qualification.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task HwndAutomaticTickContainsRemovalBetweenFramesTeardownFailures() => StaThread.RunAsync(async () => {
            using var effects = new DefaultEffectsManager();
            await using var driver = new P4PresentationDriver(true, effects);
            using var firstMesh = P4PresentationDriver.CreateMesh();
            using var secondMesh = P4PresentationDriver.CreateMesh();
            driver.Viewport.Items.Add(firstMesh);
            driver.Viewport.Items.Add(secondMesh);
            await driver.WarmAsync();
            var surface = driver.Native!;
            var device = Device(driver);
            var commands = P4PresentationDriver.ReadField<SilkD3D12CommandContext>(surface, "commandContext");
            var registration = P4PresentationDriver.ReadField<D3D12PresentationDeviceRecovery>(surface, "recoveryRegistration");
            var context = driver.Context!;
            var before = driver.Counts;
            var automaticTick = typeof(Viewport3DX)
                .GetMethod("D3D12CompositionTargetRendering", BindingFlags.Instance | BindingFlags.NonPublic)!
                .CreateDelegate<EventHandler<RenderingEventArgs>>(driver.Viewport);
            await AssertQueuedFailureAsync(
                driver, [firstMesh.SceneNode, secondMesh.SceneNode], true, () => {
                    driver.Viewport.StartRendering();
                    driver.Viewport.InvalidateRender();
                    RemoveDevice(device);
                    Assert.Equal(before, driver.Counts);
                    automaticTick(surface, CreateRenderingArgs(TimeSpan.FromSeconds(2)));
                    Assert.Equal(before, driver.Counts);
                }, () => {
                    Assert.False(device.IsDisposed);
                    Assert.False(commands.IsDisposed);
                    Assert.NotNull(context.Camera);
                    Assert.Same(registration,
                        P4PresentationDriver.ReadField<D3D12PresentationDeviceRecovery>(surface, "recoveryRegistration"));
                    Assert.True(registration.IsRemovalRequested);
                    Assert.False(registration.CanRecreate);
                    Assert.Null(P4PresentationDriver.ReadField<DispatcherOperation?>(surface, "retirementOperation"));
                    Assert.False(P4PresentationDriver.ReadField<D3D12FrameSubmission>(surface, "_frameSubmission").IsPending);
                    Assert.Equal(before, driver.Counts);
                }
            );
            Assert.True(commands.IsDisposed);
            Assert.False(effects.IsDisposed);
            Assert.False(firstMesh.SceneNode.IsDisposed);
            Assert.False(secondMesh.SceneNode.IsDisposed);
            await AssertFreshPixelsAsync(true, effects);
        }
    );

    /// <summary>Observes real queued detachment failures, ensuring the existing boundary sees retained native ownership.</summary>
    /// <param name="driver">The productive owner whose queued or automatic retirement is under test.</param>
    /// <param name="nodes">Real productive nodes whose one-shot detach callbacks may fail.</param>
    /// <param name="failRetry">Whether the second node fails during the boundary's immediate retry.</param>
    /// <param name="begin">Requests discovery, exposes a peer, or renders the frame that must retire after its CPU callback.</param>
    /// <param name="inspectRetainedOwnership">The optional check after another dispatcher turn, before explicit retry.</param>
    /// <returns>The completed boundary notification and verified, explicit native ownership retry.</returns>
    private static async Task AssertQueuedFailureAsync(
        P4PresentationDriver driver,
        SceneNode[] nodes,
        bool failRetry,
        Action begin,
        Action? inspectRetainedOwnership = null
    ) {
        var owner = (object?) driver.Wpf ?? driver.Native!;
        var device = Device(driver);
        var context = driver.Context!;
        var registration = P4PresentationDriver.ReadField<D3D12PresentationDeviceRecovery>(owner, "recoveryRegistration");
        var before = driver.Counts;
        var firstFailure = new InvalidOperationException("One-shot queued recovery detachment failure.");
        var retryFailure = new InvalidOperationException("One-shot recovery boundary teardown failure.");
        var notified = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Exception? escaped = null;
        var errorCount = 0;
        var detachFailures = 0;
        EventHandler? detach = null;
        detach = (sender, _) => {
            Assert.IsAssignableFrom<SceneNode>(sender)
                .Detached -= detach;
            throw detachFailures++ == 0
                ? firstFailure
                : retryFailure;
        };
        EventHandler<RelayExceptionEventArgs> renderError = (sender, args) => {
            errorCount++;
            Assert.Same((object?) driver.Canvas ?? driver.Native, sender);
            Assert.Same(firstFailure, args.Exception);
            Assert.False(device.IsDisposed);
            Assert.NotNull(context.Camera);
            Assert.Same(
                registration,
                P4PresentationDriver.ReadField<D3D12PresentationDeviceRecovery>(owner, "recoveryRegistration")
            );
            Assert.True(registration.IsRemovalRequested);
            Assert.False(registration.CanRecreate);
            Assert.Equal(before, driver.Counts);
            args.Handled = true;
            notified.TrySetResult();
        };
        DispatcherUnhandledExceptionEventHandler unhandled = (_, args) => {
            escaped = args.Exception;
            args.Handled = true;
            notified.TrySetResult();
        };
        nodes[0].Detached += detach;
        if (failRetry) nodes[1].Detached += detach;
        driver.Viewport.RenderExceptionOccurred += renderError;
        var dispatcher = Dispatcher.CurrentDispatcher;
        dispatcher.UnhandledException += unhandled;
        try {
            begin();
            await notified.Task.WaitAsync(P4PresentationDriver.Timeout);
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.Null(escaped);
            Assert.Equal(1, errorCount);
            Assert.Equal(
                failRetry
                    ? 2
                    : 1, detachFailures
            );
            Assert.Equal(before, driver.Counts);
            if (failRetry) {
                Assert.Same(retryFailure, driver.Viewport.RenderException);
                Assert.False(device.IsDisposed);
                Assert.Same(
                    registration,
                    P4PresentationDriver.ReadField<D3D12PresentationDeviceRecovery>(owner, "recoveryRegistration")
                );
                Assert.False(registration.CanRecreate);
                if (inspectRetainedOwnership is not null) {
                    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                    inspectRetainedOwnership();
                    Assert.Null(escaped);
                    Assert.Equal(1, errorCount);
                    Assert.Equal(2, detachFailures);
                }

                if (driver.Wpf is { } wpf) {
                    Assert.False(
                        P4PresentationDriver.ReadField<DispatcherTimer>(wpf, "serviceTimer")
                            .IsEnabled
                    );
                    wpf.Dispose();
                } else {
                    Assert.Null(P4PresentationDriver.ReadField<DispatcherOperation?>(driver.Native!, "retirementOperation"));
                    driver.Native!.Dispose();
                }
            }

            Assert.True(device.IsDisposed);
            Assert.Null(context.Camera);
            Assert.Equal(before, driver.Counts);
            await P4PresentationDriver.PollAsync(() => registration.CanRecreate, "old queued recovery owners released");
            Assert.Null(escaped);
        } finally {
            foreach (var node in nodes) node.Detached -= detach;
            driver.Viewport.RenderExceptionOccurred -= renderError;
            dispatcher.UnhandledException -= unhandled;
        }
    }

    /// <summary>Settles actual hidden WPF transport, including detachment and service timer shutdown.</summary>
    /// <param name="driver">The initialized native or composited owner.</param>
    /// <returns>The inactive native state without a synthetic completion or image binding.</returns>
    private static async Task SuspendAsync(P4PresentationDriver driver) {
        driver.Viewport.Visibility = Visibility.Hidden;
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        if (driver.Wpf is not { } wpf) return;
        await driver.FinishTransportAsync();
        await P4PresentationDriver.PollAsync(
            () => {
                _ = wpf.RenderFrame(TimeSpan.Zero, automaticFrame: true);
                return driver.Canvas!.ImageSource.BoundSurface == 0 &&
                       !P4PresentationDriver.ReadField<DispatcherTimer>(wpf, "serviceTimer")
                           .IsEnabled;
            }, "actual suspended image detachment and timer stop"
        );
    }

    /// <summary>Checks a fresh productive viewport after all failed-generation owners released the native singleton.</summary>
    /// <param name="hwnd">Whether to select HWND instead of WPF composition.</param>
    /// <param name="effects">The still-live, application-owned manager.</param>
    /// <returns>The new productive capture with actual green center pixels.</returns>
    private static async Task AssertFreshPixelsAsync(bool hwnd, IEffectsManager effects) {
        await using var fresh = new P4PresentationDriver(hwnd, effects);
        using var mesh = P4PresentationDriver.CreateMesh();
        fresh.Viewport.Items.Add(mesh);
        await fresh.WarmAsync();
        P4PresentationDriver.AssertPixel(await fresh.CaptureAsync(), 32, 24, 0, 255, 0, 255);
    }

    /// <summary>Reads the device retained by the actual productive presentation owner.</summary>
    /// <param name="driver">The initialized viewport driver.</param>
    /// <returns>The borrowed native device wrapper.</returns>
    private static SilkD3D12Device Device(P4PresentationDriver driver) =>
        P4PresentationDriver.ReadField<SilkD3D12Device>((object?) driver.Wpf ?? driver.Native!, "device");

    /// <summary>Creates ordinary WPF frame arguments using the framework constructor used by existing automatic-callback tests.</summary>
    /// <param name="timestamp">The distinct rendering timestamp.</param>
    /// <returns>The actual framework rendering arguments.</returns>
    private static RenderingEventArgs CreateRenderingArgs(TimeSpan timestamp) =>
        (RenderingEventArgs) Activator.CreateInstance(
            typeof(RenderingEventArgs),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, [timestamp], null
        )!;

    /// <summary>Invokes actual process-device removal while balancing the queried extended COM reference.</summary>
    /// <param name="device">The productive device whose native singleton is removed.</param>
    private static void RemoveDevice(SilkD3D12Device device) {
        var id = ID3D12Device5.Guid;
        Marshal.ThrowExceptionForHR(Marshal.QueryInterface(device.NativePointer, in id, out var extended));
        try {
            var method = Marshal.ReadIntPtr(Marshal.ReadIntPtr(extended), 58 * nint.Size);
            Marshal.GetDelegateForFunctionPointer<RemoveNativeDevice>(method)(extended);
        } finally {
            Marshal.Release(extended);
        }

        Assert.True(device.GetDeviceRemovedReason() < 0);
    }

    /// <summary>Matches the pinned native ID3D12Device5.RemoveDevice signature.</summary>
    /// <param name="device">The queried extended device interface.</param>
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void RemoveNativeDevice(nint device);
}

using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Utilities;
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
    });

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
            await AssertQueuedFailureAsync(driver, [firstMesh.SceneNode, secondMesh.SceneNode], failRetry, () => {
                RemoveDevice(device);
                D3D12PresentationDeviceRecovery.ProbeForRemoval();
            });
            Assert.False(effects.IsDisposed);
            Assert.False(firstMesh.SceneNode.IsDisposed);
            await AssertFreshPixelsAsync(hwnd, effects);
        });

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
                    await AssertQueuedFailureAsync(peer, [firstMesh.SceneNode, secondMesh.SceneNode], failRetry,
                        () => ready.SetResult(Device(peer)));
                    Assert.False(peerEffects.IsDisposed);
                    Assert.False(firstMesh.SceneNode.IsDisposed);
                    await AssertFreshPixelsAsync(hwnd, peerEffects);
                } catch (Exception exception) {
                    ready.TrySetException(exception);
                    throw;
                }
            });
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
        });

    /// <summary>Observes real queued detachment failures, ensuring the existing boundary sees retained native ownership.</summary>
    /// <param name="driver">The initialized and inactive presentation owner.</param>
    /// <param name="nodes">Real productive nodes whose one-shot detach callbacks may fail.</param>
    /// <param name="failRetry">Whether the second node fails during the boundary's immediate retry.</param>
    /// <param name="begin">Requests a queued probe or exposes the ready peer to its removal initiator.</param>
    /// <returns>The completed boundary notification and verified, explicit native ownership retry.</returns>
    private static async Task AssertQueuedFailureAsync(
        P4PresentationDriver driver, SceneNode[] nodes, bool failRetry, Action begin
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
            Assert.IsAssignableFrom<SceneNode>(sender).Detached -= detach;
            throw detachFailures++ == 0 ? firstFailure : retryFailure;
        };
        EventHandler<RelayExceptionEventArgs> renderError = (sender, args) => {
            errorCount++;
            Assert.Same((object?) driver.Canvas ?? driver.Native, sender);
            Assert.Same(firstFailure, args.Exception);
            Assert.False(device.IsDisposed);
            Assert.NotNull(context.Camera);
            Assert.Same(registration,
                P4PresentationDriver.ReadField<D3D12PresentationDeviceRecovery>(owner, "recoveryRegistration"));
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
            Assert.Equal(failRetry ? 2 : 1, detachFailures);
            Assert.Equal(before, driver.Counts);
            if (failRetry) {
                Assert.Same(retryFailure, driver.Viewport.RenderException);
                Assert.False(device.IsDisposed);
                Assert.Same(registration,
                    P4PresentationDriver.ReadField<D3D12PresentationDeviceRecovery>(owner, "recoveryRegistration"));
                Assert.False(registration.CanRecreate);
                if (driver.Wpf is { } wpf) {
                    Assert.False(P4PresentationDriver.ReadField<DispatcherTimer>(wpf, "serviceTimer").IsEnabled);
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
        await P4PresentationDriver.PollAsync(() => {
            _ = wpf.RenderFrame(TimeSpan.Zero, automaticFrame: true);
            return driver.Canvas!.ImageSource.BoundSurface == 0 &&
                   !P4PresentationDriver.ReadField<DispatcherTimer>(wpf, "serviceTimer").IsEnabled;
        }, "actual suspended image detachment and timer stop");
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

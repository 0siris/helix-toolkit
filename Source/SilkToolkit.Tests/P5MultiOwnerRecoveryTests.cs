using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.Wpf.SharpDX.Controls;
using Silk.NET.Direct3D12;
using Xunit;

namespace SilkToolkit.Tests;

/// <summary>Exercises actual process-device removal through normal polling and multiple productive owners.</summary>
/// <remarks>ID3D12Device5.RemoveDevice invalidates the process singleton, without resetting the physical adapter.</remarks>
/// <param name="output">Receives the exact native identities and explicitly scoped recovery evidence.</param>
[Collection(WpfCollection.Name)]
public sealed class P5MultiOwnerRecoveryTests(ITestOutputHelper output) {
    /// <summary>Checks exact pointer identity, callback coalescing, unlocked notification and retained peer barriers.</summary>
    /// <returns>The dispatcher-owned registry lifecycle check, requiring no GPU.</returns>
    [Fact]
    public Task RegistryTracksNativeGenerationsAndCallsOwnersOutsideItsMonitor() => StaThread.RunAsync(() => {
        var gate = typeof(D3D12PresentationDeviceRecovery).GetField("Gate", BindingFlags.Static | BindingFlags.NonPublic)!
            .GetValue(null)!;
        var calls = 0;
        Action retire = () => {
            Assert.False(Monitor.IsEntered(gate));
            calls++;
        };
        var first = new D3D12PresentationDeviceRecovery(101, Dispatcher.CurrentDispatcher, retire);
        var second = new D3D12PresentationDeviceRecovery(101, Dispatcher.CurrentDispatcher, retire);
        var unrelated = new D3D12PresentationDeviceRecovery(202, Dispatcher.CurrentDispatcher, retire);
        try {
            first.RequestRemoval();
            Assert.Equal(2, calls);
            Assert.True(first.IsRemovalRequested);
            Assert.True(second.IsRemovalRequested);
            Assert.False(unrelated.IsRemovalRequested);
            second.RequestRemoval();
            Assert.Equal(2, calls);
            first.Unregister();
            Assert.False(first.CanRecreate);
            second.Unregister();
            Assert.True(first.CanRecreate);
            // Native allocator address reuse by a healthy generation must not revive an old removal barrier.
            var replacement = new D3D12PresentationDeviceRecovery(101, Dispatcher.CurrentDispatcher, retire);
            try {
                Assert.True(first.CanRecreate);
                Assert.False(replacement.IsRemovalRequested);
            } finally {
                replacement.Unregister();
            }
        } finally {
            first.Unregister();
            second.Unregister();
            unrelated.Unregister();
            GC.KeepAlive(retire);
        }
    });

    /// <summary>Discovers removal through normal rendering, including a device with no pending submission.</summary>
    /// <param name="hwnd">Whether to select the HWND transport.</param>
    /// <param name="pending">Whether an actual native queue gate retains an unfinished frame at removal.</param>
    /// <returns>The bounded retirement, abandoned-frame accounting and fresh pixel check.</returns>
    [Theory(Explicit = true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task NormalRenderRecoversRemovedNativeDevice(bool hwnd, bool pending) => StaThread.RunAsync(async () => {
        using var effects = new DefaultEffectsManager();
        await using var driver = new P4PresentationDriver(hwnd, effects);
        using var mesh = P4PresentationDriver.CreateMesh();
        driver.Viewport.Items.Add(mesh);
        await driver.WarmAsync();
        var oldDevice = Device(driver);
        var oldContext = driver.Context!;
        var before = driver.Counts;
        var gate = pending ? new P4NativeQueueGate(driver) : null;
        try {
            if (pending) {
                driver.Viewport.InvalidateRender();
                Assert.True(driver.Tick() > 0);
                Assert.Equal(before.Completed, driver.Completed);
            }

            RemoveDevice(oldDevice);
            // No direct RetireSceneResources call: use the productive frame/completion boundary.
            if (hwnd && pending) Assert.False(driver.Native!.CompletePendingFrame());
            else Assert.Equal(0, driver.Tick());
            Assert.True(oldDevice.IsDisposed);
            Assert.Null(oldContext.Camera);
            Assert.Equal(before.Completed, driver.Completed);
            Assert.Equal(before.Rendered, driver.RenderedCount);
            Assert.Equal(before.OnRendered, driver.OnRenderedCount);
        } finally {
            // Test-only gate fences also retain the native singleton; release them before recreation.
            gate?.Dispose();
        }

        await driver.DrainAsync();
        Assert.NotSame(oldContext, driver.Context);
        Assert.Equal(driver.Completed + (pending ? 1UL : 0UL), driver.Submitted);
        P4PresentationDriver.AssertPixel(await driver.CaptureAsync(), 32, 24, 0, 255, 0, 255);
        Assert.False(effects.IsDisposed);
        output.WriteLine($"Normal {(hwnd ? "HWND" : "WPF")} recovery; pending={pending}; fresh green pixels.");
    });

    /// <summary>Discovers confirmed removal during the public effects-manager transition with a real unfinished submission.</summary>
    /// <param name="hwnd">Whether to select the HWND transport.</param>
    /// <returns>The manager lifecycle retirement, abandoned completion and replacement-generation output check.</returns>
    [Theory(Explicit = true)]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task ManagerChangeRecoversRemovedPendingGeneration(bool hwnd) => StaThread.RunAsync(async () => {
        using var original = new DefaultEffectsManager();
        using var replacement = new DefaultEffectsManager();
        await using var driver = new P4PresentationDriver(hwnd, original);
        using var mesh = P4PresentationDriver.CreateMesh();
        driver.Viewport.Items.Add(mesh);
        await driver.WarmAsync();
        var device = Device(driver);
        var before = driver.Counts;
        using (var gate = new P4NativeQueueGate(driver)) {
            driver.Viewport.InvalidateRender();
            Assert.True(driver.Tick() > 0);
            RemoveDevice(device);
            driver.Viewport.EffectsManager = replacement;
            Assert.True(device.IsDisposed);
            Assert.Equal(before.Completed, driver.Completed);
            Assert.Equal(before.Rendered, driver.RenderedCount);
        }

        await driver.DrainAsync();
        Assert.Same(replacement, driver.Viewport.EffectsManager);
        Assert.Equal(driver.Completed + 1, driver.Submitted);
        P4PresentationDriver.AssertPixel(await driver.CaptureAsync(), 32, 24, 0, 255, 0, 255);
        Assert.False(original.IsDisposed);
        Assert.False(replacement.IsDisposed);
    });

    /// <summary>Preserves a real unclaimed completion when no-pending completion polling discovers device removal.</summary>
    /// <returns>The actual HWND fence completion and public event accounting check.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task HwndCompletionPollingKeepsAlreadyCompletedUnclaimedFrame() => StaThread.RunAsync(async () => {
        using var effects = new DefaultEffectsManager();
        await using var driver = new P4PresentationDriver(true, effects);
        await driver.WarmAsync();
        var before = driver.Counts;
        driver.Native!.RenderOnce([0, 1, 0, 1]);
        Assert.Equal(before.Completed + 1, driver.Completed);
        Assert.Equal(before.Rendered, driver.RenderedCount);
        var device = Device(driver);
        RemoveDevice(device);
        Assert.False(driver.Native.CompletePendingFrame());
        Assert.True(device.IsDisposed);
        Assert.Equal(before.Rendered + 1, driver.RenderedCount);
        Assert.Equal(before.OnRendered + 1, driver.OnRenderedCount);
        Assert.False(driver.Native.TryTakeCompletedFrame());
        await driver.DrainAsync();
        Assert.Equal(driver.Submitted, driver.Completed);
    });

    /// <summary>Retries a removed singleton retained by an external COM owner without releasing that owner's reference.</summary>
    /// <param name="hwnd">Whether to select HWND rather than WPF composition.</param>
    /// <returns>The explicit-adapter creation retry and fresh output check after external release.</returns>
    [Theory(Explicit = true)]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task ExternalNativeReferenceRetainsRemovalUntilItsOwnerReleasesIt(bool hwnd) => StaThread.RunAsync(async () => {
        using var effects = new DefaultEffectsManager(new EffectsManagerConfiguration { AdapterIndex = 0 });
        await using var driver = new P4PresentationDriver(hwnd, effects);
        using var mesh = P4PresentationDriver.CreateMesh();
        driver.Viewport.Items.Add(mesh);
        await driver.WarmAsync();
        var device = Device(driver);
        var external = device.NativePointer;
        _ = Marshal.AddRef(external);
        try {
            RemoveDevice(device);
            Assert.Equal(0, driver.Tick());
            Assert.True(device.IsDisposed);
            for (var attempt = 0; attempt < 3; attempt++) Assert.Equal(0, driver.Tick());
            Assert.True(driver.Viewport.RenderDemand.HasRecoveryDemand);
            var id = ID3D12Device5.Guid;
            Marshal.ThrowExceptionForHR(Marshal.QueryInterface(external, in id, out var retained));
            Marshal.Release(retained);
        } finally {
            Marshal.Release(external);
        }

        await driver.DrainAsync();
        P4PresentationDriver.AssertPixel(await driver.CaptureAsync(), 32, 24, 0, 255, 0, 255);
        Assert.False(effects.IsDisposed);
    });

    /// <summary>Cancels generation-null reentry from a peer's real completion while the detecting owner still retains removal.</summary>
    /// <returns>The real completion callback, retained singleton barrier and subsequent fresh pixel check.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task PeerRetirementCompletionCannotRecreateWhileInitiatorStillOwnsRemovedSingleton() =>
        StaThread.RunAsync(async () => {
            using var effects = new DefaultEffectsManager();
            using var peerEffects = new DefaultEffectsManager();
            await using var active = new P4PresentationDriver(false, effects);
            await using var peer = new P4PresentationDriver(true, peerEffects);
            using var mesh = P4PresentationDriver.CreateMesh();
            active.Viewport.Items.Add(mesh);
            await active.WarmAsync();
            await peer.WarmAsync();
            var device = Device(active);
            var peerDevice = Device(peer);
            Assert.Equal(device.NativePointer, peerDevice.NativePointer);
            var before = peer.Counts;
            peer.Native!.RenderOnce([0, 1, 0, 1]);
            Assert.Equal(before.Completed + 1, peer.Completed);
            Assert.Equal(before.Rendered, peer.RenderedCount);
            var calls = 0;
            EventHandler completion = (_, _) => {
                calls++;
                Assert.True(peerDevice.IsDisposed);
                Assert.False(device.IsDisposed);
                Assert.False(peer.Native.IsInitialized);
                Assert.Equal(0, peer.Tick());
                Assert.False(peer.Native.IsInitialized);
                Assert.True(peer.Viewport.RenderDemand.HasRecoveryDemand);
            };
            peer.Viewport.Rendered += completion;
            try {
                RemoveDevice(device);
                Assert.Equal(0, active.Tick());
                Assert.Equal(1, calls);
                Assert.True(device.IsDisposed);
                Assert.Equal(before.Rendered + 1, peer.RenderedCount);
            } finally {
                peer.Viewport.Rendered -= completion;
            }

            await peer.DrainAsync();
            await active.DrainAsync();
            P4PresentationDriver.AssertPixel(await active.CaptureAsync(), 32, 24, 0, 255, 0, 255);
        });

    /// <summary>Retires an inactive WPF peer on the same actual singleton despite different manager ownership.</summary>
    /// <param name="activeHwnd">Whether the detecting owner uses HWND, producing a mixed-backend pair.</param>
    /// <param name="unloadPeer">Whether the peer is genuinely unloaded instead of hidden.</param>
    /// <returns>The productive multi-owner retirement and recovered output qualification.</returns>
    [Theory(Explicit = true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task RemovedSingletonRetiresInactivePeerWithDifferentManager(bool activeHwnd, bool unloadPeer) =>
        StaThread.RunAsync(async () => {
            using var activeEffects = new DefaultEffectsManager();
            using var peerEffects = new DefaultEffectsManager();
            await using var active = new P4PresentationDriver(activeHwnd, activeEffects);
            await using var peer = new P4PresentationDriver(false, peerEffects);
            using var activeMesh = P4PresentationDriver.CreateMesh();
            using var peerMesh = P4PresentationDriver.CreateMesh();
            active.Viewport.Items.Add(activeMesh);
            peer.Viewport.Items.Add(peerMesh);
            await active.WarmAsync();
            await peer.WarmAsync();
            var activeDevice = Device(active);
            var peerDevice = Device(peer);
            var pointer = activeDevice.NativePointer;
            Assert.Equal(pointer, peerDevice.NativePointer);
            Assert.NotSame(activeEffects, peerEffects);
            var peerContext = peer.Context!;
            var peerRegistration = Registration(peer);
            if (unloadPeer) peer.Root.Children.Remove(peer.Viewport);
            else peer.Viewport.Visibility = Visibility.Hidden;
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            await peer.FinishTransportAsync();
            Assert.False(peer.Canvas!.IsActive);
            Assert.True(peer.Wpf!.IsInitialized);
            Assert.False(P4PresentationDriver.ReadField<DispatcherTimer>(peer.Wpf, "serviceTimer").IsEnabled);
            Assert.False(peerDevice.IsDisposed);
            var before = active.Counts;
            using (var gate = new P4NativeQueueGate(active)) {
                active.Viewport.InvalidateRender();
                Assert.True(active.Tick() > 0);
                RemoveDevice(activeDevice);
                Assert.Equal(0, active.Tick());
                Assert.Equal(before.Completed, active.Completed);
            }

            await P4PresentationDriver.PollAsync(
                () => activeDevice.IsDisposed && peerDevice.IsDisposed, "all old productive native owners retired");
            Assert.True(peerRegistration.CanRecreate);
            Assert.Null(peerContext.Camera);
            Assert.Equal(0, peer.Canvas.ImageSource.BoundSurface);
            Assert.False(peer.Wpf.IsInitialized);
            await active.DrainAsync();
            Assert.Equal(active.Completed + 1, active.Submitted);
            P4PresentationDriver.AssertPixel(await active.CaptureAsync(), 32, 24, 0, 255, 0, 255);
            if (unloadPeer) peer.Root.Children.Add(peer.Viewport);
            else peer.Viewport.Visibility = Visibility.Visible;
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            await peer.DrainAsync();
            Assert.NotSame(peerContext, peer.Context);
            P4PresentationDriver.AssertPixel(await peer.CaptureAsync(), 32, 24, 0, 255, 0, 255);
            Assert.False(activeEffects.IsDisposed);
            Assert.False(peerEffects.IsDisposed);
            output.WriteLine($"Actual shared native pointer=0x{pointer:X}; active HWND={activeHwnd}; " +
                $"peer unloaded={unloadPeer}; both recovered fresh pixels; physical adapter unchanged.");
        });

    /// <summary>Retains an active CPU borrow when a nested peer render discovers process-device removal.</summary>
    /// <param name="hwnd">Whether the active CPU borrower uses HWND.</param>
    /// <returns>The reentrant cancellation, deferred retirement and fresh-generation check.</returns>
    [Theory(Explicit = true)]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task NestedPeerRecoveryDoesNotReleaseBorrowedCpuContext(bool hwnd) => StaThread.RunAsync(async () => {
        using var effects = new DefaultEffectsManager();
        using var peerEffects = new DefaultEffectsManager();
        await using var active = new P4PresentationDriver(hwnd, effects);
        await using var peer = new P4PresentationDriver(!hwnd, peerEffects);
        using var mesh = P4PresentationDriver.CreateMesh();
        active.Viewport.Items.Add(mesh);
        await active.WarmAsync();
        await peer.WarmAsync();
        var oldDevice = Device(active);
        var peerDevice = Device(peer);
        Assert.Equal(oldDevice.NativePointer, peerDevice.NativePointer);
        var oldContext = active.Context!;
        var before = active.Counts;
        var callbackRan = false;
        Assert.IsType<P4ObservedMeshNode>(mesh.SceneNode).OnNextUpdate = context => {
            callbackRan = true;
            Assert.Same(oldContext, context);
            RemoveDevice(oldDevice);
            Assert.Equal(0, peer.Tick());
            Assert.True(Registration(active).IsRemovalRequested);
            Assert.False(oldDevice.IsDisposed);
            Assert.NotNull(context.Camera);
            Assert.True(peerDevice.IsDisposed);
            // The nested caller must retain recovery demand and return while the CPU borrower still owns the singleton.
            Assert.Equal(0, peer.Tick());
            Assert.True(peer.Viewport.RenderDemand.HasRecoveryDemand);
            Assert.False(oldDevice.IsDisposed);
        };
        active.Viewport.InvalidateRender();
        Assert.Equal(0, active.Tick());
        Assert.True(callbackRan);
        Assert.Equal(before, active.Counts);
        await P4PresentationDriver.PollAsync(() => oldDevice.IsDisposed, "CPU borrower retirement after finally");
        Assert.Null(oldContext.Camera);
        await active.DrainAsync();
        await peer.DrainAsync();
        P4PresentationDriver.AssertPixel(await active.CaptureAsync(), 32, 24, 0, 255, 0, 255);
    });

    /// <summary>Posts retirement to a genuinely different dispatcher without blocking the detecting UI thread.</summary>
    /// <returns>The two-dispatcher, different-manager native singleton recovery check.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task SuspendedPeerOnAnotherDispatcherRetiresWithoutAFrameCallback() => StaThread.RunAsync(async () => {
        using var effects = new DefaultEffectsManager();
        await using var active = new P4PresentationDriver(false, effects);
        using var mesh = P4PresentationDriver.CreateMesh();
        active.Viewport.Items.Add(mesh);
        await active.WarmAsync();
        var ready = new TaskCompletionSource<SilkD3D12Device>(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var peerTask = StaThread.RunAsync(async () => {
            try {
                using var peerEffects = new DefaultEffectsManager();
                await using var peer = new P4PresentationDriver(false, peerEffects);
                using var peerMesh = P4PresentationDriver.CreateMesh();
                peer.Viewport.Items.Add(peerMesh);
                await peer.WarmAsync();
                var peerDevice = Device(peer);
                peer.Root.Children.Remove(peer.Viewport);
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                await peer.FinishTransportAsync();
                Assert.False(P4PresentationDriver.ReadField<DispatcherTimer>(peer.Wpf!, "serviceTimer").IsEnabled);
                ready.SetResult(peerDevice);
                await finish.Task;
                Assert.True(peerDevice.IsDisposed);
                peer.Root.Children.Add(peer.Viewport);
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                await peer.DrainAsync();
                P4PresentationDriver.AssertPixel(await peer.CaptureAsync(), 32, 24, 0, 255, 0, 255);
            } catch (Exception exception) {
                ready.TrySetException(exception);
                throw;
            }
        });
        try {
            var peerDevice = await ready.Task.WaitAsync(P4PresentationDriver.Timeout);
            var device = Device(active);
            Assert.Equal(device.NativePointer, peerDevice.NativePointer);
            RemoveDevice(device);
            Assert.Equal(0, active.Tick());
            Assert.True(active.Viewport.RenderDemand.HasRecoveryDemand);
            await P4PresentationDriver.PollAsync(
                () => device.IsDisposed && peerDevice.IsDisposed, "nonblocking cross-dispatcher retirement");
            await active.DrainAsync();
            P4PresentationDriver.AssertPixel(await active.CaptureAsync(), 32, 24, 0, 255, 0, 255);
        } finally {
            finish.TrySetResult();
            await peerTask;
        }
    });

    /// <summary>Checks that settled healthy ticks neither allocate nor replace their generation registration.</summary>
    /// <returns>The warmed strict heap and registry membership comparison.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task SettledWpfPollingKeepsRegistrationAndAllocatesZeroBytes() => StaThread.RunAsync(async () => {
        using var effects = new DefaultEffectsManager();
        await using var driver = new P4PresentationDriver(false, effects);
        await driver.WarmAsync();
        var owner = driver.Wpf!;
        var registration = Registration(driver);
        var counts = driver.Counts;
        var generation = owner.Generation;
        for (var index = 0; index < 256; index++) _ = owner.RenderFrame(TimeSpan.Zero, automaticFrame: true);
        var start = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 1024; index++) _ = owner.RenderFrame(TimeSpan.Zero, automaticFrame: true);
        var bytes = GC.GetAllocatedBytesForCurrentThread() - start;
        var controlStart = GC.GetAllocatedBytesForCurrentThread();
        var control = new byte[1024];
        var controlBytes = GC.GetAllocatedBytesForCurrentThread() - controlStart;
        GC.KeepAlive(control);
        Assert.Equal(0L, bytes);
        Assert.True(controlBytes >= 1024);
        Assert.Same(registration, Registration(driver));
        Assert.Equal(generation, owner.Generation);
        Assert.Equal(counts, driver.Counts);
        Assert.False(registration.IsRemovalRequested);
        output.WriteLine($"Settled productive WPF polls: {bytes} bytes / 1024; one retained generation registration.");
    });

    /// <summary>Reads the actual device owned by the selected productive transport.</summary>
    /// <param name="driver">The initialized viewport driver.</param>
    /// <returns>The borrowed native device wrapper.</returns>
    private static SilkD3D12Device Device(P4PresentationDriver driver) =>
        P4PresentationDriver.ReadField<SilkD3D12Device>((object?) driver.Wpf ?? driver.Native!, "device");

    /// <summary>Reads the registry membership of an initialized productive generation.</summary>
    /// <param name="driver">The initialized viewport driver.</param>
    /// <returns>The retained generation membership.</returns>
    private static D3D12PresentationDeviceRecovery Registration(P4PresentationDriver driver) =>
        P4PresentationDriver.ReadField<D3D12PresentationDeviceRecovery>(
            (object?) driver.Wpf ?? driver.Native!, "recoveryRegistration");

    /// <summary>Invokes the actual test-process device removal API, balancing the queried COM reference.</summary>
    /// <param name="device">The initialized productive device whose singleton is removed.</param>
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

    /// <summary>Matches the pinned ID3D12Device5.RemoveDevice vtable signature.</summary>
    /// <param name="device">The queried extended native interface.</param>
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void RemoveNativeDevice(nint device);
}

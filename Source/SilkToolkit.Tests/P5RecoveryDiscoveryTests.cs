using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.Wpf.SharpDX.Controls;
using Silk.NET.Direct3D12;
using Xunit;

namespace SilkToolkit.Tests;

/// <summary>Qualifies creation and disposal discovery when removed native owners receive no frame callbacks.</summary>
/// <remarks>Actual process-device removal is exercised without claiming a physical adapter reset.</remarks>
[Collection(WpfCollection.Name)]
public sealed class P5RecoveryDiscoveryTests {
    /// <summary>Coalesces queued discovery without marking a generation removed or invoking its owner under the monitor.</summary>
    /// <returns>The dispatcher-owned managed registry discovery check.</returns>
    [Fact]
    public Task RepeatedDiscoveryCoalescesAndDoesNotMarkNativeOwnersRemoved() => StaThread.RunAsync(async () => {
            var calls = 0;
            var gate = typeof(D3D12PresentationDeviceRecovery).GetField("Gate", BindingFlags.Static | BindingFlags.NonPublic)!
                .GetValue(null)!;
            Action probe = () => {
                Assert.False(Monitor.IsEntered(gate));
                calls++;
            };
            var registration = new D3D12PresentationDeviceRecovery(303, Dispatcher.CurrentDispatcher, probe);
            try {
                for (var attempt = 0; attempt < 32; attempt++) D3D12PresentationDeviceRecovery.ProbeForRemoval();
                Assert.Equal(0, calls);
                Assert.False(registration.IsRemovalRequested);
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                Assert.Equal(1, calls);
                Assert.False(registration.IsRemovalRequested);
            } finally {
                registration.Unregister();
                GC.KeepAlive(probe);
            }
        }
    );

    /// <summary>Lets a new owner discover actual removal while every old WPF owner is inactive with its service timer stopped.</summary>
    /// <param name="hwnd">Whether the newcomer uses HWND instead of WPF composition.</param>
    /// <param name="unload">Whether to genuinely unload both old owners instead of hiding them.</param>
    /// <param name="adapterIndex">The explicit adapter index or the original automatic negative-index policy.</param>
    /// <returns>The new-owner discovery, complete old-owner retirement and fresh pixel qualification.</returns>
    [Theory(Explicit = true)]
    [InlineData(false, false, 0)]
    [InlineData(true, false, 0)]
    [InlineData(false, true, 0)]
    [InlineData(true, true, 0)]
    [InlineData(false, true, -1)]
    [InlineData(true, true, -1)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task NewOwnerDiscoversRemovalAndWakesAllInactiveOwners(bool hwnd, bool unload, int adapterIndex) =>
        StaThread.RunAsync(async () => {
                using var firstEffects = new DefaultEffectsManager(new EffectsManagerConfiguration { AdapterIndex = adapterIndex });
                using var peerEffects = new DefaultEffectsManager(new EffectsManagerConfiguration { AdapterIndex = adapterIndex });
                using var newEffects = new DefaultEffectsManager(new EffectsManagerConfiguration { AdapterIndex = adapterIndex });
                await using var first = new P4PresentationDriver(false, firstEffects);
                await using var peer = new P4PresentationDriver(false, peerEffects);
                using var firstMesh = P4PresentationDriver.CreateMesh();
                using var peerMesh = P4PresentationDriver.CreateMesh();
                first.Viewport.Items.Add(firstMesh);
                peer.Viewport.Items.Add(peerMesh);
                await first.WarmAsync();
                await peer.WarmAsync();
                var firstDevice = Device(first);
                var peerDevice = Device(peer);
                Assert.Equal(firstDevice.NativePointer, peerDevice.NativePointer);
                var firstContext = first.Context!;
                var peerContext = peer.Context!;
                await SuspendAsync(first, unload);
                await SuspendAsync(peer, unload);
                var firstCounts = first.Counts;
                var peerCounts = peer.Counts;
                RemoveDevice(firstDevice);
                // Neither old owner is ticked after removal: only new native creation may initiate discovery.
                await using var newcomer = new P4PresentationDriver(hwnd, newEffects);
                using var newMesh = P4PresentationDriver.CreateMesh();
                newcomer.Viewport.Items.Add(newMesh);
                await newcomer.WarmAsync();
                Assert.True(firstDevice.IsDisposed);
                Assert.True(peerDevice.IsDisposed);
                Assert.Null(firstContext.Camera);
                Assert.Null(peerContext.Camera);
                Assert.Equal(firstCounts, first.Counts);
                Assert.Equal(peerCounts, peer.Counts);
                Assert.False(first.Wpf!.IsInitialized);
                Assert.False(peer.Wpf!.IsInitialized);
                P4PresentationDriver.AssertPixel(await newcomer.CaptureAsync(), 32, 24, 0, 255, 0, 255);
                await ResumeAsync(first, unload);
                await ResumeAsync(peer, unload);
                await first.DrainAsync();
                await peer.DrainAsync();
                P4PresentationDriver.AssertPixel(await first.CaptureAsync(), 32, 24, 0, 255, 0, 255);
                P4PresentationDriver.AssertPixel(await peer.CaptureAsync(), 32, 24, 0, 255, 0, 255);
                Assert.False(firstEffects.IsDisposed);
                Assert.False(peerEffects.IsDisposed);
                Assert.False(newEffects.IsDisposed);
            }
        );

    /// <summary>Discovers removal exclusively during WPF disposal preparation or final native release.</summary>
    /// <param name="preparedBeforeRemoval">Whether preparation ran while healthy, leaving final release as the sole discoverer.</param>
    /// <returns>The inactive-peer broadcast and preserved external-manager recovery verification.</returns>
    [Theory(Explicit = true)]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task DisposalAloneBroadcastsRemovalToInactivePeer(bool preparedBeforeRemoval) => StaThread.RunAsync(async () => {
            using var firstEffects = new DefaultEffectsManager();
            using var peerEffects = new DefaultEffectsManager();
            await using var first = new P4PresentationDriver(false, firstEffects);
            await using var peer = new P4PresentationDriver(false, peerEffects);
            using var firstMesh = P4PresentationDriver.CreateMesh();
            using var peerMesh = P4PresentationDriver.CreateMesh();
            first.Viewport.Items.Add(firstMesh);
            peer.Viewport.Items.Add(peerMesh);
            await first.WarmAsync();
            await peer.WarmAsync();
            var firstDevice = Device(first);
            var peerDevice = Device(peer);
            Assert.Equal(firstDevice.NativePointer, peerDevice.NativePointer);
            await SuspendAsync(first, true);
            await SuspendAsync(peer, true);
            var before = peer.Counts;
            if (preparedBeforeRemoval) first.Wpf!.PrepareForDisposal();
            RemoveDevice(firstDevice);
            if (!preparedBeforeRemoval) {
                first.Wpf!.PrepareForDisposal();
                await P4PresentationDriver.PollAsync(() => peerDevice.IsDisposed, "disposal preparation discovery");
                Assert.False(firstDevice.IsDisposed);
            }

            await first.DisposeAsync();
            await P4PresentationDriver.PollAsync(() => peerDevice.IsDisposed, "final disposal discovery");
            Assert.True(firstDevice.IsDisposed);
            Assert.Equal(before, peer.Counts);
            Assert.False(peer.Wpf!.IsInitialized);
            await ResumeAsync(peer, true);
            await peer.DrainAsync();
            P4PresentationDriver.AssertPixel(await peer.CaptureAsync(), 32, 24, 0, 255, 0, 255);
            Assert.False(firstEffects.IsDisposed);
            Assert.False(peerEffects.IsDisposed);
        }
    );

    /// <summary>Uses the standalone surface's automatic negative-index creation failure to discover an inactive WPF owner.</summary>
    /// <returns>The default hardware creation retry and real recovered standalone pixels.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task StandaloneAutomaticSurfaceDiscoversInactiveRemovedOwner() => StaThread.RunAsync(async () => {
            using var effects = new DefaultEffectsManager();
            await using var first = new P4PresentationDriver(false, effects);
            using var mesh = P4PresentationDriver.CreateMesh();
            first.Viewport.Items.Add(mesh);
            await first.WarmAsync();
            var device = Device(first);
            await SuspendAsync(first, true);
            RemoveDevice(device);
            using var surface = new D3D12PresentationSurface { Width = 32, Height = 24 };
            using var source = AttachSurface(surface);
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            await P4PresentationDriver.PollAsync(
                () => {
                    surface.EnsureNativeInitialized();
                    return surface.IsInitialized;
                }, "standalone automatic native recovery creation"
            );
            Assert.True(device.IsDisposed);
            Assert.False(first.Wpf!.IsInitialized);
            Assert.NotNull(first.Viewport.CameraCore);
            _ = surface.RenderViewportOnce([0, 1, 0, 1], [], first.Viewport.CameraCore!, false, captureFrame: true);
            Assert.Equal(1UL, surface.PresentedFrames);
            Assert.Equal(
                1UL, P4PresentationDriver.ReadField<D3D12FrameSubmission>(surface, "_frameSubmission")
                    .CompletedFrames
            );
            P4PresentationDriver.AssertPixel(surface.LastCapturedFrame!.Value, 16, 12, 0, 255, 0, 255);
            Assert.False(effects.IsDisposed);
        }
    );

    /// <summary>Probes a healthy WARP native owner without treating removal of a different actual hardware device as its removal.</summary>
    /// <returns>The distinct device-identity, unchanged WARP generation and fresh native output verification.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task DiscoveryLeavesHealthyDifferentNativeDeviceInitialized() => StaThread.RunAsync(async () => {
            using var effects = new DefaultEffectsManager();
            await using var first = new P4PresentationDriver(false, effects);
            await first.WarmAsync();
            var removed = Device(first);
            await SuspendAsync(first, true);
            using var surface = new D3D12PresentationSurface(SilkDriverType.Warp) { Width = 32, Height = 24 };
            using var source = AttachSurface(surface);
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            surface.EnsureNativeInitialized();
            Assert.True(surface.IsInitialized);
            var healthy = P4PresentationDriver.ReadField<SilkD3D12Device>(surface, "device");
            var registration = P4PresentationDriver.ReadField<D3D12PresentationDeviceRecovery>(surface, "recoveryRegistration");
            Assert.NotEqual(removed.NativePointer, healthy.NativePointer);
            RemoveDevice(removed);
            D3D12PresentationDeviceRecovery.ProbeForRemoval();
            D3D12PresentationDeviceRecovery.ProbeForRemoval();
            await P4PresentationDriver.PollAsync(() => removed.IsDisposed, "actual removed-device discovery");
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.True(surface.IsInitialized);
            Assert.False(healthy.IsDisposed);
            Assert.True(healthy.GetDeviceRemovedReason() >= 0);
            Assert.False(registration.IsRemovalRequested);
            Assert.Same(healthy, P4PresentationDriver.ReadField<SilkD3D12Device>(surface, "device"));
            _ = surface.RenderViewportOnce([0, 1, 0, 1], [], first.Viewport.CameraCore!, false, captureFrame: true);
            P4PresentationDriver.AssertPixel(surface.LastCapturedFrame!.Value, 16, 12, 0, 255, 0, 255);
        }
    );

    /// <summary>Stops actual WPF eligibility and drains only accepted transport before native device removal.</summary>
    /// <param name="driver">The initialized WPF owner.</param>
    /// <param name="unload">Whether to remove its actual loaded visual rather than hide it.</param>
    /// <returns>The inactive state with no running service timer or pending transport.</returns>
    private static async Task SuspendAsync(P4PresentationDriver driver, bool unload) {
        if (unload) driver.Root.Children.Remove(driver.Viewport);
        else driver.Viewport.Visibility = Visibility.Hidden;
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        await driver.FinishTransportAsync();
        Assert.False(driver.Canvas!.IsActive);
        Assert.True(driver.Wpf!.IsInitialized);
        Assert.False(
            P4PresentationDriver.ReadField<DispatcherTimer>(driver.Wpf, "serviceTimer")
                .IsEnabled
        );
    }

    /// <summary>Restores actual WPF load or visibility after old native ownership has been retired.</summary>
    /// <param name="driver">The inactive WPF owner.</param>
    /// <param name="unload">Whether its visual was removed rather than hidden.</param>
    /// <returns>The restored visual lifecycle before explicit rendering resumes.</returns>
    private static async Task ResumeAsync(P4PresentationDriver driver, bool unload) {
        if (unload) driver.Root.Children.Add(driver.Viewport);
        else driver.Viewport.Visibility = Visibility.Visible;
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
    }

    /// <summary>Attaches the standalone surface to a real native presentation source using its normal loaded lifecycle.</summary>
    /// <param name="surface">The standalone native owner.</param>
    /// <returns>The caller-owned native source, disposed before final surface disposal.</returns>
    private static HwndSource AttachSurface(D3D12PresentationSurface surface) {
        var source = new HwndSource(
            new HwndSourceParameters(nameof(P5RecoveryDiscoveryTests)) {
                Width = 64, Height = 48, WindowStyle = unchecked((int) 0x80000000)
            }
        ) { RootVisual = surface };
        surface.Measure(new Size(32, 24));
        surface.Arrange(new Rect(0, 0, 32, 24));
        surface.UpdateLayout();
        return source;
    }

    /// <summary>Reads the actual device retained by an initialized WPF presentation owner.</summary>
    /// <param name="driver">The initialized WPF owner.</param>
    /// <returns>The borrowed productive native device.</returns>
    private static SilkD3D12Device Device(P4PresentationDriver driver) =>
        P4PresentationDriver.ReadField<SilkD3D12Device>(driver.Wpf!, "device");

    /// <summary>Invalidates the actual process singleton and balances the temporary extended COM interface reference.</summary>
    /// <param name="device">The productive device to remove.</param>
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

    /// <summary>Matches ID3D12Device5.RemoveDevice in the pinned native COM interface.</summary>
    /// <param name="device">The queried extended device interface.</param>
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void RemoveNativeDevice(nint device);
}
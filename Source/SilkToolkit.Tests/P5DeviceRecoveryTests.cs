using System.Reflection;
using System.Runtime.InteropServices;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.Wpf.SharpDX.Controls;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Xunit;

namespace SilkToolkit.Tests;

/// <summary>Qualifies scoped native device removal, pending-frame abandonment and configured adapter selection.</summary>
/// <param name="output">Receives actual removal HRESULTs and adapter identities.</param>
[Collection(WpfCollection.Name)]
public sealed class P5DeviceRecoveryTests(ITestOutputHelper output) {
    /// <summary>Abandons only failed pending work while preserving already observed, unclaimed completions.</summary>
    [Fact]
    public void RemovedSubmissionDoesNotManufactureOrDiscardCompletions() {
        var frame = new D3D12FrameSubmission();
        frame.Submit(1, TimeSpan.Zero);
        Assert.True(frame.TryComplete(1, TimeSpan.Zero));
        frame.Submit(2, TimeSpan.Zero);
        frame.AbandonRemovedDeviceSubmission();
        Assert.False(frame.IsPending);
        Assert.Equal(1UL, frame.CompletedFrames);
        Assert.True(frame.TryTakeCompletion());
        Assert.False(frame.TryTakeCompletion());
        frame.Submit(1, TimeSpan.FromSeconds(1));
        Assert.True(frame.TryComplete(1, TimeSpan.FromSeconds(1)));
        Assert.Equal(2UL, frame.CompletedFrames);
    }

    /// <summary>Removes only the test-owned D3D12 device while an actual frame is blocked and then renders a new generation.</summary>
    /// <param name="hwnd">Whether to select HWND instead of WPF composition.</param>
    /// <returns>The actual native removal, bounded retirement and fresh pixel recovery verification.</returns>
    [Theory(Explicit = true)]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task RemovedProductiveDeviceRetiresPendingWorkAndRecreates(bool hwnd) => StaThread.RunAsync(async () => {
        using var effects = new DefaultEffectsManager();
        await using var driver = new P4PresentationDriver(hwnd, effects);
        using var mesh = P4PresentationDriver.CreateMesh();
        driver.Viewport.Items.Add(mesh);
        await driver.WarmAsync();
        var owner = (object?) driver.Wpf ?? driver.Native!;
        var oldDevice = P4PresentationDriver.ReadField<SilkD3D12Device>(owner, "device");
        var oldContext = driver.Context!;
        var before = driver.Counts;
        using (var gate = new P4NativeQueueGate(driver)) {
            driver.Viewport.InvalidateRender();
            Assert.True(driver.Tick() > 0);
            Assert.Equal(before.Submitted + 1, driver.Submitted);
            Assert.Equal(before.Completed, driver.Completed);
            RemoveDevice(oldDevice);
            var reason = oldDevice.GetDeviceRemovedReason();
            Assert.True(reason < 0);
            output.WriteLine($"P5 {(hwnd ? "HWND" : "WPF")} scoped ID3D12Device5.RemoveDevice HRESULT=0x{reason:X8}.");
            await P4PresentationDriver.PollAsync(() => {
                try {
                    if (driver.Wpf is { } wpf) wpf.RetireSceneResources();
                    else driver.Native!.RetireSceneResources();
                    return oldDevice.IsDisposed;
                } catch (TimeoutException) {
                    return false;
                }
            }, "removed native device retirement");
            Assert.True(oldDevice.IsDisposed);
            Assert.Null(oldContext.Camera);
            Assert.Equal(before.Completed, driver.Completed);
            Assert.Equal(before.Rendered, driver.RenderedCount);
            Assert.Equal(before.OnRendered, driver.OnRenderedCount);
            if (driver.Native is { } native)
                Assert.False(P4PresentationDriver.ReadField<D3D12FrameSubmission>(native, "_frameSubmission").IsPending);
        }
        var recovery = driver.Counts;
        if (hwnd) {
            await P4PresentationDriver.PollAsync(() => driver.Viewport.CanSubmitNativeScene, "replacement native eligibility");
            Assert.True(driver.Tick(wait: true) > 0);
            driver.AssertAdvanced(recovery, 1);
        } else {
            await driver.DrainAsync();
        }
        Assert.Equal(driver.Completed + 1, driver.Submitted);
        Assert.NotSame(oldContext, driver.Context);
        Assert.True(driver.Submitted > before.Submitted + 1);
        P4PresentationDriver.AssertPixel(await driver.CaptureAsync(), 32, 24, 0, 255, 0, 255);
        Assert.False(effects.IsDisposed);
    });

    /// <summary>Checks that actual presentation uses the configured index and that WPF interop retains its exact LUID.</summary>
    /// <param name="hwnd">Whether to select HWND instead of WPF composition.</param>
    /// <returns>The productive adapter identity verification.</returns>
    [Theory(Explicit = true)]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task ProductiveAdapterSelectionMatchesItsRequestedIndex(bool hwnd) => StaThread.RunAsync(async () => {
        using var effects = new DefaultEffectsManager(new EffectsManagerConfiguration { AdapterIndex = 0 });
        await using var driver = new P4PresentationDriver(hwnd, effects);
        await driver.WarmAsync();
        var owner = (object?) driver.Wpf ?? driver.Native!;
        var device = P4PresentationDriver.ReadField<SilkD3D12Device>(owner, "device");
        using var expected = SilkD3D12DeviceFactory.CreateForAdapter(effects.AdapterIndex);
        Assert.Equal(AdapterLuid(expected), AdapterLuid(device));
        if (driver.Wpf is { } wpf)
            Assert.Equal(AdapterLuid(device), P4PresentationDriver.ReadField<D3D12ImageInteropBridge>(wpf, "bridge").AdapterLuid);
        output.WriteLine($"P5 {(hwnd ? "HWND" : "WPF")} configured DXGI={effects.AdapterIndex}; actual LUID=0x{AdapterLuid(device):X16}.");
    });

    /// <summary>Rejects an unavailable explicit adapter instead of silently selecting another hardware device.</summary>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public void InvalidExplicitAdapterDoesNotFallBackToAutomaticSelection() {
        Assert.Throws<COMException>(() => SilkD3D12DeviceFactory.CreateForAdapter(int.MaxValue));
    }

    /// <summary>Reports removal on one isolated test-owned device; this does not reset the physical adapter.</summary>
    /// <param name="device">The device whose accepted queue is to be invalidated.</param>
    private static void RemoveDevice(SilkD3D12Device device) {
        var id = ID3D12Device5.Guid;
        Marshal.ThrowExceptionForHR(Marshal.QueryInterface(device.NativePointer, in id, out var extended));
        try {
            Method<RemoveNativeDevice>(extended, 58)(extended);
        } finally {
            Marshal.Release(extended);
        }
    }

    /// <summary>Reads the same struct-return ABI used by the pinned Silk.NET 2.23 D3D12 wrapper.</summary>
    /// <param name="device">The actual native device.</param>
    /// <returns>The complete adapter identity.</returns>
    private static long AdapterLuid(SilkD3D12Device device) {
        _ = Method<GetAdapterLuid>(device.NativePointer, 43)(device.NativePointer, out var luid);
        return unchecked((long) ((ulong) (uint) luid.High << 32 | luid.Low));
    }

    /// <summary>Reads one stable COM vtable entry from an actual native interface.</summary>
    /// <typeparam name="T">The exact native signature.</typeparam>
    /// <param name="instance">The interface pointer.</param>
    /// <param name="slot">The vtable index verified against pinned Silk.NET IL.</param>
    /// <returns>The native entry point.</returns>
    private static T Method<T>(nint instance, int slot) where T : Delegate =>
        Marshal.GetDelegateForFunctionPointer<T>(Marshal.ReadIntPtr(Marshal.ReadIntPtr(instance), slot * nint.Size));

    /// <summary>Invokes ID3D12Device5.RemoveDevice on a test-owned device.</summary>
    /// <param name="device">The extended device interface.</param>
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void RemoveNativeDevice(nint device);
    /// <summary>Reads a D3D12 adapter identity through its native result-buffer ABI.</summary>
    /// <param name="device">The actual device.</param>
    /// <param name="luid">Receives the adapter identity.</param>
    /// <returns>The result-buffer pointer.</returns>
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate nint GetAdapterLuid(nint device, out Luid luid);
}

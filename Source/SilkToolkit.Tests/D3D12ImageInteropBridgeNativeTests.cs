using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.Wpf.SharpDX.Controls;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.Direct3D9;
using Silk.NET.DXGI;
using Xunit;
using Format9 = Silk.NET.Direct3D9.Format;

namespace SilkToolkit.Tests;

/// <summary>Exercises the production bridge with a real caller-owned hardware submission stack.</summary>
/// <remarks>Readback and publication acknowledgments are test-only; these tests never present CPU pixels.</remarks>
[Collection(WpfCollection.Name)]
public sealed class D3D12ImageInteropBridgeNativeTests {
    /// <summary>Verifies actual D3D9 pixels, immutable bound slots and retained output across lock-retry analogues.</summary>
    /// <returns>The explicit STA hardware regression.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    [Trait("Category", "Wpf")]
    public Task BorrowedDeviceCopiesPremultipliedBgraWithoutOverwritingPublishedSlot() => StaThread.RunAsync(() => {
            using var frame = new HardwareFrame();
            var bridge = frame.Bridge;
            Assert.True(bridge.IsCopyComplete);
            Assert.False(bridge.IsCopyPending);
            Assert.False(bridge.HasReadySurface);
            Assert.Equal(0, bridge.ReadySurface);
            Assert.Equal(0UL, bridge.ReadySerial);
            Assert.Equal(0UL, bridge.CopyCount);
            Assert.Throws<InvalidOperationException>(() => bridge.ConfirmPublication(1));
            frame.Clear(1, 0.5f, 0.25f, 0.5f);
            bridge.BeginCopy(1);
            Assert.True(bridge.IsCopyPending);
            Assert.False(bridge.HasReadySurface);
            Assert.Throws<InvalidOperationException>(() => bridge.BeginCopy(2));
            frame.WaitForCopy();
            var first = bridge.ReadySurface;
            AssertSurface(first, [32, 64, 128, 128]);
            AssertActualDevice(frame.Device, bridge.Target.ColorResource);
            Assert.Equal(bridge.AdapterLuid, ReadD3D9AdapterLuid(first));

            for (var retry = 0; retry < 128; retry++) {
                // A failed TryLock performs no publication acknowledgment and no new copy.
                Assert.True(bridge.IsCopyComplete);
                Assert.True(bridge.HasReadySurface);
                Assert.Equal(first, bridge.ReadySurface);
                Assert.Equal(1UL, bridge.ReadySerial);
            }

            Assert.Throws<InvalidOperationException>(() => bridge.BeginCopy(2));
            Assert.Throws<InvalidOperationException>(() => bridge.ConfirmPublication(2));
            Assert.Equal(1UL, bridge.CopyCount);
            Assert.Equal(1UL, bridge.ReadySerial);
            bridge.ConfirmPublication(1);
            Assert.False(bridge.HasReadySurface);
            frame.Clear(0, 0, 1, 1);
            bridge.BeginCopy(2); // The earlier rejected serial remains available.
            AssertSurface(first, [32, 64, 128, 128]);
            frame.WaitForCopy();
            var second = bridge.ReadySurface;
            Assert.NotEqual(first, second);
            AssertSurface(second, [255, 0, 0, 255]);
            AssertSurface(first, [32, 64, 128, 128]);
            Assert.Throws<InvalidOperationException>(() => bridge.ConfirmPublication(1));
            bridge.ConfirmPublication(2);
            frame.Clear(1, 1, 1, 0);
            bridge.BeginCopy(3);
            frame.WaitForCopy();
            Assert.Equal(first, bridge.ReadySurface);
            AssertSurface(first, [0, 0, 0, 0]);
            bridge.ConfirmDetached();
            Assert.Equal(first, bridge.ReadySurface);
            Assert.Equal(3UL, bridge.ReadySerial);
            bridge.ConfirmPublication(3);
            Assert.Equal(3UL, bridge.CopyCount);
        }
    );

    /// <summary>Checks same-serial rebind bookkeeping and rejects stale acknowledgments or invalid source states.</summary>
    /// <returns>The explicit STA slot-lifecycle regression.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    [Trait("Category", "Wpf")]
    public Task DetachedPublishedSurfaceRebindProtectsItsSlotAndReadySerial() => StaThread.RunAsync(() => {
            using var frame = new HardwareFrame();
            var bridge = frame.Bridge;
            frame.Clear(0, 1, 0, 1);
            bridge.BeginCopy(5);
            frame.WaitForCopy();
            var first = bridge.ReadySurface;
            bridge.ConfirmPublication(5);
            bridge.ConfirmDetached();
            bridge.ConfirmPublication(5); // Analogue of rebind/unlock of the retained published pointer.
            Assert.Equal(1UL, bridge.CopyCount);
            Assert.Throws<ArgumentOutOfRangeException>(() => bridge.BeginCopy(5));
            Assert.Throws<ArgumentOutOfRangeException>(() => bridge.BeginCopy(0));
            Assert.Throws<InvalidOperationException>(() => bridge.ConfirmPublication(4));
            frame.Context.Reset();
            frame.Context.Transition(bridge.Target.ColorResource, ResourceStates.RenderTarget);
            Assert.Throws<InvalidOperationException>(() => bridge.BeginCopy(6));
            Assert.Equal(1UL, bridge.CopyCount);
            frame.Context.Transition(bridge.Target.ColorResource, ResourceStates.Common);
            frame.Context.Close();
            frame.SubmitAndDrain();
            bridge.BeginCopy(6);
            Assert.Throws<InvalidOperationException>(() => bridge.ConfirmPublication(5));
            frame.WaitForCopy();
            var second = bridge.ReadySurface;
            Assert.NotEqual(first, second);
            Assert.Throws<InvalidOperationException>(() => bridge.ConfirmPublication(5));
            bridge.ConfirmDetached();
            Assert.Equal(second, bridge.ReadySurface);
            Assert.Equal(6UL, bridge.ReadySerial);
            bridge.ConfirmPublication(6);
            bridge.BeginCopy(7);
            frame.WaitForCopy();
            Assert.Equal(first, bridge.ReadySurface);
            Assert.Equal(7UL, bridge.ReadySerial);
        }
    );

    /// <summary>Measures warmed real copies, query polling and acknowledgments without recurring managed allocations.</summary>
    /// <returns>The explicit STA hardware allocation regression.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    [Trait("Category", "Wpf")]
    public Task WarmCopiesAndPublicationBookkeepingReuseAllManagedStorage() => StaThread.RunAsync(() => {
            using var frame = new HardwareFrame();
            frame.Clear(1, 0, 0, 1);
            for (ulong serial = 1; serial <= 32; serial++) frame.CopyAndAcknowledge(serial);
            var countBefore = frame.Bridge.CopyCount;
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (ulong serial = 33; serial <= 96; serial++) frame.CopyAndAcknowledge(serial);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.Equal(64UL, frame.Bridge.CopyCount - countBefore);
            Assert.Equal(0L, allocated);
            frame.Bridge.BeginCopy(97);
            frame.WaitForCopy();
            var ready = frame.Bridge.ReadySurface;
            AssertSurface(ready, [0, 0, 255, 255]);
            before = GC.GetAllocatedBytesForCurrentThread();
            var stable = true;
            for (var retry = 0; retry < 1024; retry++)
                stable &= frame.Bridge.IsCopyComplete && frame.Bridge.HasReadySurface &&
                          frame.Bridge.ReadySurface == ready && frame.Bridge.ReadySerial == 97;
            allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.True(stable);
            Assert.Equal(0L, allocated);
            Assert.Equal(97UL, frame.Bridge.CopyCount);
        }
    );

    /// <summary>Checks creation validation, enforced thread affinity and retention until confirmed teardown.</summary>
    /// <returns>The explicit STA lifetime regression.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    [Trait("Category", "Wpf")]
    public Task TeardownRequiresDetachmentAndLeavesBorrowedDeviceAlive() => StaThread.RunAsync(() => {
            using var frame = new HardwareFrame();
            var bridge = frame.Bridge;
            using (var unused = new D3D12ImageInteropBridge(frame.Device, frame.Window.Handle, 3, 2)) {
                unused.Dispose();
                Assert.True(unused.IsDisposed);
                Assert.False(frame.Device.IsDisposed);
            }

            Assert.Throws<ArgumentException>(() => new D3D12ImageInteropBridge(frame.Device, 0, 8, 6));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new D3D12ImageInteropBridge(frame.Device, frame.Window.Handle, 0, 6)
            );
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new D3D12ImageInteropBridge(frame.Device, frame.Window.Handle, 8, uint.MaxValue)
            );
            Exception? wrongThread = null;
            var thread = new Thread(() => {
                    try {
                        _ = bridge.IsCopyComplete;
                    } catch (Exception exception) {
                        wrongThread = exception;
                    }
                }
            );
            thread.Start();
            Assert.True(thread.Join(TimeSpan.FromSeconds(5)));
            Assert.IsType<InvalidOperationException>(wrongThread);
            frame.Clear(1, 0, 0, 1);
            bridge.BeginCopy(1);
            Assert.Throws<InvalidOperationException>(bridge.Dispose);
            Assert.False(bridge.IsDisposed);
            Assert.False(bridge.Target.ColorResource.IsDisposed);
            frame.WaitForCopy();
            var source = bridge.Target.ColorResource;
            var descriptor = bridge.Target.RenderTargetView;
            Assert.Throws<InvalidOperationException>(bridge.Dispose);
            // These native tests never attach a surface to WPF; this truthfully acknowledges actual detachment.
            bridge.ConfirmDetached();
            bridge.Dispose();
            Assert.True(bridge.IsDisposed);
            Assert.True(source.IsDisposed);
            Assert.True(descriptor.IsDisposed);
            Assert.False(frame.Device.IsDisposed);
            using var survivor = frame.Device.CreateBuffer(256);
            Assert.NotEqual(0, survivor.NativePointer);
            Assert.Throws<ObjectDisposedException>(() => bridge.BeginCopy(2));
            bridge.Dispose();

            using var copyingFrame = new HardwareFrame();
            copyingFrame.Clear(0, 1, 0, 1);
            copyingFrame.Bridge.BeginCopy(1);
            Assert.True(copyingFrame.Bridge.IsCopyPending);
            // No completion poll precedes Dispose: teardown itself must drain the submitted D3D11 event.
            copyingFrame.Bridge.ConfirmDetached();
            copyingFrame.Bridge.Dispose();
            Assert.True(copyingFrame.Bridge.IsDisposed);
            Assert.False(copyingFrame.Device.IsDisposed);
        }
    );

    /// <summary>Compares actual top-left and bottom-right premultiplied BGRA destination bytes.</summary>
    /// <param name="surface">The completed D3D9 destination surface.</param>
    /// <param name="expected">Expected BGRA channels, allowing one unit of GPU conversion rounding.</param>
    private static void AssertSurface(nint surface, byte[] expected) {
        foreach (var last in new[] { false, true }) {
            var actual = ReadSurfacePixel(surface, 8, 6, last);
            for (var channel = 0; channel < 4; channel++)
                Assert.InRange((int) actual[channel], expected[channel] - 1, expected[channel] + 1);
        }
    }

    /// <summary>Checks that the canonical source belongs to the exact caller-owned D3D12 device.</summary>
    /// <param name="device">The actual submission device.</param>
    /// <param name="source">The bridge source.</param>
    private static void AssertActualDevice(SilkD3D12Device device, SilkD3D12Resource source) {
        var deviceId = ID3D12Device.Guid;
        Marshal.ThrowExceptionForHR(
            Method<GetResourceDevice>(source.NativePointer, 7)(
                source.NativePointer, in deviceId, out var actual
            )
        );
        try {
            Assert.Equal(device.NativePointer, actual);
        } finally {
            Marshal.Release(actual);
        }
    }

    /// <summary>Reads the actual D3D9Ex adapter LUID selected by the destination's native device.</summary>
    /// <param name="surface">A completed destination surface.</param>
    /// <returns>The D3D9Ex device's actual adapter identity.</returns>
    private static long ReadD3D9AdapterLuid(nint surface) {
        Marshal.ThrowExceptionForHR(Method<GetDevice>(surface, 3)(surface, out var device));
        nint direct3D = 0;
        nint extended = 0;
        try {
            Marshal.ThrowExceptionForHR(Method<GetCreationParameters>(device, 9)(device, out var creation));
            Marshal.ThrowExceptionForHR(Method<GetDevice>(device, 6)(device, out direct3D));
            var id = IDirect3D9Ex.Guid;
            Marshal.ThrowExceptionForHR(Marshal.QueryInterface(direct3D, ref id, out extended));
            Marshal.ThrowExceptionForHR(
                Method<GetAdapterLuid>(extended, 21)(
                    extended, creation.AdapterOrdinal, out var luid
                )
            );
            return unchecked((long) ((ulong) (uint) luid.High << 32 | luid.Low));
        } finally {
            if (extended != 0) Marshal.Release(extended);
            if (direct3D != 0) Marshal.Release(direct3D);
            Marshal.Release(device);
        }
    }

    /// <summary>Owns the real D3D12 submission stack exclusively in this opt-in native test harness.</summary>
    private sealed class HardwareFrame : IDisposable {
        /// <summary>Owns the clear submission queue, independent of the bridge.</summary>
        private SilkD3D12CommandQueue? queue;

        /// <summary>Owns the clear completion fence, independent of the bridge.</summary>
        private SilkD3D12Fence? fence;

        /// <summary>Gets the hidden HWND used solely for D3D9Ex creation.</summary>
        internal HwndSource Window { get; }

        /// <summary>Gets the actual hardware device borrowed by the bridge.</summary>
        internal SilkD3D12Device Device { get; }

        /// <summary>Gets the caller-owned clear command context.</summary>
        internal SilkD3D12CommandContext Context { get; } = null!;

        /// <summary>Gets the production bridge under test.</summary>
        internal D3D12ImageInteropBridge Bridge { get; } = null!;

        /// <summary>Creates hardware submission owners and the production bridge on the current STA thread.</summary>
        internal HardwareFrame() {
            Window = new HwndSource(
                new HwndSourceParameters(nameof(D3D12ImageInteropBridgeNativeTests)) {
                    Width = 16, Height = 16, WindowStyle = unchecked((int) 0x80000000)
                }
            );
            try {
                Device = SilkD3D12DeviceFactory.CreateDefault();
                queue = Device.CreateCommandQueue();
                Context = Device.CreateCommandContext();
                fence = Device.CreateFence();
                Bridge = new D3D12ImageInteropBridge(Device, Window.Handle, 8, 6);
            } catch {
                // Construction submits no work and never attaches a WPF surface.
                Context?.Dispose();
                fence?.Dispose();
                queue?.Dispose();
                Device?.Dispose();
                Window.Dispose();
                throw;
            }
        }

        /// <summary>Clears the real bridge target and completes D3D12 work before handing it to D3D11.</summary>
        /// <param name="r">Straight red.</param>
        /// <param name="g">Straight green.</param>
        /// <param name="b">Straight blue.</param>
        /// <param name="a">Coverage alpha.</param>
        internal void Clear(float r, float g, float b, float a) {
            var target = Bridge.Target;
            Context.Reset();
            Context.Transition(target.ColorResource, ResourceStates.RenderTarget);
            Context.ClearRenderTarget(target.ColorResource, target.RenderTargetView, [r * a, g * a, b * a, a]);
            Context.Transition(target.ColorResource, ResourceStates.Common);
            Context.Close();
            SubmitAndDrain();
        }

        /// <summary>Submits the closed clear list and drains its explicit caller-owned fence.</summary>
        internal void SubmitAndDrain() {
            queue!.Execute(Context);
            fence!.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));
            Device.ThrowIfDeviceRemoved();
        }

        /// <summary>Polls the bridge with a finite test-only wait, without allocating a callback.</summary>
        internal void WaitForCopy() {
            var started = Stopwatch.GetTimestamp();
            while (!Bridge.IsCopyComplete) {
                if (Stopwatch.GetElapsedTime(started) >= TimeSpan.FromSeconds(5))
                    throw new TimeoutException("Hardware bridge copy did not finish within five seconds.");
                Thread.Yield();
            }
        }

        /// <summary>Executes one real GPU copy and acknowledges a successful publication analogue.</summary>
        /// <param name="serial">The next copy serial.</param>
        internal void CopyAndAcknowledge(ulong serial) {
            Bridge.BeginCopy(serial);
            WaitForCopy();
            Bridge.ConfirmPublication(serial);
        }

        /// <summary>Drains D3D12 and confirms no real WPF binding before releasing bridge and borrowed device.</summary>
        /// <remarks>A failed drain or bridge teardown retains the dependent objects instead of freeing them.</remarks>
        public void Dispose() {
            fence!.Wait(queue!.Signal(fence), TimeSpan.FromSeconds(5));
            if (!Bridge.IsDisposed) {
                Bridge.ConfirmDetached();
                Bridge.Dispose();
            }

            Context.Dispose();
            fence.Dispose();
            queue.Dispose();
            Device.Dispose();
            Window.Dispose();
        }
    }

    /// <summary>Calls ID3D12DeviceChild::GetDevice on the canonical source resource.</summary>
    /// <param name="self">The source resource.</param>
    /// <param name="interfaceId">The requested D3D12 device interface.</param>
    /// <param name="device">The retained caller-owned device pointer.</param>
    /// <returns>The native HRESULT.</returns>
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetResourceDevice(nint self, in Guid interfaceId, out nint device);

    /// <summary>Calls IDirect3DDevice9::GetCreationParameters.</summary>
    /// <param name="self">The native device.</param>
    /// <param name="creation">The actual device's adapter ordinal and creation flags.</param>
    /// <returns>The native HRESULT.</returns>
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetCreationParameters(nint self, out DeviceCreationParameters creation);

    /// <summary>Calls IDirect3D9Ex::GetAdapterLUID.</summary>
    /// <param name="self">The extended D3D9 factory.</param>
    /// <param name="adapter">The ordinal read from the actual device's creation parameters.</param>
    /// <param name="luid">The selected adapter identity.</param>
    /// <returns>The native HRESULT.</returns>
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetAdapterLuid(nint self, uint adapter, out Luid luid);

    /// <summary>Reads one pixel using a temporary D3D9 system-memory surface; never used by presentation code.</summary>
    /// <param name="surface">The shared render-target surface.</param>
    /// <param name="width">The expected texture width.</param>
    /// <param name="height">The expected texture height.</param>
    /// <param name="last">Whether to read the bottom-right pixel instead of the top-left pixel.</param>
    /// <returns>The four BGRA bytes.</returns>
    private static byte[] ReadSurfacePixel(nint surface, uint width, uint height, bool last) {
        Marshal.ThrowExceptionForHR(Method<GetDescription>(surface, 12)(surface, out var description));
        Assert.Equal(Format9.A8R8G8B8, description.Format);
        Assert.Equal(width, description.Width);
        Assert.Equal(height, description.Height);
        Marshal.ThrowExceptionForHR(Method<GetDevice>(surface, 3)(surface, out var device));
        nint staging = 0;
        try {
            Marshal.ThrowExceptionForHR(
                Method<CreateOffscreenSurface>(device, 36)(
                    device, width, height,
                    Format9.A8R8G8B8, Pool.Systemmem, out staging, 0
                )
            );
            Marshal.ThrowExceptionForHR(Method<CopyRenderTarget>(device, 32)(device, surface, staging));
            Marshal.ThrowExceptionForHR(Method<LockSurface>(staging, 13)(staging, out var locked, 0, 0x10));
            try {
                var offset = last
                    ? checked((int) (height - 1) * locked.Pitch + (int) (width - 1) * 4)
                    : 0;
                var pixel = new byte[4];
                Marshal.Copy(locked.Bits + offset, pixel, 0, pixel.Length);
                return pixel;
            } finally {
                Marshal.ThrowExceptionForHR(Method<UnlockSurface>(staging, 14)(staging));
            }
        } finally {
            if (staging != 0) Marshal.Release(staging);
            Marshal.Release(device);
        }
    }

    /// <summary>Gets a typed D3D9 COM method without enabling unsafe code for the test project.</summary>
    /// <typeparam name="T">The native method signature.</typeparam>
    /// <param name="instance">The COM object.</param>
    /// <param name="slot">The documented vtable slot including IUnknown.</param>
    /// <returns>The callable native method.</returns>
    private static T Method<T>(nint instance, int slot) where T : Delegate =>
        Marshal.GetDelegateForFunctionPointer<T>(Marshal.ReadIntPtr(Marshal.ReadIntPtr(instance), slot * nint.Size));

    /// <summary>Marshals D3DLOCKED_RECT using a pointer-sized managed integer.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct LockedSurface {
        /// <summary>The byte stride between adjacent rows.</summary>
        public int Pitch;

        /// <summary>The first readable byte in the staging allocation.</summary>
        public nint Bits;
    }

    /// <summary>Calls IDirect3DSurface9::GetDesc.</summary>
    /// <param name="self">The native surface.</param>
    /// <param name="description">The surface format and dimensions.</param>
    /// <returns>The native HRESULT.</returns>
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetDescription(nint self, out SurfaceDesc description);

    /// <summary>Calls IDirect3DSurface9::GetDevice.</summary>
    /// <param name="self">The native surface.</param>
    /// <param name="device">The retained device reference.</param>
    /// <returns>The native HRESULT.</returns>
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetDevice(nint self, out nint device);

    /// <summary>Calls IDirect3DDevice9::CreateOffscreenPlainSurface.</summary>
    /// <param name="self">The native device.</param>
    /// <param name="width">The surface width.</param>
    /// <param name="height">The surface height.</param>
    /// <param name="format">The pixel format.</param>
    /// <param name="pool">The system-memory pool.</param>
    /// <param name="surface">The owned staging surface.</param>
    /// <param name="sharedHandle">A null sharing-handle output pointer.</param>
    /// <returns>The native HRESULT.</returns>
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int CreateOffscreenSurface(
        nint self,
        uint width,
        uint height,
        Format9 format,
        Pool pool,
        out nint surface,
        nint sharedHandle
    );

    /// <summary>Calls IDirect3DDevice9::GetRenderTargetData in this explicit test-only readback path.</summary>
    /// <param name="self">The native device.</param>
    /// <param name="source">The completed shared surface.</param>
    /// <param name="destination">The staging surface.</param>
    /// <returns>The native HRESULT.</returns>
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int CopyRenderTarget(nint self, nint source, nint destination);

    /// <summary>Calls IDirect3DSurface9::LockRect on the system-memory staging surface.</summary>
    /// <param name="self">The staging surface.</param>
    /// <param name="locked">The mapped bytes and row pitch.</param>
    /// <param name="rectangle">A null rectangle for the complete surface.</param>
    /// <param name="flags">The read-only lock flag.</param>
    /// <returns>The native HRESULT.</returns>
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int LockSurface(nint self, out LockedSurface locked, nint rectangle, uint flags);

    /// <summary>Calls IDirect3DSurface9::UnlockRect.</summary>
    /// <param name="self">The staging surface.</param>
    /// <returns>The native HRESULT.</returns>
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int UnlockSurface(nint self);
}
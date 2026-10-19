using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using HelixToolkit.Wpf.SharpDX.Controls;
using Silk.NET.Direct3D9;
using Xunit;

namespace SilkToolkit.Tests;

/// <summary>Exercises actual hardware sharing and reads the D3D9 destination only in explicit test code.</summary>
[Collection(WpfCollection.Name)]
public sealed class D3DImageInteropNativeTests {
    /// <summary>Verifies GPU clears, BGRA channel order, premultiplication, repeated copies, and recreation.</summary>
    /// <returns>The completion of the hardware STA check.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "Wpf")]
    public Task SharedD3D12ColorsReachD3D9SurfaceTest() => StaThread.RunAsync(() => {
            using var window = new HwndSource(
                new HwndSourceParameters(nameof(D3DImageInteropNativeTests)) {
                    Width = 16, Height = 16, WindowStyle = unchecked((int) 0x80000000)
                }
            );
            int selectedIndex;
            long selectedLuid;
            using (var probe = new D3DImageInteropProbe(window.Handle, 8, 6)) {
                selectedIndex = probe.AdapterIndex;
                selectedLuid = probe.AdapterLuid;
                Assert.NotEqual(0, probe.D3D9Surface);
                Assert.True(probe.IsRenderComplete);
                Assert.True(probe.IsCopyComplete);
                Assert.Throws<InvalidOperationException>(probe.BeginCopyToSurface);
                Assert.Throws<ArgumentOutOfRangeException>(() => probe.SubmitColor(float.NaN, 0, 0, 1));
                Assert.Throws<ArgumentOutOfRangeException>(() => probe.SubmitColor(0, 0, 0, 2));

                var firstSlot = CheckColor(probe, 1, 0.5f, 0.25f, 0.5f, [32, 64, 128, 128]);
                var secondSlot = CheckColor(probe, 0, 0, 1, 1, [255, 0, 0, 255]);
                Assert.NotEqual(firstSlot, secondSlot);
                Assert.Equal(firstSlot, CheckColor(probe, 1, 1, 1, 0, [0, 0, 0, 0]));
            }

            using (var resized = new D3DImageInteropProbe(window.Handle, 13, 9, selectedIndex)) {
                Assert.Equal(selectedIndex, resized.AdapterIndex);
                Assert.Equal(selectedLuid, resized.AdapterLuid);
                CheckColor(resized, 0, 1, 0, 1, [0, 255, 0, 255]);
                // Dispose must also quiesce a submitted clear that the caller has not polled or copied.
                resized.SubmitColor(1, 0, 0, 1);
            }

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new D3DImageInteropProbe(window.Handle, 8, 6, int.MaxValue)
            );
        }
    );

    /// <summary>Submits a clear and copies to a detached slot before explicit test-only readback.</summary>
    /// <param name="probe">The native owner under test.</param>
    /// <param name="r">Straight red.</param>
    /// <param name="g">Straight green.</param>
    /// <param name="b">Straight blue.</param>
    /// <param name="a">Alpha.</param>
    /// <param name="expected">The expected premultiplied BGRA bytes.</param>
    /// <returns>The selected destination surface, allowing the test to verify slot alternation.</returns>
    private static nint CheckColor(D3DImageInteropProbe probe, float r, float g, float b, float a, byte[] expected) {
        probe.SubmitColor(r, g, b, a);
        WaitFor(() => probe.IsRenderComplete);
        probe.BeginCopyToSurface();
        Assert.Throws<InvalidOperationException>(probe.BeginCopyToSurface);
        WaitFor(() => probe.IsCopyComplete);
        // Neither slot is attached to WPF in this native test, so both remain available for alternating writes.
        var first = ReadSurfacePixel(probe.D3D9Surface, probe.Width, probe.Height, false);
        var last = ReadSurfacePixel(probe.D3D9Surface, probe.Width, probe.Height, true);
        for (var channel = 0; channel < 4; channel++) {
            Assert.InRange((int) first[channel], expected[channel] - 1, expected[channel] + 1);
            Assert.InRange((int) last[channel], expected[channel] - 1, expected[channel] + 1);
        }

        return probe.D3D9Surface;
    }

    /// <summary>Bounds synchronous waits only in this explicitly requested hardware test.</summary>
    /// <param name="completed">The native nonblocking completion query.</param>
    private static void WaitFor(Func<bool> completed) {
        var timer = Stopwatch.StartNew();
        while (!completed()) {
            Assert.True(timer.Elapsed < TimeSpan.FromSeconds(5), "GPU work did not complete within five seconds.");
            Thread.Sleep(1);
        }
    }

    /// <summary>Reads one pixel using a temporary D3D9 system-memory surface; never used by presentation code.</summary>
    /// <param name="surface">The shared render-target surface.</param>
    /// <param name="width">The expected texture width.</param>
    /// <param name="height">The expected texture height.</param>
    /// <param name="last">Whether to read the bottom-right pixel instead of the top-left pixel.</param>
    /// <returns>The four BGRA bytes.</returns>
    private static byte[] ReadSurfacePixel(nint surface, uint width, uint height, bool last) {
        Marshal.ThrowExceptionForHR(Method<GetDescription>(surface, 12)(surface, out var description));
        Assert.Equal(Format.A8R8G8B8, description.Format);
        Assert.Equal(width, description.Width);
        Assert.Equal(height, description.Height);
        Marshal.ThrowExceptionForHR(Method<GetDevice>(surface, 3)(surface, out var device));
        nint staging = 0;
        try {
            Marshal.ThrowExceptionForHR(
                Method<CreateOffscreenSurface>(device, 36)(
                    device, width, height,
                    Format.A8R8G8B8, Pool.Systemmem, out staging, 0
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
        Format format,
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
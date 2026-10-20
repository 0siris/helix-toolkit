using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using HelixToolkit.SharpDX.Core.Native;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using D9 = Silk.NET.Direct3D9;
using D11 = Silk.NET.Direct3D11;
using D12 = Silk.NET.Direct3D12;
using Dxgi = Silk.NET.DXGI;

namespace HelixToolkit.Wpf.SharpDX.Controls;

/// <summary>Copies a borrowed device's shared BGRA target into two explicitly acknowledged WPF slots.</summary>
/// <remarks>
/// All members are confined to the creating thread. The caller owns the D3D12 device, queue, command context,
/// fence and scene. It must complete D3D12 work and restore Target to COMMON before BeginCopy, and must not
/// reuse the source until copying and publication finish. Pixels must already be premultiplied BGRA8.
/// Only a completed, unpublished surface is exposed. A failed WPF lock leaves that surface and serial retained.
/// ConfirmPublication follows successful publication and unlock; ConfirmDetached follows actual WPF detachment.
/// Native code cannot inspect a partial WPF publication: the caller must retain its actual binding and detach
/// it before teardown. Dispose requires drained D3D12 work and detached WPF surfaces, and never disposes the
/// borrowed device. Recreate for size changes. No WARP, remote-session or cross-adapter support is promised.
/// </remarks>
internal sealed unsafe class D3D12ImageInteropBridge : IDisposable {
    /// <summary>Keeps D3D11 exports loaded for the native references' lifetime.</summary>
    private static readonly D11.D3D11 D3D11Api = D11.D3D11.GetApi(window: null!);

    /// <summary>Keeps D3D9 exports loaded while WPF may access the destination allocations.</summary>
    private static readonly D9.D3D9 D3D9Api = D9.D3D9.GetApi(window: null!);

    /// <summary>Bounds copy completion and explicit teardown independently.</summary>
    private static readonly TimeSpan CopyTimeout = TimeSpan.FromSeconds(5);

    /// <summary>Borrows the actual presentation device; the caller keeps it alive through successful disposal.</summary>
    private readonly SilkD3D12Device device12;

    /// <summary>Records the creating thread for immediate-context and slot access.</summary>
    private readonly int ownerThread = Environment.CurrentManagedThreadId;

    /// <summary>Owns the sole toolkit wrapper and state registration for the shared source.</summary>
    private SilkD3D12Resource? source12;

    /// <summary>Owns the source's single render-target descriptor heap.</summary>
    private SilkD3D12DescriptorHeap? rtvHeap12;

    /// <summary>Owns the source's render-target descriptor until both APIs are quiescent.</summary>
    private SilkD3D12Descriptor? rtv12;

    /// <summary>Retains the borrowed frame target without allocating on access.</summary>
    private D3D12FrameTarget target;

    /// <summary>Retains the actual presentation adapter identity.</summary>
    private long adapterLuid;

    /// <summary>Owns the matching D3D11 device.</summary>
    private ComPtr<D11.ID3D11Device> device11;

    /// <summary>Owns the immediate context used only for GPU copies and their event query.</summary>
    private ComPtr<D11.ID3D11DeviceContext> context11;

    /// <summary>Owns the D3D11 import of the NT-shared source, not another toolkit resource wrapper.</summary>
    private ComPtr<D11.ID3D11Texture2D> source11;

    /// <summary>Owns D3D11 imports of the two D3D9Ex legacy-shared destinations.</summary>
    private readonly ComPtr<D11.ID3D11Texture2D>[] destinations11 = new ComPtr<D11.ID3D11Texture2D>[2];

    /// <summary>Owns the single reusable completion query.</summary>
    private ComPtr<D11.ID3D11Query> copyQuery11;

    /// <summary>Owns D3D9Ex enumeration and device creation.</summary>
    private ComPtr<D9.IDirect3D9Ex> direct3D9;

    /// <summary>Owns the multithreaded, adapter-matched D3D9Ex device used by WPF.</summary>
    private ComPtr<D9.IDirect3DDevice9Ex> device9;

    /// <summary>Owns the two independent legacy-shared BGRA destination textures.</summary>
    private readonly ComPtr<D9.IDirect3DTexture9>[] textures9 = new ComPtr<D9.IDirect3DTexture9>[2];

    /// <summary>Owns level-zero surfaces whose pointers are borrowed during publication.</summary>
    private readonly ComPtr<D9.IDirect3DSurface9>[] surfaces9 = new ComPtr<D9.IDirect3DSurface9>[2];

    /// <summary>Identifies the last successfully published slot, or minus one after confirmed detachment.</summary>
    private int boundSlot = -1;

    /// <summary>Retains the last published slot for a detach/rebind cycle without a new copy.</summary>
    private int lastPublishedSlot = -1;

    /// <summary>Retains the last published serial for same-frame frontbuffer recovery.</summary>
    private ulong lastPublishedSerial;

    /// <summary>Identifies a completed unpublished slot, or minus one.</summary>
    private int readySlot = -1;

    /// <summary>Identifies the current copy target, or minus one before any copy.</summary>
    private int copySlot = -1;

    /// <summary>Identifies the serial submitted to the reusable query.</summary>
    private ulong copySerial;

    /// <summary>Retains the completed unpublished serial, or zero when none is ready.</summary>
    private ulong readySerial;

    /// <summary>Counts accepted copy submissions independently of scene and publication counters.</summary>
    private ulong copyCount;

    /// <summary>Starts the finite pending-copy deadline without allocating a timer.</summary>
    private long copyStarted;

    /// <summary>Indicates that the event query still protects source reads and destination writes.</summary>
    private bool copyPending;

    /// <summary>Requires a real WPF detachment acknowledgment after any copy that might be published.</summary>
    private bool detached = true;

    /// <summary>Prevents new copies after teardown begins or a pending copy exceeds its deadline.</summary>
    private bool stopping;

    /// <summary>Indicates that all owned native resources have been released.</summary>
    private bool disposed;

    /// <summary>Creates one shared source and two destination slots on the borrowed device's actual adapter.</summary>
    /// <param name="device">The live presentation device, retained by the caller through bridge disposal.</param>
    /// <param name="hwnd">A live caller-owned WPF HWND used only to create the D3D9Ex device.</param>
    /// <param name="width">The nonzero physical width.</param>
    /// <param name="height">The nonzero physical height.</param>
    /// <exception cref="ArgumentException">The HWND is zero.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A dimension is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The borrowed device is disposed.</exception>
    /// <exception cref="PlatformNotSupportedException">The actual adapter cannot provide compatible sharing.</exception>
    public D3D12ImageInteropBridge(SilkD3D12Device device, nint hwnd, uint width, uint height) {
        device.AsGuardNotNull();
        ObjectDisposedException.ThrowIf(device.IsDisposed, device);
        if (hwnd == 0) throw new ArgumentException("A live WPF HWND is required.", nameof(hwnd));
        if (width == 0 || width > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(width));
        if (height == 0 || height > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(height));
        device12 = device;
        try {
            InitializeDevices(hwnd);
            InitializeTextures(width, height);
        } catch {
            // Construction submits no GPU work and exposes no WPF pointers.
            ReleaseResources();
            throw;
        }
    }

    /// <summary>Gets the borrowed source and RTV; the caller must observe the transport reuse gate.</summary>
    public D3D12FrameTarget Target {
        get {
            VerifyUsable();
            return target;
        }
    }

    /// <summary>Gets the borrowed D3D12 device's actual adapter LUID.</summary>
    public long AdapterLuid {
        get {
            VerifyThread();
            return adapterLuid;
        }
    }

    /// <summary>Gets whether the single D3D11 event query remains pending, without querying the GPU.</summary>
    public bool IsCopyPending {
        get {
            VerifyAlive();
            return copyPending;
        }
    }

    /// <summary>Polls the event query once without flushing or waiting; initially returns true.</summary>
    /// <exception cref="TimeoutException">A copy exceeded its deadline; all resources remain retained.</exception>
    /// <exception cref="COMException">D3D11 reports a native failure.</exception>
    public bool IsCopyComplete {
        get {
            VerifyAlive();
            if (PollCopyCompletion()) return true;
            if (!stopping && Stopwatch.GetElapsedTime(copyStarted) >= CopyTimeout) {
                stopping = true;
                throw new TimeoutException("D3D11 copy timed out; native resources remain owned.");
            }

            return false;
        }
    }

    /// <summary>Gets whether a completed surface awaits explicit publication acknowledgment.</summary>
    public bool HasReadySurface {
        get {
            VerifyAlive();
            return readySlot >= 0;
        }
    }

    /// <summary>Gets the completed borrowed D3D9 surface, or zero; remains stable until acknowledgment.</summary>
    public nint ReadySurface {
        get {
            VerifyAlive();
            return readySlot < 0
                ? 0
                : (nint) surfaces9[readySlot].Handle;
        }
    }

    /// <summary>Gets the completed unpublished serial, or zero; lock retries do not change it.</summary>
    public ulong ReadySerial {
        get {
            VerifyAlive();
            return readySerial;
        }
    }

    /// <summary>Gets the accepted copy count; failed validation and publication retries do not advance it.</summary>
    public ulong CopyCount {
        get {
            VerifyThread();
            return copyCount;
        }
    }

    /// <summary>Gets whether disposal released all native ownership.</summary>
    public bool IsDisposed {
        get {
            VerifyThread();
            return disposed;
        }
    }

    /// <summary>Gets confirmed D3D11 removal without treating a live-device timeout as permission to release resources.</summary>
    internal bool IsDeviceRemoved {
        get {
            VerifyAlive();
            return device11.Handle->GetDeviceRemovedReason() < 0;
        }
    }

    /// <summary>Copies the completed COMMON source into a destination that is neither bound nor unpublished.</summary>
    /// <param name="frameSerial">A nonzero serial strictly greater than the last accepted copy serial.</param>
    /// <remarks>
    /// D3D12 completion is a caller precondition: this bridge owns no fence and cannot verify it.
    /// No WPF lock is needed for copying. Do not render into the source again before copy and publication finish.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The serial is zero or does not advance.</exception>
    /// <exception cref="InvalidOperationException">The source state or transport ownership forbids a copy.</exception>
    public void BeginCopy(ulong frameSerial) {
        VerifyUsable();
        if (frameSerial == 0 || frameSerial <= copySerial)
            throw new ArgumentOutOfRangeException(nameof(frameSerial));
        if (copyPending || readySlot >= 0)
            throw new InvalidOperationException("A pending or unpublished copy still owns the transport.");
        if (source12!.State != D12.ResourceStates.Common)
            throw new InvalidOperationException("The completed D3D12 source must be in COMMON state.");
        SilkMarshal.ThrowHResult(device11.Handle->GetDeviceRemovedReason());
        copySlot = boundSlot == 0
            ? 1
            : 0;
        copyStarted = Stopwatch.GetTimestamp();
        copyPending = true;
        detached = false;
        context11.Handle->CopyResource(
            (D11.ID3D11Resource*) destinations11[copySlot].Handle,
            (D11.ID3D11Resource*) source11.Handle
        );
        context11.Handle->End((D11.ID3D11Asynchronous*) copyQuery11.Handle);
        context11.Handle->Flush();
        copySerial = frameSerial;
        copyCount++;
    }

    /// <summary>Acknowledges successful WPF publication and unlock, freeing the previously bound slot.</summary>
    /// <param name="frameSerial">The exact ready serial, or last published serial for a copy-free rebind.</param>
    /// <remarks>
    /// A partial publication must not call this method. Retain the actual WPF binding and both allocations
    /// until publication succeeds or actual detachment is confirmed. Native code cannot inspect WPF's binding.
    /// With no pending or ready copy, the last published serial may be acknowledged again after rebinding
    /// its retained surface. A newer ready frame always takes precedence over that older published serial.
    /// </remarks>
    /// <exception cref="InvalidOperationException">No matching ready frame or safe rebind exists.</exception>
    public void ConfirmPublication(ulong frameSerial) {
        VerifyUsable();
        if (readySlot >= 0) {
            if (frameSerial != readySerial)
                throw new InvalidOperationException("Publication must acknowledge the exact ready serial.");
            boundSlot = readySlot;
            lastPublishedSlot = readySlot;
            lastPublishedSerial = readySerial;
            readySlot = -1;
            readySerial = 0;
        } else {
            if (copyPending || lastPublishedSlot < 0 || frameSerial != lastPublishedSerial)
                throw new InvalidOperationException("No completed published surface is available for this rebind.");
            boundSlot = lastPublishedSlot;
        }

        detached = false;
    }

    /// <summary>Acknowledges actual WPF detachment without discarding a completed unpublished frame.</summary>
    /// <remarks>Call only after WPF has detached its actual bound surface, including partial-publication cases.</remarks>
    public void ConfirmDetached() {
        VerifyAlive();
        boundSlot = -1;
        detached = true;
    }

    /// <summary>Drains D3D11 with a finite deadline and releases owned resources after confirmed detachment.</summary>
    /// <remarks>
    /// The caller must first drain all D3D12 work and detach WPF. No D3D12 wait is performed here.
    /// A timeout retains every native reference and disables new copies; keep the bridge and borrowed device
    /// alive and retry Dispose. Confirmed D3D11 device removal permits retirement of its outstanding copy.
    /// </remarks>
    /// <exception cref="InvalidOperationException">WPF detachment has not been acknowledged after copying.</exception>
    /// <exception cref="TimeoutException">D3D11 did not quiesce; native ownership remains retained.</exception>
    public void Dispose() {
        VerifyThread();
        if (disposed) return;
        if (!detached)
            throw new InvalidOperationException("Confirm actual WPF detachment before disposing the bridge.");
        stopping = true;
        var started = Stopwatch.GetTimestamp();
        while (copyPending && device11.Handle->GetDeviceRemovedReason() >= 0 && !PollCopyCompletion()) {
            if (Stopwatch.GetElapsedTime(started) >= CopyTimeout)
                throw new TimeoutException("D3D11 teardown timed out; native resources remain owned.");
            Thread.Sleep(1);
        }

        ReleaseResources();
        disposed = true;
    }

    /// <summary>Creates D3D11 and D3D9Ex devices by the actual borrowed D3D12 device's LUID.</summary>
    /// <param name="hwnd">The caller-owned WPF focus window, never used for native presentation.</param>
    private void InitializeDevices(nint hwnd) {
        var luid = device12.Handle->GetAdapterLuid();
        adapterLuid = unchecked((long) ((ulong) (uint) luid.High << 32 | luid.Low));
        SilkMarshal.ThrowHResult(
            SilkD3D12DeviceFactory.DxgiApi.CreateDXGIFactory2(
                0, out ComPtr<Dxgi.IDXGIFactory4> factory
            )
        );
        using (factory) {
            var adapterGuid = Dxgi.IDXGIAdapter1.Guid;
            Dxgi.IDXGIAdapter1* actualAdapter = null;
            SilkMarshal.ThrowHResult(factory.Handle->EnumAdapterByLuid(luid, &adapterGuid, (void**) &actualAdapter));
            using var actual = new ComPtr<Dxgi.IDXGIAdapter1>(actualAdapter);
            actualAdapter->Release();
            Dxgi.AdapterDesc1 description;
            SilkMarshal.ThrowHResult(actual.Handle->GetDesc1(&description));
            if (!SameLuid(luid, description.AdapterLuid))
                throw new InvalidOperationException("DXGI did not return the borrowed device's actual adapter.");
            if (((Dxgi.AdapterFlag) description.Flags & Dxgi.AdapterFlag.Software) != 0)
                throw new PlatformNotSupportedException("The WPF bridge requires a matching D3D9Ex hardware adapter.");
            D11.ID3D11Device* native11 = null;
            D11.ID3D11DeviceContext* immediate11 = null;
            var level = D3DFeatureLevel.Level110;
            var result = D3D11Api.CreateDevice(
                (Dxgi.IDXGIAdapter*) actual.Handle, D3DDriverType.Unknown, 0,
                (uint) D11.CreateDeviceFlag.BgraSupport, &level, 1, D11.D3D11.SdkVersion,
                &native11, null, &immediate11
            );
            device11 = new ComPtr<D11.ID3D11Device>(native11);
            context11 = new ComPtr<D11.ID3D11DeviceContext>(immediate11);
            if (immediate11 != null) immediate11->Release();
            if (native11 != null) native11->Release();
            SilkMarshal.ThrowHResult(result);
        }

        SilkMarshal.ThrowHResult(D3D9Api.Direct3DCreate9Ex((uint) D9.D3D9.SdkVersion, ref direct3D9));
        var adapter9 = FindD3D9Adapter(luid);
        var present = new D9.PresentParameters {
            Windowed = true,
            HDeviceWindow = hwnd,
            BackBufferWidth = 1,
            BackBufferHeight = 1,
            BackBufferCount = 1,
            SwapEffect = D9.Swapeffect.Discard,
            BackBufferFormat = D9.Format.Unknown
        };
        SilkMarshal.ThrowHResult(
            direct3D9.Handle->CreateDeviceEx(
                adapter9, D9.Devtype.Hal, hwnd,
                (uint) (D9.D3D9.CreateMultithreaded | D9.D3D9.CreateFpuPreserve | D9.D3D9.CreateSoftwareVertexprocessing),
                &present, null, ref device9
            )
        );
    }

    /// <summary>Creates the canonical tracked source, its RTV, D3D11 imports and both D3D9Ex destinations.</summary>
    /// <param name="width">The physical width.</param>
    /// <param name="height">The physical height.</param>
    private void InitializeTextures(uint width, uint height) {
        source12 = device12.CreateSharedBgraRenderTargetTexture2D(width, height);
        rtvHeap12 = device12.CreateDescriptorHeap(D12.DescriptorHeapType.Rtv, 1);
        rtv12 = rtvHeap12.Allocate();
        device12.CreateRenderTargetView(source12, rtv12);
        target = new D3D12FrameTarget(source12, rtv12, width, height, Dxgi.Format.FormatB8G8R8A8Unorm);
        SilkMarshal.ThrowHResult(device11.QueryInterface(out ComPtr<D11.ID3D11Device1> device11v1));
        using (device11v1) {
            void* ntHandle = null;
            try {
                SilkMarshal.ThrowHResult(
                    device12.Handle->CreateSharedHandle(
                        (D12.ID3D12DeviceChild*) source12.Handle, null, 0x10000000, (char*) null, &ntHandle
                    )
                );
                SilkMarshal.ThrowHResult(device11v1.OpenSharedResource1(ntHandle, out source11));
            } finally {
                // Only the D3D12 NT handle is owned by this process; the import retains the allocation.
                if (ntHandle != null) CloseHandle((nint) ntHandle);
            }
        }

        for (var slot = 0; slot < destinations11.Length; slot++) {
            // These D3D9 legacy sharing tokens must never be passed to CloseHandle.
            void* legacyHandle = null;
            SilkMarshal.ThrowHResult(
                device9.Handle->CreateTexture(
                    width, height, 1, (uint) D9.D3D9.UsageRendertarget,
                    D9.Format.A8R8G8B8, D9.Pool.Default, ref textures9[slot], &legacyHandle
                )
            );
            if (legacyHandle == null)
                throw new PlatformNotSupportedException("D3D9Ex did not provide a legacy texture sharing token.");
            SilkMarshal.ThrowHResult(device11.OpenSharedResource(legacyHandle, out destinations11[slot]));
            D11.Texture2DDesc description;
            destinations11[slot].Handle->GetDesc(&description);
            if (description.Format != target.Format || description.Width != width || description.Height != height ||
                description.SampleDesc.Count != 1 || description.SampleDesc.Quality != 0 ||
                description.MipLevels != 1 || description.ArraySize != 1)
                throw new PlatformNotSupportedException("The legacy destination must match the BGRA source.");
            SilkMarshal.ThrowHResult(textures9[slot].Handle->GetSurfaceLevel(0, ref surfaces9[slot]));
        }

        var query = new D11.QueryDesc { Query = D11.Query.Event };
        SilkMarshal.ThrowHResult(device11.Handle->CreateQuery(&query, ref copyQuery11));
    }

    /// <summary>Finds the D3D9Ex adapter by LUID, independently of DXGI enumeration order.</summary>
    /// <param name="luid">The borrowed D3D12 device's actual identity.</param>
    /// <returns>The matching D3D9Ex adapter ordinal.</returns>
    /// <exception cref="PlatformNotSupportedException">No matching D3D9Ex adapter exists.</exception>
    private uint FindD3D9Adapter(Luid luid) {
        for (uint index = 0; index < direct3D9.Handle->GetAdapterCount(); index++) {
            Luid candidate;
            SilkMarshal.ThrowHResult(direct3D9.Handle->GetAdapterLUID(index, &candidate));
            if (SameLuid(luid, candidate)) return index;
        }

        throw new PlatformNotSupportedException("No D3D9Ex adapter matches the borrowed D3D12 device's LUID.");
    }

    /// <summary>Compares every bit of the native adapter identities.</summary>
    /// <param name="left">The requested identity.</param>
    /// <param name="right">The candidate identity.</param>
    /// <returns>Whether both identities match.</returns>
    private static bool SameLuid(Luid left, Luid right) => left.Low == right.Low && left.High == right.High;

    /// <summary>Polls once with DONOTFLUSH, retaining completed output until explicit acknowledgment.</summary>
    /// <returns>Whether the copy has completed or no copy was submitted.</returns>
    private bool PollCopyCompletion() {
        if (!copyPending) return true;
        int complete = 0;
        var result = context11.Handle->GetData((D11.ID3D11Asynchronous*) copyQuery11.Handle, &complete, sizeof(int), 1);
        SilkMarshal.ThrowHResult(result);
        if (result != 0 || complete == 0) return false;
        copyPending = false;
        readySlot = copySlot;
        readySerial = copySerial;
        return true;
    }

    /// <summary>Checks thread ownership, lifetime and the ban on new work after teardown begins.</summary>
    private void VerifyUsable() {
        VerifyAlive();
        ObjectDisposedException.ThrowIf(stopping, this);
    }

    /// <summary>Checks thread ownership and native lifetime while allowing retained-state recovery queries.</summary>
    private void VerifyAlive() {
        VerifyThread();
        ObjectDisposedException.ThrowIf(disposed, this);
        ObjectDisposedException.ThrowIf(device12.IsDisposed, device12);
    }

    /// <summary>Rejects cross-thread access to immediate-context and slot bookkeeping.</summary>
    /// <exception cref="InvalidOperationException">The current thread did not create the bridge.</exception>
    private void VerifyThread() {
        if (Environment.CurrentManagedThreadId != ownerThread)
            throw new InvalidOperationException("Use the image bridge only on its creating thread.");
    }

    /// <summary>Releases ownership only after safe teardown or before construction exposed any resource.</summary>
    private void ReleaseResources() {
        copyQuery11.Dispose();
        foreach (var destination in destinations11) destination.Dispose();
        source11.Dispose();
        context11.Dispose();
        device11.Dispose();
        foreach (var surface in surfaces9) surface.Dispose();
        foreach (var texture in textures9) texture.Dispose();
        device9.Dispose();
        direct3D9.Dispose();
        rtv12?.Dispose();
        rtvHeap12?.Dispose();
        source12?.Dispose();
    }

    /// <summary>Closes an owned NT sharing handle after D3D11 imports it.</summary>
    /// <param name="handle">The NT handle, never a D3D9 legacy token.</param>
    /// <returns>Whether Windows closed the handle.</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool CloseHandle(nint handle);
}

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using HelixToolkit.SharpDX.Core.Native;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using D9 = Silk.NET.Direct3D9;
using D11 = Silk.NET.Direct3D11;
using D12 = Silk.NET.Direct3D12;
using Dxgi = Silk.NET.DXGI;

namespace HelixToolkit.Wpf.SharpDX.Controls;

/// <summary>Proves GPU-only D3D12 to D3D11 to D3D9Ex BGRA sharing without a scene renderer.</summary>
/// <remarks>
/// This experimental owner is thread-affine. Submit a color, poll render completion, and begin a copy into the
/// detached destination slot. Poll copy completion, then lock D3DImage, set its back buffer to D3D9Surface, dirty it,
/// and unlock. Fully publish that slot before beginning another copy: the previous slot must have been detached.
/// The attached slot remains immutable even during WPF software snapshots. Detach the back buffer under its lock
/// before disposal. Recreate this owner to change size.
/// No WARP, remote-session, device-recovery, or cross-adapter support is implied.
/// </remarks>
internal sealed unsafe class D3DImageInteropProbe : IDisposable {
    /// <summary>Keeps D3D11 exports loaded while their COM objects remain alive.</summary>
    private static readonly D11.D3D11 D3D11Api = D11.D3D11.GetApi(window: null!);

    /// <summary>Keeps D3D9 exports loaded while WPF can access their COM objects.</summary>
    private static readonly D9.D3D9 D3D9Api = D9.D3D9.GetApi(window: null!);

    /// <summary>Bounds explicit teardown waits; ordinary polling never waits.</summary>
    private static readonly TimeSpan TeardownTimeout = TimeSpan.FromSeconds(5);

    /// <summary>Owns the existing toolkit D3D12 device wrapper.</summary>
    private SilkD3D12Device? device12;

    /// <summary>Owns the clear submission queue.</summary>
    private SilkD3D12CommandQueue? queue12;

    /// <summary>Owns the reusable clear commands and allocator.</summary>
    private SilkD3D12CommandContext? commands12;

    /// <summary>Protects the clear commands and the D3D11 source texture.</summary>
    private SilkD3D12Fence? fence12;

    /// <summary>Owns the actual D3D12 shared render target.</summary>
    private ComPtr<D12.ID3D12Resource> renderTarget12;

    /// <summary>Owns the single render-target descriptor.</summary>
    private ComPtr<D12.ID3D12DescriptorHeap> rtvHeap12;

    /// <summary>Owns the D3D11 device on the actual D3D12 adapter.</summary>
    private ComPtr<D11.ID3D11Device> device11;

    /// <summary>Owns the immediate copy context.</summary>
    private ComPtr<D11.ID3D11DeviceContext> context11;

    /// <summary>Owns the D3D11 view of the NT-shared D3D12 texture.</summary>
    private ComPtr<D11.ID3D11Texture2D> source11;

    /// <summary>Owns D3D11 views of the two separate legacy-shared D3D9 destination allocations.</summary>
    private readonly ComPtr<D11.ID3D11Texture2D>[] destinations11 = new ComPtr<D11.ID3D11Texture2D>[2];

    /// <summary>Confirms completion of all D3D11 writes before WPF unlocks.</summary>
    private ComPtr<D11.ID3D11Query> copyQuery11;

    /// <summary>Owns D3D9Ex adapter enumeration and device creation.</summary>
    private ComPtr<D9.IDirect3D9Ex> direct3D9;

    /// <summary>Owns the multithreaded D3D9Ex device consumed by WPF.</summary>
    private ComPtr<D9.IDirect3DDevice9Ex> device9;

    /// <summary>Owns the two alternating legacy-shared destination allocations.</summary>
    private readonly ComPtr<D9.IDirect3DTexture9>[] textures9 = new ComPtr<D9.IDirect3DTexture9>[2];

    /// <summary>Owns both level-zero surfaces borrowed by the caller.</summary>
    private readonly ComPtr<D9.IDirect3DSurface9>[] surfaces9 = new ComPtr<D9.IDirect3DSurface9>[2];

    /// <summary>Identifies the current copy target; minus one selects slot zero for the first copy.</summary>
    private int copySlot = -1;

    /// <summary>Records the creating thread to serialize reuse of the immediate context.</summary>
    private readonly int ownerThread = Environment.CurrentManagedThreadId;

    /// <summary>Records the latest submitted D3D12 fence value, or zero before the first clear.</summary>
    private ulong renderFenceValue;

    /// <summary>Indicates that a clear is available for one copy submission.</summary>
    private bool hasFrame;

    /// <summary>Indicates that the D3D11 completion query is in flight.</summary>
    private bool copyPending;

    /// <summary>Prevents further submissions after teardown has started, including a timed-out teardown.</summary>
    private bool stopping;

    /// <summary>Indicates that every native reference has been released.</summary>
    private bool disposed;

    /// <summary>Creates the shared source, two destination slots, and matching native devices.</summary>
    /// <param name="hwnd">A live caller-owned HWND, used only for D3D9Ex device creation, never presentation.</param>
    /// <param name="width">The nonzero texture width.</param>
    /// <param name="height">The nonzero texture height.</param>
    /// <param name="adapterIndex">The explicit DXGI enumeration index, or minus one for toolkit selection.</param>
    /// <exception cref="ArgumentOutOfRangeException">A size or adapter index is invalid.</exception>
    /// <exception cref="ArgumentException">The HWND is zero.</exception>
    /// <exception cref="PlatformNotSupportedException">The adapters cannot provide the required sharing.</exception>
    public D3DImageInteropProbe(nint hwnd, uint width, uint height, int adapterIndex = -1) {
        if (hwnd == 0) throw new ArgumentException("A live HWND is required.", nameof(hwnd));
        if (width == 0 || width > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(width));
        if (height == 0 || height > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(height));
        if (adapterIndex < -1) throw new ArgumentOutOfRangeException(nameof(adapterIndex));
        Width = width;
        Height = height;
        try {
            InitializeDevices(hwnd, adapterIndex);
            InitializeTextures();
        } catch {
            // Initialization submits no GPU commands, so partial construction can release immediately.
            ReleaseResources();
            throw;
        }
    }

    /// <summary>Gets the texture width.</summary>
    public uint Width { get; }

    /// <summary>Gets the texture height.</summary>
    public uint Height { get; }

    /// <summary>Gets the actual DXGI enumeration index selected for this probe.</summary>
    public int AdapterIndex { get; private set; }

    /// <summary>Gets the actual D3D12 adapter LUID as a signed 64-bit identifier.</summary>
    public long AdapterLuid { get; private set; }

    /// <summary>Gets the borrowed IDirect3DSurface9 pointer for the selected copy target.</summary>
    /// <remarks>Publish only after copy completion. Before the first copy, this returns slot zero.</remarks>
    public nint D3D9Surface {
        get {
            VerifyUsable();
            return (nint) surfaces9[Math.Max(copySlot, 0)].Handle;
        }
    }

    /// <summary>Gets whether D3D12 has completed its most recent clear, without waiting.</summary>
    /// <exception cref="COMException">The D3D12 device was removed.</exception>
    public bool IsRenderComplete {
        get {
            VerifyUsable();
            var completed = fence12!.CompletedValue;
            if (completed == ulong.MaxValue)
                SilkMarshal.ThrowHResult(device12!.Handle->GetDeviceRemovedReason());
            return completed >= renderFenceValue;
        }
    }

    /// <summary>Gets whether the D3D11 copy has completed, without flushing or waiting.</summary>
    /// <remarks>Do not publish the selected destination or start another copy while this returns false.</remarks>
    public bool IsCopyComplete {
        get {
            VerifyUsable();
            return PollCopyCompletion();
        }
    }

    /// <summary>Submits one D3D12 clear, converting straight RGBA to premultiplied BGRA storage.</summary>
    /// <param name="r">The finite red component between zero and one.</param>
    /// <param name="g">The finite green component between zero and one.</param>
    /// <param name="b">The finite blue component between zero and one.</param>
    /// <param name="a">The finite alpha component between zero and one.</param>
    /// <exception cref="InvalidOperationException">Previous render or copy work is still in flight.</exception>
    public void SubmitColor(float r, float g, float b, float a) {
        VerifyUsable();
        ValidateColor(r, nameof(r));
        ValidateColor(g, nameof(g));
        ValidateColor(b, nameof(b));
        ValidateColor(a, nameof(a));
        if (!IsRenderComplete || !PollCopyCompletion())
            throw new InvalidOperationException("Previous GPU work still owns the reusable resources.");

        commands12!.Reset();
        Transition(D12.ResourceStates.Common, D12.ResourceStates.RenderTarget);
        float* color = stackalloc float[4] { r * a, g * a, b * a, a };
        commands12.CommandList.Handle->ClearRenderTargetView(
            rtvHeap12.Handle->GetCPUDescriptorHandleForHeapStart(),
            color, 0, null
        );
        Transition(D12.ResourceStates.RenderTarget, D12.ResourceStates.Common);
        commands12!.Close();
        // A failed signal must not allow allocator reuse; teardown can signal again to cover this submission.
        hasFrame = false;
        stopping = true;
        queue12!.Execute(commands12);
        renderFenceValue = queue12!.Signal(fence12!);
        stopping = false;
        hasFrame = true;
    }

    /// <summary>Starts the GPU copy into the next detached destination, alternating slots from slot zero.</summary>
    /// <remarks>
    /// The caller must ensure the target is detached from D3DImage. After IsCopyComplete becomes true, publish
    /// D3D9Surface under the WPF lock and unlock before the next BeginCopyToSurface. That publication detaches
    /// the old slot, which becomes the next copy target. Native code cannot inspect this caller precondition.
    /// No WPF lock is needed while the GPU copies or while the caller polls completion.
    /// </remarks>
    /// <exception cref="InvalidOperationException">No completed, unconsumed clear is available.</exception>
    public void BeginCopyToSurface() {
        VerifyUsable();
        if (!hasFrame || !IsRenderComplete || !PollCopyCompletion())
            throw new InvalidOperationException("A completed, unconsumed clear is required before copying.");
        copyPending = true;
        hasFrame = false;
        copySlot = (copySlot + 1) % destinations11.Length;
        context11.Handle->CopyResource(
            (D11.ID3D11Resource*) destinations11[copySlot].Handle,
            (D11.ID3D11Resource*) source11.Handle
        );
        context11.Handle->End((D11.ID3D11Asynchronous*) copyQuery11.Handle);
        context11.Handle->Flush();
    }

    /// <summary>Quiesces GPU work with a bounded teardown wait, then releases all native ownership.</summary>
    /// <remarks>
    /// Detach the caller's D3DImage first. A timeout retains all native references and disables further work;
    /// Dispose may be retried. This method is an explicit teardown path, never a normal dispatcher tick.
    /// </remarks>
    /// <exception cref="TimeoutException">GPU work did not quiesce; native ownership remains retained.</exception>
    public void Dispose() {
        VerifyThread();
        if (disposed) return;
        stopping = true;
        if (device12 is not null && queue12 is not null && fence12 is not null &&
            device12.Handle->GetDeviceRemovedReason() >= 0)
            fence12.Wait(queue12.Signal(fence12), TeardownTimeout);

        var timer = Stopwatch.StartNew();
        while (copyPending && device11.Handle->GetDeviceRemovedReason() >= 0 && !PollCopyCompletion()) {
            if (timer.Elapsed >= TeardownTimeout)
                throw new TimeoutException("D3D11 copy did not complete; native resources remain owned.");
            Thread.Sleep(1);
        }

        ReleaseResources();
        disposed = true;
    }

    /// <summary>Creates the devices and verifies both DXGI and D3D9Ex identities against the D3D12 LUID.</summary>
    /// <param name="hwnd">The D3D9Ex focus window.</param>
    /// <param name="adapterIndex">The requested DXGI adapter, or minus one.</param>
    private void InitializeDevices(nint hwnd, int adapterIndex) {
        SilkMarshal.ThrowHResult(
            SilkD3D12DeviceFactory.DxgiApi.CreateDXGIFactory2(
                0,
                out ComPtr<Dxgi.IDXGIFactory4> factory
            )
        );
        using (factory) {
            if (adapterIndex == -1)
                device12 = SilkD3D12DeviceFactory.CreateDefault();
            else {
                ComPtr<Dxgi.IDXGIAdapter1> selected = default;
                var result = factory.Handle->EnumAdapters1((uint) adapterIndex, ref selected);
                using (selected) {
                    if (result == unchecked((int) 0x887A0002))
                        throw new ArgumentOutOfRangeException(nameof(adapterIndex), "No such DXGI adapter exists.");
                    SilkMarshal.ThrowHResult(result);
                    Dxgi.AdapterDesc1 description;
                    SilkMarshal.ThrowHResult(selected.Handle->GetDesc1(&description));
                    if (((Dxgi.AdapterFlag) description.Flags & Dxgi.AdapterFlag.Software) != 0)
                        throw new PlatformNotSupportedException("The probe requires a D3D9Ex hardware adapter.");
                    SilkMarshal.ThrowHResult(
                        SilkD3D12DeviceFactory.Api.CreateDevice(
                            selected,
                            D3DFeatureLevel.Level110, out ComPtr<D12.ID3D12Device> nativeDevice
                        )
                    );
                    device12 = new SilkD3D12Device(nativeDevice, SilkFeatureLevel.Level110);
                }
            }

            var luid = device12.Handle->GetAdapterLuid();
            AdapterLuid = unchecked((long) ((ulong) (uint) luid.High << 32 | luid.Low));
            var adapterGuid = Dxgi.IDXGIAdapter1.Guid;
            Dxgi.IDXGIAdapter1* actualAdapter = null;
            SilkMarshal.ThrowHResult(factory.Handle->EnumAdapterByLuid(luid, &adapterGuid, (void**) &actualAdapter));
            using var actual = new ComPtr<Dxgi.IDXGIAdapter1>(actualAdapter);
            Dxgi.AdapterDesc1 actualDescription;
            SilkMarshal.ThrowHResult(actual.Handle->GetDesc1(&actualDescription));
            if (!SameLuid(luid, actualDescription.AdapterLuid))
                throw new InvalidOperationException("DXGI did not return the actual D3D12 adapter.");

            AdapterIndex = FindDxgiAdapter(factory, luid);
            if (adapterIndex >= 0 && AdapterIndex != adapterIndex)
                throw new InvalidOperationException("The configured DXGI adapter was not used by D3D12.");

            D11.ID3D11Device* native11 = null;
            D11.ID3D11DeviceContext* immediate11 = null;
            var level = D3DFeatureLevel.Level110;
            var create11 = D3D11Api.CreateDevice(
                (Dxgi.IDXGIAdapter*) actual.Handle, D3DDriverType.Unknown, 0,
                (uint) D11.CreateDeviceFlag.BgraSupport, &level, 1, D11.D3D11.SdkVersion,
                &native11, null, &immediate11
            );
            device11 = new ComPtr<D11.ID3D11Device>(native11);
            context11 = new ComPtr<D11.ID3D11DeviceContext>(immediate11);
            SilkMarshal.ThrowHResult(create11);

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

        queue12 = device12.CreateCommandQueue();
        commands12 = device12.CreateCommandContext();
        fence12 = device12.CreateFence();
    }

    /// <summary>Creates the NT-shared render texture and two separate legacy-shared WPF destinations.</summary>
    private void InitializeTextures() {
        var heap = new D12.HeapProperties { Type = D12.HeapType.Default, CreationNodeMask = 1, VisibleNodeMask = 1 };
        var description = new D12.ResourceDesc {
            Dimension = D12.ResourceDimension.Texture2D,
            Width = Width,
            Height = Height,
            DepthOrArraySize = 1,
            MipLevels = 1,
            Format = Dxgi.Format.FormatB8G8R8A8Unorm,
            SampleDesc = new Dxgi.SampleDesc(1, 0),
            Layout = D12.TextureLayout.LayoutUnknown,
            Flags = D12.ResourceFlags.AllowRenderTarget | D12.ResourceFlags.AllowSimultaneousAccess
        };
        SilkMarshal.ThrowHResult(
            device12!.NativeDevice.CreateCommittedResource(
                in heap, D12.HeapFlags.Shared,
                in description, D12.ResourceStates.Common, null, out renderTarget12
            )
        );
        var rtvDescription = new D12.DescriptorHeapDesc { Type = D12.DescriptorHeapType.Rtv, NumDescriptors = 1 };
        SilkMarshal.ThrowHResult(device12.NativeDevice.CreateDescriptorHeap(in rtvDescription, out rtvHeap12));
        device12.Handle->CreateRenderTargetView(
            renderTarget12.Handle, null,
            rtvHeap12.Handle->GetCPUDescriptorHandleForHeapStart()
        );

        SilkMarshal.ThrowHResult(device11.QueryInterface(out ComPtr<D11.ID3D11Device1> device11v1));
        using (device11v1) {
            void* ntHandle = null;
            try {
                // This is a real NT handle. D3D11 retains the allocation after opening it.
                SilkMarshal.ThrowHResult(
                    device12.Handle->CreateSharedHandle(
                        (D12.ID3D12DeviceChild*) renderTarget12.Handle,
                        null, 0x10000000, (char*) null, &ntHandle
                    )
                );
                SilkMarshal.ThrowHResult(device11v1.OpenSharedResource1(ntHandle, out source11));
            } finally {
                if (ntHandle != null) CloseHandle((nint) ntHandle);
            }
        }

        // D3D9 owns this allocation; its legacy sharing token is not an NT handle and must never be closed.
        // https://learn.microsoft.com/windows/win32/api/d3d11/nf-d3d11-id3d11device-opensharedresource
        for (var slot = 0; slot < destinations11.Length; slot++) {
            void* legacyHandle = null;
            SilkMarshal.ThrowHResult(
                device9.Handle->CreateTexture(
                    Width, Height, 1, (uint) D9.D3D9.UsageRendertarget,
                    D9.Format.A8R8G8B8, D9.Pool.Default, ref textures9[slot], &legacyHandle
                )
            );
            if (legacyHandle == null)
                throw new PlatformNotSupportedException("D3D9Ex did not provide a legacy texture sharing token.");
            SilkMarshal.ThrowHResult(device11.OpenSharedResource(legacyHandle, out destinations11[slot]));
            D11.Texture2DDesc destinationDescription;
            destinations11[slot].Handle->GetDesc(&destinationDescription);
            if (destinationDescription.Format != description.Format || destinationDescription.Width != Width ||
                destinationDescription.Height != Height || destinationDescription.SampleDesc.Count != 1 ||
                destinationDescription.MipLevels != 1)
                throw new PlatformNotSupportedException("The legacy destination is not a matching single-sample BGRA texture.");
            SilkMarshal.ThrowHResult(textures9[slot].Handle->GetSurfaceLevel(0, ref surfaces9[slot]));
        }

        var query = new D11.QueryDesc { Query = D11.Query.Event };
        SilkMarshal.ThrowHResult(device11.Handle->CreateQuery(&query, ref copyQuery11));
    }

    /// <summary>Finds a DXGI index by identity rather than equating it with a D3D9 index.</summary>
    /// <param name="factory">The DXGI factory.</param>
    /// <param name="luid">The actual D3D12 adapter identity.</param>
    /// <returns>The matching DXGI index.</returns>
    private static int FindDxgiAdapter(ComPtr<Dxgi.IDXGIFactory4> factory, Luid luid) {
        for (uint index = 0;; index++) {
            ComPtr<Dxgi.IDXGIAdapter1> candidate = default;
            var result = factory.Handle->EnumAdapters1(index, ref candidate);
            using (candidate) {
                if (result == unchecked((int) 0x887A0002)) break;
                SilkMarshal.ThrowHResult(result);
                Dxgi.AdapterDesc1 description;
                SilkMarshal.ThrowHResult(candidate.Handle->GetDesc1(&description));
                if (SameLuid(luid, description.AdapterLuid)) return checked((int) index);
            }
        }

        throw new PlatformNotSupportedException("The actual D3D12 adapter is missing from DXGI enumeration.");
    }

    /// <summary>Matches the actual D3D12 LUID against D3D9Ex adapter LUIDs.</summary>
    /// <param name="luid">The actual D3D12 adapter identity.</param>
    /// <returns>The matching D3D9Ex adapter index.</returns>
    private uint FindD3D9Adapter(Luid luid) {
        for (uint index = 0; index < direct3D9.Handle->GetAdapterCount(); index++) {
            Luid candidate;
            SilkMarshal.ThrowHResult(direct3D9.Handle->GetAdapterLUID(index, &candidate));
            if (SameLuid(luid, candidate)) return index;
        }

        throw new PlatformNotSupportedException("No D3D9Ex adapter matches the actual D3D12 adapter LUID.");
    }

    /// <summary>Compares both halves of native adapter identifiers.</summary>
    /// <param name="left">The first identity.</param>
    /// <param name="right">The second identity.</param>
    /// <returns>Whether both identities are equal.</returns>
    private static bool SameLuid(Luid left, Luid right) => left.Low == right.Low && left.High == right.High;

    /// <summary>Records an explicit state transition, returning the shared source to COMMON before D3D11 use.</summary>
    /// <param name="before">The current resource state.</param>
    /// <param name="after">The required resource state.</param>
    private void Transition(D12.ResourceStates before, D12.ResourceStates after) {
        var barrier = new D12.ResourceBarrier {
            Type = D12.ResourceBarrierType.Transition,
            Transition = new D12.ResourceTransitionBarrier {
                PResource = renderTarget12.Handle, Subresource = uint.MaxValue, StateBefore = before, StateAfter = after
            }
        };
        commands12!.CommandList.Handle->ResourceBarrier(1, &barrier);
    }

    /// <summary>Reads one D3D11 event-query result without flushing or blocking.</summary>
    /// <returns>Whether no destination write remains pending.</returns>
    private bool PollCopyCompletion() {
        if (!copyPending) return true;
        int complete = 0;
        // D3D11_ASYNC_GETDATA_DONOTFLUSH = 1; BeginCopyToSurface already flushed the submitted event.
        var result = context11.Handle->GetData((D11.ID3D11Asynchronous*) copyQuery11.Handle, &complete, sizeof(int), 1);
        SilkMarshal.ThrowHResult(result);
        if (result != 0 || complete == 0) return false;
        copyPending = false;
        return true;
    }

    /// <summary>Checks thread ownership and rejects operations after teardown begins.</summary>
    private void VerifyUsable() {
        VerifyThread();
        ObjectDisposedException.ThrowIf(disposed || stopping, this);
    }

    /// <summary>Rejects concurrent access to the reusable native commands and immediate context.</summary>
    private void VerifyThread() {
        if (Environment.CurrentManagedThreadId != ownerThread)
            throw new InvalidOperationException("Use the probe only on its creating thread.");
    }

    /// <summary>Validates one straight-alpha input component.</summary>
    /// <param name="value">The color or alpha value.</param>
    /// <param name="name">The caller's parameter name.</param>
    private static void ValidateColor(float value, string name) {
        if (!float.IsFinite(value) || value < 0 || value > 1)
            throw new ArgumentOutOfRangeException(name, "Color components must be finite and between zero and one.");
    }

    /// <summary>Releases native references only after quiescence or before any submission during construction.</summary>
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
        rtvHeap12.Dispose();
        renderTarget12.Dispose();
        commands12?.Dispose();
        fence12?.Dispose();
        queue12?.Dispose();
        device12?.Dispose();
    }

    /// <summary>Closes an owned NT sharing handle after the importing device has opened it.</summary>
    /// <param name="handle">The NT handle; never a D3D9 legacy sharing token.</param>
    /// <returns>Whether Windows closed the handle.</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool CloseHandle(nint handle);
}
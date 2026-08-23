/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using SilkD3D12ResourcePtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D12.ID3D12Resource>;
using SilkDxgiFactory4Ptr = Silk.NET.Core.Native.ComPtr<Silk.NET.DXGI.IDXGIFactory4>;
using SilkDxgiSwapChain3Ptr = Silk.NET.Core.Native.ComPtr<Silk.NET.DXGI.IDXGISwapChain3>;

namespace HelixToolkit.SharpDX.Core.Native;

/// <summary>
///     Owns a three-buffer Direct3D 12 flip-model swap chain and its render-target views.
/// </summary>
public sealed unsafe class SilkD3D12SwapChain : IDisposable {
    /// <summary>
    ///     Number of buffers used by the presentation ring.
    /// </summary>
    public const uint BufferCount = 3;

    private readonly SilkD3D12Device device;
    private readonly SilkD3D12DescriptorHeap renderTargetHeap;
    private SilkDxgiFactory4Ptr factory;
    private SilkDxgiSwapChain3Ptr swapChain;
    private SilkD3D12Resource[] backBuffers = [];
    private SilkD3D12Descriptor[] renderTargetViews = [];

    /// <summary>
    ///     Creates a DX12 swap chain for an existing HWND and direct command queue.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="queue">The direct command queue.</param>
    /// <param name="window">The target window handle.</param>
    /// <param name="width">The initial width in pixels.</param>
    /// <param name="height">The initial height in pixels.</param>
    /// <param name="format">The back-buffer format.</param>
    public SilkD3D12SwapChain(
        SilkD3D12Device device,
        SilkD3D12CommandQueue queue,
        nint window,
        uint width,
        uint height,
        Format format = Format.FormatR8G8B8A8Unorm
    ) {
        this.device = device ?? throw new ArgumentNullException(nameof(device));
        queue.AssertArgumentNotNull();
        if (window == 0) throw new ArgumentException("A valid HWND is required.", nameof(window));
        if (width == 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height == 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (format == Format.FormatUnknown) throw new ArgumentOutOfRangeException(nameof(format));

        Window = window;
        Width = width;
        Height = height;
        Format = format;
        renderTargetHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, (int) BufferCount);

        try {
            CreateNativeSwapChain(queue);
            CreateBackBuffers();
        } catch {
            Dispose();
            throw;
        }
    }

    /// <summary>
    ///     Gets the target window handle.
    /// </summary>
    public nint Window { get; }

    /// <summary>
    ///     Gets the current back-buffer width.
    /// </summary>
    public uint Width { get; private set; }

    /// <summary>
    ///     Gets the current back-buffer height.
    /// </summary>
    public uint Height { get; private set; }

    /// <summary>
    ///     Gets the back-buffer format.
    /// </summary>
    public Format Format { get; }

    /// <summary>
    ///     Gets the current native back-buffer index.
    /// </summary>
    public uint CurrentBackBufferIndex {
        get {
            ThrowIfDisposed();
            return swapChain.GetCurrentBackBufferIndex();
        }
    }

    /// <summary>
    ///     Gets the current back-buffer resource owned by this swap chain.
    /// </summary>
    public SilkD3D12Resource CurrentBackBuffer => backBuffers[CurrentBackBufferIndex];

    /// <summary>
    ///     Gets the current render-target-view descriptor owned by this swap chain.
    /// </summary>
    public SilkD3D12Descriptor CurrentRenderTargetView => renderTargetViews[CurrentBackBufferIndex];

    /// <summary>
    ///     Gets whether the swap chain and its buffers have been disposed.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    ///     Presents the current buffer and advances the flip-model ring.
    /// </summary>
    /// <param name="syncInterval">The vertical-sync interval.</param>
    public void Present(uint syncInterval = 1) {
        ThrowIfDisposed();
        if (CurrentBackBuffer.State != ResourceStates.Present)
            throw new InvalidOperationException("The current back buffer must be in present state.");

        SilkMarshal.ThrowHResult(swapChain.Present(syncInterval, 0));
    }

    /// <summary>
    ///     Resizes all back buffers after the caller has waited for outstanding GPU work.
    /// </summary>
    /// <param name="width">The new width in pixels.</param>
    /// <param name="height">The new height in pixels.</param>
    public void Resize(uint width, uint height) {
        ThrowIfDisposed();
        if (width == 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height == 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (width == Width && height == Height) return;

        ReleaseBackBuffers();
        SilkMarshal.ThrowHResult(swapChain.ResizeBuffers(BufferCount,
            width,
            height,
            Format.FormatUnknown,
            0));
        Width = width;
        Height = height;
        CreateBackBuffers();
    }

    /// <summary>
    ///     Releases the back buffers, descriptor heap, swap chain, and factory.
    /// </summary>
    public void Dispose() {
        if (IsDisposed) return;

        ReleaseBackBuffers();
        renderTargetHeap.Dispose();
        swapChain.Dispose();
        factory.Dispose();
        IsDisposed = true;
    }

    /// <summary>
    ///     Creates the native flip-discard swap chain.
    /// </summary>
    /// <param name="queue">The presentation command queue.</param>
    private void CreateNativeSwapChain(SilkD3D12CommandQueue queue) {
        SilkMarshal.ThrowHResult(SilkD3D12DeviceFactory.DxgiApi.CreateDXGIFactory2(0, out factory));
        var description = new SwapChainDesc1 {
            Width = Width,
            Height = Height,
            Format = Format,
            Stereo = false,
            SampleDesc = new SampleDesc(1, 0),
            BufferUsage = 0x20,
            BufferCount = BufferCount,
            Scaling = Silk.NET.DXGI.Scaling.Stretch,
            SwapEffect = Silk.NET.DXGI.SwapEffect.FlipDiscard,
            AlphaMode = AlphaMode.Unspecified,
            Flags = 0
        };
        IDXGISwapChain1* baseSwapChain = null;
        SilkMarshal.ThrowHResult(factory.CreateSwapChainForHwnd((IUnknown*) queue.Handle,
            Window,
            in description,
            (SwapChainFullscreenDesc*) null,
            (IDXGIOutput*) null,
            &baseSwapChain));

        try {
            var swapChainGuid = IDXGISwapChain3.Guid;
            IDXGISwapChain3* swapChain3 = null;
            SilkMarshal.ThrowHResult(baseSwapChain->QueryInterface(&swapChainGuid, (void**) &swapChain3));
            swapChain = new SilkDxgiSwapChain3Ptr(swapChain3);
            swapChain3->Release();
        } finally {
            baseSwapChain->Release();
        }
    }

    /// <summary>
    ///     Acquires every native back buffer and creates its render-target view.
    /// </summary>
    private void CreateBackBuffers() {
        backBuffers = new SilkD3D12Resource[BufferCount];
        renderTargetViews = new SilkD3D12Descriptor[BufferCount];
        var resourceDescription = new ResourceDesc {
            Dimension = ResourceDimension.Texture2D,
            Width = Width,
            Height = Height,
            DepthOrArraySize = 1,
            MipLevels = 1,
            Format = Format,
            SampleDesc = new SampleDesc(1, 0),
            Layout = TextureLayout.LayoutUnknown,
            Flags = ResourceFlags.AllowRenderTarget
        };

        for (uint index = 0; index < BufferCount; index++) {
            ID3D12Resource* resource = null;
            var resourceGuid = ID3D12Resource.Guid;
            SilkMarshal.ThrowHResult(swapChain.GetBuffer(index, &resourceGuid, (void**) &resource));
            var nativeResource = new SilkD3D12ResourcePtr(resource);
            resource->Release();

            backBuffers[index] = new SilkD3D12Resource(nativeResource,
                resourceDescription,
                0,
                HeapType.Default,
                ResourceStates.Present,
                device.ResourceStates);
            renderTargetViews[index] = renderTargetHeap.Allocate();
            device.CreateRenderTargetView(backBuffers[index], renderTargetViews[index]);
        }
    }

    /// <summary>
    ///     Releases all native back-buffer references before resize or disposal.
    /// </summary>
    private void ReleaseBackBuffers() {
        foreach (var descriptor in renderTargetViews) descriptor?.Dispose();
        foreach (var resource in backBuffers) resource?.Dispose();
        renderTargetViews = [];
        backBuffers = [];
    }

    /// <summary>
    ///     Throws when the native swap chain is unavailable.
    /// </summary>
    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(IsDisposed, this);
}

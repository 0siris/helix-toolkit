/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace HelixToolkit.SharpDX.Core.Native;

/// <summary>
///     Owns the typeless depth texture and typed views used by one Direct3D 12 shadow map.
/// </summary>
internal sealed class SilkD3D12ShadowMap : IDisposable {
    /// <summary>
    ///     The device used to create replacement textures and views.
    /// </summary>
    private readonly SilkD3D12Device device;

    /// <summary>
    ///     The private non-shader-visible DSV heap.
    /// </summary>
    private readonly SilkD3D12DescriptorHeap depthStencilHeap;

    /// <summary>
    ///     The stable DSV descriptor allocation.
    /// </summary>
    private readonly SilkD3D12Descriptor depthStencilView;

    /// <summary>
    ///     The current size-dependent typeless depth texture.
    /// </summary>
    private SilkD3D12Resource resource;

    /// <summary>
    ///     Initializes one shadow map at the requested size.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="width">The shadow-map width.</param>
    /// <param name="height">The shadow-map height.</param>
    internal SilkD3D12ShadowMap(SilkD3D12Device device, uint width, uint height) {
        device.GuardNotNull();
        this.device = device;
        depthStencilHeap = device.CreateDescriptorHeap(DescriptorHeapType.Dsv, 1);
        depthStencilView = depthStencilHeap.Allocate();
        try {
            resource = CreateResource(width, height);
        } catch {
            depthStencilView.Dispose();
            depthStencilHeap.Dispose();
            throw;
        }
    }

    /// <summary>
    ///     Gets the current typeless depth resource.
    /// </summary>
    internal SilkD3D12Resource Resource {
        get {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            return resource;
        }
    }

    /// <summary>
    ///     Gets the shadow-map width.
    /// </summary>
    internal uint Width => Resource.Description.Width > uint.MaxValue
        ? throw new InvalidOperationException("The shadow-map width exceeds the managed contract.")
        : (uint) Resource.Description.Width;

    /// <summary>
    ///     Gets the shadow-map height.
    /// </summary>
    internal uint Height => Resource.Description.Height;

    /// <summary>
    ///     Gets whether every native shadow-map resource has been released.
    /// </summary>
    internal bool IsDisposed { get; private set; }

    /// <summary>
    ///     Replaces the depth texture when its dimensions change.
    /// </summary>
    /// <param name="width">The requested width.</param>
    /// <param name="height">The requested height.</param>
    internal void Resize(uint width, uint height) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (width == Width && height == Height) return;
        var replacement = CreateResource(width, height);
        resource.Dispose();
        resource = replacement;
    }

    /// <summary>
    ///     Transitions, clears, and binds the depth-only target for shadow recording.
    /// </summary>
    /// <param name="context">The open command context.</param>
    internal void BeginDepthWrite(SilkD3D12CommandContext context) {
        context.GuardNotNull();
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        context.Transition(resource, ResourceStates.DepthWrite);
        context.ClearDepthStencil(resource, depthStencilView);
        context.SetDepthStencil(depthStencilView);
        context.SetViewport(Width, Height);
    }

    /// <summary>
    ///     Transitions the completed shadow depth to pixel-shader sampling state.
    /// </summary>
    /// <param name="context">The open command context.</param>
    internal void EndDepthWrite(SilkD3D12CommandContext context) {
        context.GuardNotNull();
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        context.Transition(resource, ResourceStates.PixelShaderResource);
    }

    /// <summary>
    ///     Releases the texture, descriptor, and heap in dependency order.
    /// </summary>
    public void Dispose() {
        if (IsDisposed) return;
        resource.Dispose();
        depthStencilView.Dispose();
        depthStencilHeap.Dispose();
        IsDisposed = true;
    }

    /// <summary>
    ///     Creates a typeless R32 texture with a typed D32 view.
    /// </summary>
    /// <param name="width">The texture width.</param>
    /// <param name="height">The texture height.</param>
    /// <returns>The initialized depth-write resource.</returns>
    private SilkD3D12Resource CreateResource(uint width, uint height) {
        var created = device.CreateTexture2D(width,
            height,
            Format.FormatR32Typeless,
            ResourceFlags.AllowDepthStencil,
            ResourceStates.DepthWrite);
        try {
            device.CreateDepthStencilView(created, depthStencilView, Format.FormatD32Float);
            return created;
        } catch {
            created.Dispose();
            throw;
        }
    }
}

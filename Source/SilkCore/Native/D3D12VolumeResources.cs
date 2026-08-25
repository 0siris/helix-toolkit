/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using System.Runtime.InteropServices;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace HelixToolkit.SharpDX.Core.Native;

/// <summary>
///     Owns the immutable volume cube and the size-dependent back-position render target.
/// </summary>
internal sealed class SilkD3D12VolumeResources : IDisposable {
    /// <summary>
    ///     The cube vertex buffer.
    /// </summary>
    private readonly SilkD3D12Resource vertices;

    /// <summary>
    ///     The cube index buffer.
    /// </summary>
    private readonly SilkD3D12Resource indices;

    /// <summary>
    ///     The private render-target descriptor heap.
    /// </summary>
    private readonly SilkD3D12DescriptorHeap renderTargetHeap;

    /// <summary>
    ///     The private depth/stencil descriptor heap.
    /// </summary>
    private readonly SilkD3D12DescriptorHeap depthStencilHeap;

    /// <summary>
    ///     The stable render-target descriptor.
    /// </summary>
    private readonly SilkD3D12Descriptor renderTargetView;

    /// <summary>
    ///     The stable depth/stencil descriptor.
    /// </summary>
    private readonly SilkD3D12Descriptor depthStencilView;

    /// <summary>
    ///     The device used for size-dependent allocations.
    /// </summary>
    private readonly SilkD3D12Device device;

    /// <summary>
    ///     The size-dependent world-position texture.
    /// </summary>
    private SilkD3D12Resource? backPositions;

    /// <summary>
    ///     The size-dependent depth/stencil texture.
    /// </summary>
    private SilkD3D12Resource? depthStencil;

    /// <summary>
    ///     Initializes the cube and descriptor owners.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    internal SilkD3D12VolumeResources(SilkD3D12Device device) {
        device.AssertArgumentNotNull();
        this.device = device;
        Vector3[] positions = [
            new(-0.5f, -0.5f, -0.5f), new(0.5f, -0.5f, -0.5f),
            new(-0.5f, 0.5f, -0.5f), new(0.5f, 0.5f, -0.5f),
            new(-0.5f, -0.5f, 0.5f), new(0.5f, -0.5f, 0.5f),
            new(-0.5f, 0.5f, 0.5f), new(0.5f, 0.5f, 0.5f)
        ];
        int[] cubeIndices = [
            0, 2, 3, 3, 1, 0, 4, 5, 7, 7, 6, 4,
            0, 1, 5, 5, 4, 0, 1, 3, 7, 7, 5, 1,
            3, 2, 6, 6, 7, 3, 2, 0, 4, 4, 6, 2
        ];
        vertices = CreateUploadBuffer(device, positions);
        indices = CreateUploadBuffer(device, cubeIndices);
        renderTargetHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        depthStencilHeap = device.CreateDescriptorHeap(DescriptorHeapType.Dsv, 1);
        renderTargetView = renderTargetHeap.Allocate();
        depthStencilView = depthStencilHeap.Allocate();
    }

    /// <summary>
    ///     Gets the world-position texture after its backface pass.
    /// </summary>
    internal SilkD3D12Resource BackPositions => backPositions
        ?? throw new InvalidOperationException("The volume target has not been sized.");

    /// <summary>
    ///     Gets whether every owned native resource has been released.
    /// </summary>
    internal bool IsDisposed { get; private set; }

    /// <summary>
    ///     Resizes, clears, and binds the offscreen target for backface and opaque-position rendering.
    /// </summary>
    /// <param name="context">The open command context.</param>
    /// <param name="width">The target width.</param>
    /// <param name="height">The target height.</param>
    internal void BeginBackPositions(SilkD3D12CommandContext context, uint width, uint height) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        context.AssertArgumentNotNull();
        Resize(width, height);
        var positions = BackPositions;
        var depth = depthStencil!;
        context.Transition(positions, ResourceStates.RenderTarget);
        context.Transition(depth, ResourceStates.DepthWrite);
        context.ClearRenderTarget(positions, renderTargetView, [0, 0, 0, 0]);
        context.ClearDepthStencil(depth, depthStencilView, 1, 1);
        context.SetRenderTargets(renderTargetView, depthStencilView);
        context.SetViewport(width, height);
    }

    /// <summary>
    ///     Makes the completed back-position texture available to the volume pixel shader.
    /// </summary>
    /// <param name="context">The open command context.</param>
    internal void EndBackPositions(SilkD3D12CommandContext context) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        context.Transition(BackPositions, ResourceStates.PixelShaderResource);
    }

    /// <summary>
    ///     Binds and draws the indexed volume cube.
    /// </summary>
    /// <param name="context">The open command context.</param>
    /// <param name="topology">The selected pass topology.</param>
    internal void DrawCube(SilkD3D12CommandContext context, PrimitiveTopology topology) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        context.SetVertexBuffer(0, vertices, SilkMath.Vector3SizeInBytes);
        context.SetIndexBuffer(indices, Format.FormatR32Uint);
        context.SetPrimitiveTopology(topology);
        context.DrawIndexedInstanced(36);
    }

    /// <summary>
    ///     Releases the cube, offscreen textures, descriptors, and private heaps.
    /// </summary>
    public void Dispose() {
        if (IsDisposed) return;
        backPositions?.Dispose();
        depthStencil?.Dispose();
        renderTargetView.Dispose();
        depthStencilView.Dispose();
        renderTargetHeap.Dispose();
        depthStencilHeap.Dispose();
        vertices.Dispose();
        indices.Dispose();
        IsDisposed = true;
    }

    /// <summary>
    ///     Replaces size-dependent textures only when their physical size changes.
    /// </summary>
    /// <param name="width">The target width.</param>
    /// <param name="height">The target height.</param>
    private void Resize(uint width, uint height) {
        if (width == 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height == 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (backPositions is { Description.Width: var currentWidth, Description.Height: var currentHeight } &&
            currentWidth == width && currentHeight == height)
            return;

        var newBackPositions = device.CreateRenderTargetTexture2D(width,
            height,
            Format.FormatR16G16B16A16Float);
        var newDepthStencil = device.CreateDepthStencilTexture2D(width,
            height,
            Format.FormatD32FloatS8X24Uint);
        try {
            device.CreateRenderTargetView(newBackPositions, renderTargetView);
            device.CreateDepthStencilView(newDepthStencil, depthStencilView);
        } catch {
            newDepthStencil.Dispose();
            newBackPositions.Dispose();
            throw;
        }
        backPositions?.Dispose();
        depthStencil?.Dispose();
        backPositions = newBackPositions;
        depthStencil = newDepthStencil;
    }

    /// <summary>
    ///     Creates one upload buffer containing an unmanaged span.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="values">The source values.</param>
    /// <returns>The initialized upload buffer.</returns>
    private static SilkD3D12Resource CreateUploadBuffer<T>(SilkD3D12Device device, ReadOnlySpan<T> values)
        where T : unmanaged {
        var bytes = MemoryMarshal.AsBytes(values);
        var resource = device.CreateBuffer(checked((ulong) bytes.Length), HeapType.Upload);
        resource.Write(bytes);
        return resource;
    }
}

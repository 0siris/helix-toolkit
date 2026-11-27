using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace HelixToolkit.SharpDX.Core.Native;

/// Owns an isolated post-effect stencil and retains replaced generations until the host completes its frame fence.
internal sealed class D3D12PostEffectStencil : IDisposable {
    /// The native allocation owner.
    private readonly SilkD3D12Device device;
    /// DSV allocations for the current and retired generations.
    private readonly SilkD3D12DescriptorHeap heap;
    /// Generations still referenced by recorded commands.
    private readonly List<(SilkD3D12Resource Resource, SilkD3D12Descriptor View)> retired = [];
    /// The size-dependent resource and its view are published together.
    private (SilkD3D12Resource Resource, SilkD3D12Descriptor View)? current;
    /// The current size-dependent stencil resource.
    internal SilkD3D12Resource? Resource => current?.Resource;
    /// The descriptor of the current generation.
    internal SilkD3D12Descriptor? View => current?.View;

    /// <summary>
    ///     Creates the private descriptor owner.
    /// </summary>
    /// <param name="device">The native allocation owner.</param>
    internal D3D12PostEffectStencil(SilkD3D12Device device) {
        this.device = device;
        heap = device.CreateDescriptorHeap(DescriptorHeapType.Dsv, 64);
    }

    /// Releases only generations retired before the host completed the preceding frame fence.
    internal void BeginFrame() {
        foreach (var generation in retired) {
            generation.Resource.Dispose();
            generation.View.Dispose();
        }
        retired.Clear();
    }

    /// <summary>
    ///     Clears an outline stencil, or copies scene depth and clears only stencil for XRay occlusion.
    /// </summary>
    /// <param name="context">The open command context.</param>
    /// <param name="width">The target width.</param>
    /// <param name="height">The target height.</param>
    /// <param name="sceneDepth">The optional scene depth whose resource and state remain unchanged.</param>
    /// <returns>The private writable depth/stencil view.</returns>
    internal unsafe SilkD3D12Descriptor Begin(SilkD3D12CommandContext context, uint width, uint height,
        SilkD3D12Resource? sceneDepth = null) {
        var format = sceneDepth?.Description.Format ?? Format.FormatD32FloatS8X24Uint;
        if (current is not { } generation || generation.Resource.Description.Width != width ||
            generation.Resource.Description.Height != height || generation.Resource.Description.Format != format) {
            var replacement = device.CreateDepthStencilTexture2D(width, height, format);
            var view = heap.Allocate();
            device.CreateDepthStencilView(replacement, view);

            if (current is { } previous)
                retired.Add(previous);
            generation = (replacement, view);
            current = generation;
        }

        var (resource, descriptor) = generation;
        if (sceneDepth is not null) {
            var originalState = sceneDepth.State;
            context.Transition(sceneDepth, ResourceStates.CopySource);
            context.Transition(resource, ResourceStates.CopyDest);
            context.CopyTexture(resource, sceneDepth);
            context.Transition(sceneDepth, originalState);
            context.Transition(resource, ResourceStates.DepthWrite);
            context.CommandList.ClearDepthStencilView(descriptor.CpuHandle, ClearFlags.Stencil, 1, 0, 0,
                (Silk.NET.Maths.Box2D<int>*)null);
        } else {
            context.Transition(resource, ResourceStates.DepthWrite);
            context.ClearDepthStencil(resource, descriptor, 1, 0);
        }
        return descriptor;
    }

    /// Releases every retained resource and descriptor after the owning host has completed submission.
    public void Dispose() {
        BeginFrame();
        if (current is { } generation) {
            generation.Resource.Dispose();
            generation.View.Dispose();
            current = null;
        }
        heap.Dispose();
    }
}

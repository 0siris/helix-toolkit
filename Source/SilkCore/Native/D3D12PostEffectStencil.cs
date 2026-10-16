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
    /// The current size-dependent stencil resource.
    internal SilkD3D12Resource? Resource { get; private set; }
    /// The descriptor of the current generation.
    internal SilkD3D12Descriptor? View { get; private set; }

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
        if (Resource is null || Resource.Description.Width != width || Resource.Description.Height != height ||
            Resource.Description.Format != format) {
            var replacement = device.CreateDepthStencilTexture2D(width, height, format);
            var view = heap.Allocate();
            device.CreateDepthStencilView(replacement, view);
            if (Resource is not null) retired.Add((Resource, View!));
            Resource = replacement;
            View = view;
        }
        if (sceneDepth is not null) {
            var originalState = sceneDepth.State;
            context.Transition(sceneDepth, ResourceStates.CopySource);
            context.Transition(Resource, ResourceStates.CopyDest);
            context.CopyTexture(Resource, sceneDepth);
            context.Transition(sceneDepth, originalState);
            context.Transition(Resource, ResourceStates.DepthWrite);
            context.CommandList.ClearDepthStencilView(View!.CpuHandle, ClearFlags.ClearFlagStencil, 1, 0, 0,
                (Silk.NET.Maths.Box2D<int>*)null);
        } else {
            context.Transition(Resource, ResourceStates.DepthWrite);
            context.ClearDepthStencil(Resource, View!, 1, 0);
        }
        return View!;
    }

    /// Releases every retained resource and descriptor after the owning host has completed submission.
    public void Dispose() {
        BeginFrame();
        Resource?.Dispose();
        View?.Dispose();
        Resource = null;
        View = null;
        heap.Dispose();
    }
}

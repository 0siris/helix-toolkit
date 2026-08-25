/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Shaders;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace HelixToolkit.SharpDX.Core.Native;

/// <summary>
///     Owns the size-dependent weighted-OIT and depth-peeling targets plus their full-screen bindings.
/// </summary>
internal sealed class SilkD3D12TransparencyResources : IDisposable {
    /// <summary>
    ///     Weighted-OIT accumulation format.
    /// </summary>
    internal const Format WeightedColorFormat = Format.FormatR16G16B16A16Float;

    /// <summary>
    ///     Weighted-OIT revealage format.
    /// </summary>
    internal const Format WeightedAlphaFormat = Format.FormatA8Unorm;

    /// <summary>
    ///     Depth-peeling min/max format.
    /// </summary>
    internal const Format PeelingDepthFormat = Format.FormatR32G32Float;

    /// <summary>
    ///     Depth-peeling color format.
    /// </summary>
    internal const Format PeelingColorFormat = Format.FormatB8G8R8A8Unorm;

    /// <summary>
    ///     The device used for size-dependent resources.
    /// </summary>
    private readonly SilkD3D12Device device;

    /// <summary>
    ///     Private render-target descriptors in resource order.
    /// </summary>
    private readonly SilkD3D12DescriptorHeap renderTargetHeap;

    /// <summary>
    ///     The six stable render-target descriptors.
    /// </summary>
    private readonly SilkD3D12Descriptor[] renderTargetViews;

    /// <summary>
    ///     Full-screen root tables.
    /// </summary>
    private readonly SilkD3D12GraphicsBindings bindings;

    /// <summary>
    ///     The b0 global constants used by full-screen passes.
    /// </summary>
    private readonly SilkD3D12ConstantBuffer globalTransforms;

    /// <summary>
    ///     Zero constants backing unused b-registers.
    /// </summary>
    private readonly SilkD3D12Resource fallbackConstants;

    /// <summary>
    ///     Size-dependent target resources in descriptor order.
    /// </summary>
    private readonly SilkD3D12Resource?[] targets = new SilkD3D12Resource?[6];

    /// <summary>
    ///     Initializes stable descriptors and full-screen bindings.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="resourceHeap">The shared shader-visible resource heap.</param>
    /// <param name="samplerHeap">The shared shader-visible sampler heap.</param>
    internal SilkD3D12TransparencyResources(
        SilkD3D12Device device,
        SilkD3D12DescriptorHeap resourceHeap,
        SilkD3D12DescriptorHeap samplerHeap
    ) {
        device.AssertArgumentNotNull();
        this.device = device;
        renderTargetHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 6);
        renderTargetViews = renderTargetHeap.AllocateRange(6);
        bindings = new SilkD3D12GraphicsBindings(resourceHeap, samplerHeap);
        globalTransforms = new SilkD3D12ConstantBuffer(device,
            bindings.ConstantBuffer(0),
            GlobalTransformStruct.SizeInBytes);
        fallbackConstants = device.CreateBuffer(256, HeapType.Upload);
        fallbackConstants.Write(new byte[256]);
        bindings.InitializeFallbackDescriptors(device, fallbackConstants, 0);
        device.CreateSampler(bindings.Sampler(0));
    }

    /// <summary>
    ///     Gets the current weighted color target for verification.
    /// </summary>
    internal SilkD3D12Resource WeightedColor => Target(0);

    /// <summary>
    ///     Gets the current weighted alpha target for verification.
    /// </summary>
    internal SilkD3D12Resource WeightedAlpha => Target(1);

    /// <summary>
    ///     Gets whether all owned resources and descriptors have been released.
    /// </summary>
    internal bool IsDisposed { get; private set; }

    /// <summary>
    ///     Clears and binds both weighted-OIT targets with the presentation depth buffer.
    /// </summary>
    /// <param name="context">The open command context.</param>
    /// <param name="width">The physical target width.</param>
    /// <param name="height">The physical target height.</param>
    /// <param name="depthStencil">The presentation depth/stencil descriptor.</param>
    internal void BeginWeighted(
        SilkD3D12CommandContext context,
        uint width,
        uint height,
        SilkD3D12Descriptor depthStencil
    ) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        Resize(width, height);
        context.Transition(Target(0), ResourceStates.RenderTarget);
        context.Transition(Target(1), ResourceStates.RenderTarget);
        context.ClearRenderTarget(Target(0), renderTargetViews[0], [0, 0, 0, 0]);
        context.ClearRenderTarget(Target(1), renderTargetViews[1], [1, 1, 1, 1]);
        context.SetRenderTargets(renderTargetViews.AsSpan(0, 2), depthStencil);
        context.SetViewport(width, height);
    }

    /// <summary>
    ///     Composites the completed weighted targets onto the presentation target.
    /// </summary>
    /// <param name="context">The open command context.</param>
    /// <param name="pass">The existing weighted-OIT full-screen pass.</param>
    /// <param name="transforms">The current global constants.</param>
    /// <param name="mainRenderTarget">The presentation render-target descriptor.</param>
    /// <param name="width">The physical target width.</param>
    /// <param name="height">The physical target height.</param>
    internal void CompositeWeighted(
        SilkD3D12CommandContext context,
        ShaderPass pass,
        in GlobalTransformStruct transforms,
        SilkD3D12Descriptor mainRenderTarget,
        uint width,
        uint height
    ) {
        context.Transition(Target(0), ResourceStates.PixelShaderResource);
        context.Transition(Target(1), ResourceStates.PixelShaderResource);
        device.CreateShaderResourceView(Target(0), bindings.ShaderResource(10));
        device.CreateShaderResourceView(Target(1), bindings.ShaderResource(11));
        DrawFullscreen(context, pass, in transforms, mainRenderTarget, width, height);
    }

    /// <summary>
    ///     Copies the opaque frame, clears the peeling accumulators, and binds the first min/max target.
    /// </summary>
    /// <param name="context">The open command context.</param>
    /// <param name="mainColor">The presentation color resource.</param>
    /// <param name="depthStencil">The presentation depth/stencil descriptor.</param>
    /// <param name="width">The physical target width.</param>
    /// <param name="height">The physical target height.</param>
    internal void BeginDepthPeeling(
        SilkD3D12CommandContext context,
        SilkD3D12Resource mainColor,
        SilkD3D12Descriptor depthStencil,
        uint width,
        uint height
    ) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        Resize(width, height);
        context.Transition(Target(4), ResourceStates.RenderTarget);
        context.ClearRenderTarget(Target(4), renderTargetViews[4], [0, 0, 0, 1]);
        context.Transition(mainColor, ResourceStates.CopySource);
        context.Transition(Target(5), ResourceStates.CopyDest);
        context.CopyTexture(Target(5), mainColor);
        context.Transition(mainColor, ResourceStates.RenderTarget);
        context.Transition(Target(2), ResourceStates.RenderTarget);
        context.ClearRenderTarget(Target(2), renderTargetViews[2], [-1, -1, 0, 0]);
        context.SetRenderTargets(renderTargetViews.AsSpan(2, 1), depthStencil);
        context.SetViewport(width, height);
    }

    /// <summary>
    ///     Binds one depth-peeling layer and returns its previous min/max depth texture.
    /// </summary>
    /// <param name="context">The open command context.</param>
    /// <param name="layer">The positive peeling layer index.</param>
    /// <param name="depthStencil">The presentation depth/stencil descriptor.</param>
    /// <returns>The previous layer's min/max texture for t100.</returns>
    internal SilkD3D12Resource BeginDepthPeelingLayer(
        SilkD3D12CommandContext context,
        int layer,
        SilkD3D12Descriptor depthStencil
    ) {
        if (layer < 1) throw new ArgumentOutOfRangeException(nameof(layer));
        var current = 2 + layer % 2;
        var previous = 2 + 1 - layer % 2;
        context.Transition(Target(previous), ResourceStates.PixelShaderResource);
        context.Transition(Target(current), ResourceStates.RenderTarget);
        context.Transition(Target(4), ResourceStates.RenderTarget);
        context.Transition(Target(5), ResourceStates.RenderTarget);
        context.ClearRenderTarget(Target(current), renderTargetViews[current], [-1, -1, 0, 0]);
        SilkD3D12Descriptor[] views = [renderTargetViews[current], renderTargetViews[4], renderTargetViews[5]];
        context.SetRenderTargets(views, depthStencil);
        return Target(previous);
    }

    /// <summary>
    ///     Composites the accumulated front and back layers onto the presentation target.
    /// </summary>
    /// <param name="context">The open command context.</param>
    /// <param name="pass">The existing depth-peeling final pass.</param>
    /// <param name="lastLayer">The final positive layer index, or zero for the initialization pass.</param>
    /// <param name="transforms">The current global constants.</param>
    /// <param name="mainRenderTarget">The presentation render-target descriptor.</param>
    /// <param name="width">The physical target width.</param>
    /// <param name="height">The physical target height.</param>
    internal void CompositeDepthPeeling(
        SilkD3D12CommandContext context,
        ShaderPass pass,
        int lastLayer,
        in GlobalTransformStruct transforms,
        SilkD3D12Descriptor mainRenderTarget,
        uint width,
        uint height
    ) {
        var depthIndex = 2 + Math.Max(0, lastLayer) % 2;
        context.Transition(Target(depthIndex), ResourceStates.PixelShaderResource);
        context.Transition(Target(4), ResourceStates.PixelShaderResource);
        context.Transition(Target(5), ResourceStates.PixelShaderResource);
        device.CreateShaderResourceView(Target(depthIndex), bindings.ShaderResource(100));
        device.CreateShaderResourceView(Target(4), bindings.ShaderResource(101));
        device.CreateShaderResourceView(Target(5), bindings.ShaderResource(102));
        DrawFullscreen(context, pass, in transforms, mainRenderTarget, width, height);
    }

    /// <summary>
    ///     Releases all size-dependent targets and stable descriptor owners.
    /// </summary>
    public void Dispose() {
        if (IsDisposed) return;
        for (var index = 0; index < targets.Length; index++) targets[index]?.Dispose();
        fallbackConstants.Dispose();
        globalTransforms.Dispose();
        bindings.Dispose();
        foreach (var view in renderTargetViews) view.Dispose();
        renderTargetHeap.Dispose();
        IsDisposed = true;
    }

    /// <summary>
    ///     Gets one initialized target.
    /// </summary>
    /// <param name="index">The target index.</param>
    /// <returns>The initialized resource.</returns>
    private SilkD3D12Resource Target(int index) => targets[index]
        ?? throw new InvalidOperationException("The transparency targets have not been sized.");

    /// <summary>
    ///     Replaces all targets atomically when the physical size changes.
    /// </summary>
    /// <param name="width">The target width.</param>
    /// <param name="height">The target height.</param>
    private void Resize(uint width, uint height) {
        if (width == 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height == 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (targets[0] is { Description.Width: var oldWidth, Description.Height: var oldHeight } &&
            oldWidth == width && oldHeight == height)
            return;

        Format[] formats = [
            WeightedColorFormat,
            WeightedAlphaFormat,
            PeelingDepthFormat,
            PeelingDepthFormat,
            PeelingColorFormat,
            PeelingColorFormat
        ];
        var replacements = new SilkD3D12Resource[formats.Length];
        try {
            for (var index = 0; index < replacements.Length; index++) {
                replacements[index] = device.CreateRenderTargetTexture2D(width, height, formats[index]);
                device.CreateRenderTargetView(replacements[index], renderTargetViews[index]);
            }
        } catch {
            foreach (var replacement in replacements) replacement?.Dispose();
            throw;
        }
        for (var index = 0; index < targets.Length; index++) {
            targets[index]?.Dispose();
            targets[index] = replacements[index];
        }
    }

    /// <summary>
    ///     Binds common full-screen state and records a four-vertex triangle strip.
    /// </summary>
    /// <param name="context">The open command context.</param>
    /// <param name="pass">The native full-screen pass.</param>
    /// <param name="transforms">The current global constants.</param>
    /// <param name="mainRenderTarget">The destination render target.</param>
    /// <param name="width">The target width.</param>
    /// <param name="height">The target height.</param>
    private void DrawFullscreen(
        SilkD3D12CommandContext context,
        ShaderPass pass,
        in GlobalTransformStruct transforms,
        SilkD3D12Descriptor mainRenderTarget,
        uint width,
        uint height
    ) {
        globalTransforms.Write(in transforms);
        context.SetRenderTarget(mainRenderTarget);
        context.SetViewport(width, height);
        pass.BindShader(context);
        bindings.BindGraphics(context);
        context.DrawInstanced(4);
    }
}

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
///     Owns the two full-resolution targets and shared bindings used by DX12 full-screen post effects.
/// </summary>
internal sealed class SilkD3D12PostProcessResources : IDisposable {
    /// <summary>
    ///     The format shared with the WPF swap chain.
    /// </summary>
    internal const Format TargetFormat = Format.FormatB8G8R8A8Unorm;

    /// <summary>
    ///     The native device.
    /// </summary>
    private readonly SilkD3D12Device device;

    /// <summary>
    ///     The shared shader-visible resource heap.
    /// </summary>
    private readonly SilkD3D12DescriptorHeap resourceHeap;

    /// <summary>
    ///     The shared shader-visible sampler heap.
    /// </summary>
    private readonly SilkD3D12DescriptorHeap samplerHeap;

    /// <summary>
    ///     Stable per-draw tables retained until the current frame fence completes.
    /// </summary>
    private readonly List<BindingSlot> bindingSlots = [];

    /// <summary>
    ///     The next binding slot used by this frame.
    /// </summary>
    private int bindingSlotIndex;

    /// <summary>
    ///     The private RTV heap.
    /// </summary>
    private readonly SilkD3D12DescriptorHeap renderTargetHeap;

    /// <summary>
    ///     Stable RTV descriptors for the ping-pong resources.
    /// </summary>
    private readonly SilkD3D12Descriptor[] renderTargetViews;

    /// <summary>
    ///     The current size-dependent ping-pong resources.
    /// </summary>
    private readonly SilkD3D12Resource?[] targets = new SilkD3D12Resource?[2];

    /// <summary>
    ///     Creates stable descriptor tables and render-target descriptors.
    /// </summary>
    /// <param name="device">The native device.</param>
    /// <param name="resourceHeap">The shared shader-visible resource heap.</param>
    /// <param name="samplerHeap">The shared shader-visible sampler heap.</param>
    internal SilkD3D12PostProcessResources(
        SilkD3D12Device device,
        SilkD3D12DescriptorHeap resourceHeap,
        SilkD3D12DescriptorHeap samplerHeap
    ) {
        this.device = device;
        this.resourceHeap = resourceHeap;
        this.samplerHeap = samplerHeap;
        renderTargetHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 2);
        renderTargetViews = renderTargetHeap.AllocateRange(2);
    }

    /// <summary>
    ///     Gets the first ping-pong resource for contract tests.
    /// </summary>
    internal SilkD3D12Resource First => Target(0);

    /// <summary>
    ///     Gets the second ping-pong resource for contract tests.
    /// </summary>
    internal SilkD3D12Resource Second => Target(1);

    /// <summary>
    ///     Gets whether all resources and descriptor owners have been released.
    /// </summary>
    internal bool IsDisposed { get; private set; }

    /// <summary>
    ///     Reuses completed-frame binding slots without overwriting descriptors referenced by the open command list.
    /// </summary>
    internal void BeginFrame() {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        bindingSlotIndex = 0;
    }

    /// <summary>
    ///     Clears and binds one ping-pong target for a geometry mask pass.
    /// </summary>
    /// <param name="context">The open command context.</param>
    /// <param name="index">The destination target index.</param>
    /// <param name="width">The physical width.</param>
    /// <param name="height">The physical height.</param>
    /// <param name="depthStencil">The optional depth/stencil descriptor.</param>
    /// <returns>The bound target resource.</returns>
    internal SilkD3D12Resource BeginTarget(
        SilkD3D12CommandContext context,
        int index,
        uint width,
        uint height,
        SilkD3D12Descriptor? depthStencil = null
    ) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        Resize(width, height);
        var target = Target(index);
        context.Transition(target, ResourceStates.RenderTarget);
        context.ClearRenderTarget(target, renderTargetViews[index], [0, 0, 0, 0]);
        context.SetRenderTargets([renderTargetViews[index]], depthStencil);
        context.SetViewport(width, height);
        return target;
    }

    /// <summary>
    ///     Copies the current presentation color into the first ping-pong resource.
    /// </summary>
    /// <param name="context">The open command context.</param>
    /// <param name="source">The presentation color resource.</param>
    /// <param name="width">The physical width.</param>
    /// <param name="height">The physical height.</param>
    internal void Capture(
        SilkD3D12CommandContext context,
        SilkD3D12Resource source,
        uint width,
        uint height
    ) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        Resize(width, height);
        context.Transition(source, ResourceStates.CopySource);
        context.Transition(Target(0), ResourceStates.CopyDest);
        context.CopyTexture(Target(0), source);
    }

    /// <summary>
    ///     Draws one full-screen pass from a ping-pong source into another ping-pong target.
    /// </summary>
    /// <param name="context">The open command context.</param>
    /// <param name="pass">The native full-screen pass.</param>
    /// <param name="sourceIndex">The source target index.</param>
    /// <param name="destinationIndex">The destination target index.</param>
    /// <param name="transforms">The current global constants.</param>
    /// <param name="effect">The b6 post-effect constants.</param>
    internal void Draw(
        SilkD3D12CommandContext context,
        ShaderPass pass,
        int sourceIndex,
        int destinationIndex,
        in GlobalTransformStruct transforms,
        in BorderEffectStruct effect
    ) {
        if (sourceIndex == destinationIndex) throw new ArgumentException("Post-process source and target must differ.");
        context.Transition(Target(destinationIndex), ResourceStates.RenderTarget);
        Draw(context,
            pass,
            Target(sourceIndex),
            renderTargetViews[destinationIndex],
            in transforms,
            in effect);
    }

    /// <summary>
    ///     Draws one ping-pong source into the presentation target.
    /// </summary>
    /// <param name="context">The open command context.</param>
    /// <param name="pass">The native full-screen pass.</param>
    /// <param name="sourceIndex">The source target index.</param>
    /// <param name="destination">The presentation RTV.</param>
    /// <param name="transforms">The current global constants.</param>
    /// <param name="effect">The b6 post-effect constants.</param>
    internal void DrawToPresentation(
        SilkD3D12CommandContext context,
        ShaderPass pass,
        int sourceIndex,
        SilkD3D12Descriptor destination,
        in GlobalTransformStruct transforms,
        in BorderEffectStruct effect
    ) => Draw(context, pass, Target(sourceIndex), destination, in transforms, in effect);

    /// <summary>
    ///     Releases native resources and descriptor allocations.
    /// </summary>
    public void Dispose() {
        if (IsDisposed) return;
        foreach (var target in targets) target?.Dispose();
        foreach (var view in renderTargetViews) view.Dispose();
        renderTargetHeap.Dispose();
        foreach (var slot in bindingSlots) slot.Dispose();
        IsDisposed = true;
    }

    /// <summary>
    ///     Records one full-screen draw using t0 and s0.
    /// </summary>
    /// <param name="context">The open command context.</param>
    /// <param name="pass">The native full-screen pass.</param>
    /// <param name="source">The source texture.</param>
    /// <param name="destination">The destination RTV.</param>
    /// <param name="transforms">The current global constants.</param>
    /// <param name="effect">The b6 post-effect constants.</param>
    private void Draw(
        SilkD3D12CommandContext context,
        ShaderPass pass,
        SilkD3D12Resource source,
        SilkD3D12Descriptor destination,
        in GlobalTransformStruct transforms,
        in BorderEffectStruct effect
    ) {
        var slot = NextBindingSlot();
        context.Transition(source, ResourceStates.PixelShaderResource);
        device.CreateShaderResourceView(source, slot.Bindings.ShaderResource(0));
        slot.GlobalTransforms.Write(in transforms);
        slot.EffectConstants.Write(in effect);
        context.SetRenderTarget(destination);
        context.SetViewport(checked((uint) source.Description.Width), source.Description.Height);
        pass.BindShader(context);
        slot.Bindings.BindGraphics(context);
        context.DrawInstanced(4);
    }

    /// <summary>
    ///     Returns one descriptor-and-constant set that remains immutable for the recorded draw.
    /// </summary>
    /// <returns>The next frame-local binding slot.</returns>
    private BindingSlot NextBindingSlot() {
        if (bindingSlotIndex == bindingSlots.Count)
            bindingSlots.Add(new BindingSlot(device, resourceHeap, samplerHeap));
        return bindingSlots[bindingSlotIndex++];
    }

    /// <summary>
    ///     Gets one initialized target.
    /// </summary>
    /// <param name="index">The target index.</param>
    /// <returns>The initialized target.</returns>
    private SilkD3D12Resource Target(int index) {
        if ((uint) index >= targets.Length) throw new ArgumentOutOfRangeException(nameof(index));
        return targets[index] ?? throw new InvalidOperationException("Post-process targets are not initialized.");
    }

    /// <summary>
    ///     Replaces both targets when the physical size changes.
    /// </summary>
    /// <param name="width">The new width.</param>
    /// <param name="height">The new height.</param>
    private void Resize(uint width, uint height) {
        ArgumentOutOfRangeException.ThrowIfZero(width);
        ArgumentOutOfRangeException.ThrowIfZero(height);
        if (targets[0] is { Description.Width: var currentWidth, Description.Height: var currentHeight } &&
            currentWidth == width && currentHeight == height)
            return;

        var replacements = new SilkD3D12Resource[2];
        try {
            for (var index = 0; index < replacements.Length; index++) {
                replacements[index] = device.CreateRenderTargetTexture2D(width, height, TargetFormat);
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
    ///     Owns one immutable descriptor table and upload constants for a recorded full-screen draw.
    /// </summary>
    private sealed class BindingSlot : IDisposable {
        /// <summary>
        ///     Initializes one complete table and its b0/b6 constants.
        /// </summary>
        /// <param name="device">The native device.</param>
        /// <param name="resourceHeap">The shared shader-visible resource heap.</param>
        /// <param name="samplerHeap">The shared shader-visible sampler heap.</param>
        internal BindingSlot(
            SilkD3D12Device device,
            SilkD3D12DescriptorHeap resourceHeap,
            SilkD3D12DescriptorHeap samplerHeap
        ) {
            Bindings = new SilkD3D12GraphicsBindings(resourceHeap, samplerHeap);
            GlobalTransforms = new SilkD3D12ConstantBuffer(device,
                Bindings.ConstantBuffer(0),
                GlobalTransformStruct.SizeInBytes);
            EffectConstants = new SilkD3D12ConstantBuffer(device,
                Bindings.ConstantBuffer(6),
                BorderEffectStruct.SizeInBytes);
            FallbackConstants = device.CreateBuffer(256, HeapType.Upload);
            FallbackConstants.Write(new byte[256]);
            Bindings.InitializeFallbackDescriptors(device, FallbackConstants, 0, 6);
            device.CreateSampler(Bindings.Sampler(0), Filter.MinMagMipLinear, TextureAddressMode.Clamp);
        }

        /// <summary>Gets the complete resource and sampler tables.</summary>
        internal SilkD3D12GraphicsBindings Bindings { get; }

        /// <summary>Gets the b0 upload constants.</summary>
        internal SilkD3D12ConstantBuffer GlobalTransforms { get; }

        /// <summary>Gets the b6 upload constants.</summary>
        internal SilkD3D12ConstantBuffer EffectConstants { get; }

        /// <summary>Gets the zero-filled storage for unused constant registers.</summary>
        private SilkD3D12Resource FallbackConstants { get; }

        /// <summary>Releases the table and all owned buffers.</summary>
        public void Dispose() {
            FallbackConstants.Dispose();
            EffectConstants.Dispose();
            GlobalTransforms.Dispose();
            Bindings.Dispose();
        }
    }
}

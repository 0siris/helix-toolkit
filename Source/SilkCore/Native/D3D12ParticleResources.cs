/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using System.Runtime.InteropServices;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Shaders;
using Silk.NET.Direct3D12;

namespace HelixToolkit.SharpDX.Core.Native;

/// <summary>
///     Owns the ping-pong simulation buffers, counters, root tables, and indirect arguments for one particle core.
/// </summary>
internal sealed class SilkD3D12ParticleResources : IDisposable {
    /// <summary>The b0 register used by global transforms.</summary>
    private const int GlobalTransformRegister = 0;

    /// <summary>The b4 register used by the particle model.</summary>
    private const int ModelRegister = 4;

    /// <summary>The b7 register used by per-frame simulation parameters.</summary>
    private const int FrameRegister = 7;

    /// <summary>The b8 register used by insertion parameters.</summary>
    private const int InsertRegister = 8;

    /// <summary>The t0 register used by the current simulation state during drawing.</summary>
    private const int SimulationRegister = 0;

    /// <summary>The t1 register used by the optional particle texture.</summary>
    private const int TextureRegister = 1;

    /// <summary>The u0 register used by the consume buffer.</summary>
    private const int CurrentStateRegister = 0;

    /// <summary>The u1 register used by the append buffer.</summary>
    private const int NewStateRegister = 1;

    /// <summary>The s6 register used by the particle sampler.</summary>
    private const int SamplerRegister = 6;

    /// <summary>The Direct3D 12 device used to populate descriptors.</summary>
    private readonly SilkD3D12Device device;

    /// <summary>The complete root descriptor tables.</summary>
    private readonly SilkD3D12GraphicsBindings bindings;

    /// <summary>The b0 upload constant buffer.</summary>
    private readonly SilkD3D12ConstantBuffer globalTransforms;

    /// <summary>The b4 upload constant buffer.</summary>
    private readonly SilkD3D12ConstantBuffer model;

    /// <summary>The b8 upload constant buffer.</summary>
    private readonly SilkD3D12ConstantBuffer insert;

    /// <summary>The b7 default-heap constant buffer whose first word is populated from the current GPU counter.</summary>
    private readonly SilkD3D12Resource frame;

    /// <summary>The upload source for the complete b7 payload.</summary>
    private readonly SilkD3D12Resource frameUpload;

    /// <summary>The zero source used to reset append counters.</summary>
    private readonly SilkD3D12Resource zeroUpload;

    /// <summary>The GPU indirect-draw arguments.</summary>
    private readonly SilkD3D12Resource arguments;

    /// <summary>The per-frame upload source for indirect-draw arguments.</summary>
    private readonly SilkD3D12Resource argumentsUpload;

    /// <summary>The shared non-indexed draw command signature.</summary>
    private readonly SilkD3D12CommandSignature commandSignature;

    /// <summary>The two structured particle buffers.</summary>
    private readonly SilkD3D12Resource[] states;

    /// <summary>The separate append/consume counters associated with <see cref="states" />.</summary>
    private readonly SilkD3D12Resource[] counters;

    /// <summary>The index of the buffer consumed by the next update pass.</summary>
    private int currentIndex;

    /// <summary>Whether both counter resources have received their initial zero.</summary>
    private bool initialized;

    /// <summary>
    ///     Initializes all resources for one maximum particle count.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="resourceHeap">The shader-visible resource heap.</param>
    /// <param name="samplerHeap">The shader-visible sampler heap.</param>
    /// <param name="capacity">The maximum number of particles.</param>
    internal SilkD3D12ParticleResources(
        SilkD3D12Device device,
        SilkD3D12DescriptorHeap resourceHeap,
        SilkD3D12DescriptorHeap samplerHeap,
        uint capacity
    ) {
        device.AssertArgumentNotNull();
        if (capacity == 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        this.device = device;
        Capacity = capacity;
        bindings = new SilkD3D12GraphicsBindings(resourceHeap, samplerHeap);
        var fallback = device.CreateBuffer(256, HeapType.Upload);
        try {
            fallback.Write(new byte[256]);
            bindings.InitializeFallbackDescriptors(device,
                fallback,
                GlobalTransformRegister,
                ModelRegister,
                FrameRegister,
                InsertRegister);
            globalTransforms = new SilkD3D12ConstantBuffer(device,
                bindings.ConstantBuffer(GlobalTransformRegister),
                GlobalTransformStruct.SizeInBytes);
            model = new SilkD3D12ConstantBuffer(device,
                bindings.ConstantBuffer(ModelRegister),
                ParticleModelStruct.SizeInBytes);
            insert = new SilkD3D12ConstantBuffer(device,
                bindings.ConstantBuffer(InsertRegister),
                ParticleInsertParameters.SizeInBytes);
            frame = device.CreateBuffer(256);
            frameUpload = device.CreateBuffer(256, HeapType.Upload);
            zeroUpload = device.CreateBuffer(sizeof(uint), HeapType.Upload);
            arguments = device.CreateBuffer(ParticleCountIndirectArgs.SizeInBytes);
            argumentsUpload = device.CreateBuffer(ParticleCountIndirectArgs.SizeInBytes, HeapType.Upload);
            commandSignature = device.CreateDrawCommandSignature();
            states = [
                device.CreateBuffer(checked((ulong) capacity * Particle.SizeInBytes),
                    flags: ResourceFlags.AllowUnorderedAccess),
                device.CreateBuffer(checked((ulong) capacity * Particle.SizeInBytes),
                    flags: ResourceFlags.AllowUnorderedAccess)
            ];
            counters = [
                device.CreateBuffer(sizeof(uint), flags: ResourceFlags.AllowUnorderedAccess),
                device.CreateBuffer(sizeof(uint), flags: ResourceFlags.AllowUnorderedAccess)
            ];
            zeroUpload.Write(new byte[sizeof(uint)]);
            device.CreateConstantBufferView(frame, bindings.ConstantBuffer(FrameRegister), 256);
            device.CreateSampler(bindings.Sampler(SamplerRegister));
        } catch {
            Dispose();
            throw;
        } finally {
            fallback.Dispose();
        }
    }

    /// <summary>Gets the maximum particle count represented by the native buffers.</summary>
    internal uint Capacity { get; }

    /// <summary>Gets the counter containing the particle count rendered by the latest frame.</summary>
    internal SilkD3D12Resource RenderCounter => counters[currentIndex];

    /// <summary>Gets the structured state rendered by the latest frame.</summary>
    internal SilkD3D12Resource RenderState => states[currentIndex];

    /// <summary>Gets the GPU indirect-draw argument buffer.</summary>
    internal SilkD3D12Resource Arguments => arguments;

    /// <summary>Gets whether all owned native resources have been released.</summary>
    internal bool IsDisposed { get; private set; }

    /// <summary>
    ///     Records simulation, optional insertion, and the resulting indirect particle draw.
    /// </summary>
    /// <param name="context">The open command context.</param>
    /// <param name="resources">The host resource manager.</param>
    /// <param name="core">The existing particle render core.</param>
    /// <param name="updatePass">The compute update pass.</param>
    /// <param name="insertPass">The compute insertion pass.</param>
    /// <param name="renderPass">The particle graphics pass.</param>
    /// <param name="transforms">The current camera transforms.</param>
    /// <returns>Whether a particle draw was recorded.</returns>
    internal bool Render(
        SilkD3D12CommandContext context,
        SilkD3D12ResourceManager resources,
        ParticleRenderCore core,
        ShaderPass updatePass,
        ShaderPass insertPass,
        ShaderPass renderPass,
        in GlobalTransformStruct transforms
    ) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        context.AssertArgumentNotNull();
        resources.AssertArgumentNotNull();
        core.AssertArgumentNotNull();
        var shouldInsert = core.PrepareD3D12(transforms.TimeStamp,
            out var frameData,
            out var insertData,
            out var modelData);
        var nextIndex = 1 - currentIndex;
        InitializeCounters(context);
        ResetCounter(context, counters[nextIndex]);

        globalTransforms.Write(in transforms);
        model.Write(in modelData);
        insert.Write(in insertData);
        Write(frameUpload, in frameData);
        context.Transition(frame, ResourceStates.CopyDest);
        context.CopyBuffer(frame, 0, frameUpload, 0, ParticlePerFrame.SizeInBytes);
        context.Transition(counters[currentIndex], ResourceStates.CopySource);
        context.CopyBuffer(frame, ParticlePerFrame.NumParticlesOffset, counters[currentIndex], 0, sizeof(uint));
        context.Transition(counters[currentIndex], ResourceStates.UnorderedAccess);
        context.Transition(frame, ResourceStates.VertexAndConstantBuffer);

        BindSimulationDescriptors(context, currentIndex, nextIndex);
        updatePass.BindShader(context);
        bindings.BindCompute(context);
        context.Dispatch(Math.Max(1u, (Capacity + 511u) / 512u), 1, 1);
        context.UavBarrier(states[nextIndex]);
        context.UavBarrier(counters[nextIndex]);
        if (shouldInsert) {
            insertPass.BindShader(context);
            bindings.BindCompute(context);
            context.Dispatch(1, 1, 1);
            context.UavBarrier(states[nextIndex]);
            context.UavBarrier(counters[nextIndex]);
        }

        var instanceBuffer = core.InstanceBuffer.HasElements
            ? resources.GetOrCreate((IElementsBufferModel<Matrix>) core.InstanceBuffer)
            : null;
        var drawArguments = new ParticleCountIndirectArgs {
            InstanceCount = instanceBuffer?.ElementCount ?? 1
        };
        Write(argumentsUpload, in drawArguments);
        context.Transition(arguments, ResourceStates.CopyDest);
        context.CopyBuffer(arguments, 0, argumentsUpload, 0, ParticleCountIndirectArgs.SizeInBytes);
        context.Transition(counters[nextIndex], ResourceStates.CopySource);
        context.CopyBuffer(arguments, 0, counters[nextIndex], 0, sizeof(uint));
        context.Transition(arguments, ResourceStates.IndirectArgument);
        context.Transition(states[nextIndex], ResourceStates.NonPixelShaderResource);
        device.CreateStructuredBufferShaderResourceView(states[nextIndex],
            bindings.ShaderResource(SimulationRegister),
            Capacity,
            Particle.SizeInBytes);
        BindTexture(context, resources, core);

        renderPass.BindShader(context);
        bindings.BindGraphics(context);
        instanceBuffer?.Bind(context, 0);
        context.DrawInstancedIndirect(commandSignature, arguments);
        currentIndex = nextIndex;
        return true;
    }

    /// <summary>
    ///     Initializes both counter resources before their first UAV use.
    /// </summary>
    /// <param name="context">The open command context.</param>
    private void InitializeCounters(SilkD3D12CommandContext context) {
        if (initialized) return;
        foreach (var counter in counters) ResetCounter(context, counter);
        initialized = true;
    }

    /// <summary>
    ///     Resets one append/consume counter to zero and returns it to UAV state.
    /// </summary>
    /// <param name="context">The open command context.</param>
    /// <param name="counter">The counter resource.</param>
    private void ResetCounter(SilkD3D12CommandContext context, SilkD3D12Resource counter) {
        context.Transition(counter, ResourceStates.CopyDest);
        context.CopyBuffer(counter, 0, zeroUpload, 0, sizeof(uint));
        context.Transition(counter, ResourceStates.UnorderedAccess);
    }

    /// <summary>
    ///     Rewrites the u0/u1 append/consume descriptors for the current ping-pong direction.
    /// </summary>
    /// <param name="context">The open command context.</param>
    /// <param name="current">The consume-state index.</param>
    /// <param name="next">The append-state index.</param>
    private void BindSimulationDescriptors(SilkD3D12CommandContext context, int current, int next) {
        context.Transition(states[current], ResourceStates.UnorderedAccess);
        context.Transition(states[next], ResourceStates.UnorderedAccess);
        device.CreateStructuredBufferUnorderedAccessView(states[current],
            counters[current],
            bindings.UnorderedAccess(CurrentStateRegister),
            Capacity,
            Particle.SizeInBytes);
        device.CreateStructuredBufferUnorderedAccessView(states[next],
            counters[next],
            bindings.UnorderedAccess(NewStateRegister),
            Capacity,
            Particle.SizeInBytes);
    }

    /// <summary>
    ///     Loads or clears the optional t1 texture and writes the existing s6 sampler.
    /// </summary>
    /// <param name="context">The command context receiving a first-use upload.</param>
    /// <param name="resources">The shared resource manager.</param>
    /// <param name="core">The particle core providing texture and sampler state.</param>
    private void BindTexture(
        SilkD3D12CommandContext context,
        SilkD3D12ResourceManager resources,
        ParticleRenderCore core
    ) {
        if (core.ParticleTexture is { } textureModel) {
            var texture = resources.GetOrCreate(context, textureModel);
            device.CreateShaderResourceView(texture.Resource, bindings.ShaderResource(TextureRegister));
        } else {
            device.CreateNullShaderResourceView(bindings.ShaderResource(TextureRegister));
        }
        device.CreateSampler(bindings.Sampler(SamplerRegister), core.SamplerDescription);
    }

    /// <summary>
    ///     Returns one unmanaged structure as bytes for upload writes.
    /// </summary>
    /// <typeparam name="T">The unmanaged structure type.</typeparam>
    /// <param name="value">The source structure.</param>
    /// <param name="resource">The upload resource receiving the bytes.</param>
    private static void Write<T>(SilkD3D12Resource resource, in T value) where T : unmanaged {
        var copy = value;
        resource.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref copy, 1)));
    }

    /// <summary>
    ///     Releases every native particle allocation and descriptor range.
    /// </summary>
    public void Dispose() {
        if (IsDisposed) return;
        foreach (var state in states ?? []) state.Dispose();
        foreach (var counter in counters ?? []) counter.Dispose();
        commandSignature?.Dispose();
        argumentsUpload?.Dispose();
        arguments?.Dispose();
        zeroUpload?.Dispose();
        frameUpload?.Dispose();
        frame?.Dispose();
        insert?.Dispose();
        model?.Dispose();
        globalTransforms?.Dispose();
        bindings?.Dispose();
        IsDisposed = true;
    }
}

using System.Runtime.InteropServices;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Shaders;
using Silk.NET.Direct3D12;

namespace HelixToolkit.SharpDX.Core.Native;
/// <summary>Owns one immutable dispatch generation, append output and GPU indirect draw in the host command engine.</summary>
internal sealed class SilkD3D12IntersectionResources : IDisposable {
    /// All owned allocations, released in reverse order after the host fence.
    private readonly List<IDisposable> owned = [];
    /// The existing device, borrowed from the render host.
    private readonly SilkD3D12Device device;
    /// The standard root signature used by the compute pipelines.
    private readonly SilkD3D12RootSignature root;
    /// The generic selected-pair compute pipeline.
    private readonly SilkD3D12PipelineState genericPipeline;
    /// The shared rendering-index compute pipeline.
    private readonly SilkD3D12PipelineState sharedPipeline;
    /// The separate append counter.
    private readonly SilkD3D12Resource counter;
    /// The immutable zero and initial indirect arguments upload.
    private readonly SilkD3D12Resource reset;
    /// The standard indirect draw signature.
    private readonly SilkD3D12CommandSignature signature;
    /// Per-batch immutable descriptor tables and constants, never overwritten by a later recorded dispatch.
    private readonly List<Batch> batches = [];
    /// Whether the immutable input generation has already been dispatched.
    private bool dispatched;
    /// Gets the exact input publication represented by these allocations.
    internal IReadOnlyList<IntersectionDispatchInput> Inputs { get; }
    /// Gets the maximum number of appended segments.
    internal uint Capacity { get; }
    /// Gets the GPU-generated segment buffer.
    internal SilkD3D12Resource Segments { get; }
    /// Gets the GPU-generated indirect arguments.
    internal SilkD3D12Resource Arguments { get; }
    /// Gets whether the host has released this generation after its fence.
    internal bool IsDisposed { get; private set; }
    /// <summary>
    ///     Allocates a complete immutable generation using the host's device, heaps and render-buffer manager.
    /// </summary>
    /// <param name="device">The borrowed host device.</param>
    /// <param name="resourceHeap">The host resource heap.</param>
    /// <param name="samplerHeap">The host sampler heap.</param>
    /// <param name="resources">The host geometry resource manager.</param>
    /// <param name="inputs">The immutable dispatch publication.</param>
    /// <param name="root">The host-owned standard compute root signature.</param>
    /// <param name="pipelines">The host-owned pipeline cache.</param>
    internal SilkD3D12IntersectionResources(SilkD3D12Device device, SilkD3D12DescriptorHeap resourceHeap,
        SilkD3D12DescriptorHeap samplerHeap, SilkD3D12ResourceManager resources,
        IReadOnlyList<IntersectionDispatchInput> inputs, SilkD3D12RootSignature root, D3D12PipelineStateCache pipelines) {
        this.device = device;
        Inputs = inputs;
        try {
            Capacity = checked((uint)Math.Max(1, inputs.Sum(input => input.Candidates.Length)));
            this.root = root;
            var genericShader = D3D12ShaderModule.Load("CS", "csMeshIntersect");
            var sharedShader = D3D12ShaderModule.Load("CS", "csMeshIntersectSharedRenderBuffer");
            genericPipeline = pipelines.GetOrCreate(D3D12PipelineStateKey.Compute(genericShader),
                () => device.CreateComputePipelineState(root, genericShader));
            sharedPipeline = pipelines.GetOrCreate(D3D12PipelineStateKey.Compute(sharedShader),
                () => device.CreateComputePipelineState(root, sharedShader));
            Segments = Own(device.CreateBuffer((ulong)Capacity * 28, flags: ResourceFlags.AllowUnorderedAccess));
            counter = Own(device.CreateBuffer(4, flags: ResourceFlags.AllowUnorderedAccess));
            Arguments = Own(device.CreateBuffer(16));
            reset = Upload<uint>([2, 0, 0, 0]);
            signature = Own(device.CreateDrawCommandSignature());
            var meshBuffers = new Dictionary<IntersectionMeshSnapshot, (SilkD3D12Resource Indices, SilkD3D12Resource Positions, bool Shared)>();
            foreach (var input in inputs.Where(input => input.Candidates.Length > 0)) {
                var bindings = Own(new SilkD3D12GraphicsBindings(resourceHeap, samplerHeap));
                var fallback = Own(device.CreateBuffer(256, HeapType.Upload));
                fallback.Write(new byte[256]);
                bindings.InitializeFallbackDescriptors(device, fallback, 0);
                var constants = Own(new SilkD3D12ConstantBuffer(device, bindings.ConstantBuffer(0), 144));
                var transforms = new IntersectionTransforms {
                    WorldA = input.WorldA, WorldB = input.WorldB, CandidateCount = checked((uint)input.Candidates.Length)
                };
                constants.Write(in transforms);
                var share = input.First.CanShare() && input.Second.CanShare();
                BindMesh(input.First, 0, share);
                BindMesh(input.Second, 2, share);
                var candidates = Upload<IntersectionCandidate>(input.Candidates);
                device.CreateStructuredBufferShaderResourceView(candidates, bindings.ShaderResource(4),
                    checked((uint)input.Candidates.Length), 16);
                device.CreateStructuredBufferUnorderedAccessView(Segments, counter, bindings.UnorderedAccess(0), Capacity, 28);
                batches.Add(new Batch(bindings, checked((uint)input.Candidates.Length), share));

                // A mesh snapshot owns original geometry identity, while the host retains borrowed rendering buffers.
                /// <summary>
                ///     Binds immutable original positions and either original int4 or borrowed flat indices.
                /// </summary>
                /// <param name="mesh">The immutable original source mesh.</param>
                /// <param name="register">The first source t-register for this mesh.</param>
                /// <param name="shared">Whether to borrow actual rendering indices.</param>
                void BindMesh(IntersectionMeshSnapshot mesh, int register, bool shared) {
                    if (!meshBuffers.TryGetValue(mesh, out var buffers) || buffers.Shared != shared) {
                        var positions = Upload<Vector4>(mesh.Positions);
                        SilkD3D12Resource indices;
                        if (shared) indices = resources.GetOrCreate(mesh.RenderBuffer!).IndexBuffer;
                        else {
                            var triangles = Enumerable.Range(0, mesh.TriangleCount)
                                .Select(index => new Int4(mesh.Indices[index * 3], mesh.Indices[index * 3 + 1], mesh.Indices[index * 3 + 2], 0))
                                .ToArray();
                            indices = Upload<Int4>(triangles);
                        }
                        buffers = (indices, positions, shared);
                        meshBuffers[mesh] = buffers;
                    }
                    if (shared) CreateIndexView(buffers.Indices, bindings.ShaderResource(register), checked((uint)mesh.Indices.Length));
                    else device.CreateStructuredBufferShaderResourceView(buffers.Indices, bindings.ShaderResource(register),
                        checked((uint)mesh.TriangleCount), 16);
                    device.CreateStructuredBufferShaderResourceView(buffers.Positions, bindings.ShaderResource(register + 1),
                        checked((uint)mesh.Positions.Length), 16);
                }
            }
        } catch {
            Dispose();
            throw;
        }
    }
    /// <summary>
    ///     Records the source kernels once, then restores the draw's heap pair for GPU-counted indirect output.
    /// </summary>
    /// <param name="context">The host's open command context.</param>
    /// <param name="pass">The source-compatible line or post-effect graphics pass.</param>
    /// <param name="bindings">The existing point/line material bindings.</param>
    /// <returns>Whether an indirect draw was recorded.</returns>
    internal bool Render(SilkD3D12CommandContext context, ShaderPass pass, SilkD3D12PointLineBindings bindings) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (!dispatched) {
            context.Transition(counter, ResourceStates.CopyDest);
            context.CopyBuffer(counter, 0, reset, 4, 4);
            context.Transition(counter, ResourceStates.UnorderedAccess);
            context.Transition(Segments, ResourceStates.UnorderedAccess);
            foreach (var batch in batches) {
                context.SetComputePipeline(root, batch.Shared ? sharedPipeline : genericPipeline);
                batch.Bindings.BindCompute(context);
                context.Dispatch((batch.Count + 511) / 512, 1, 1);
                context.UavBarrier(Segments);
                context.UavBarrier(counter);
            }
            context.Transition(Arguments, ResourceStates.CopyDest);
            context.CopyBuffer(Arguments, 0, reset, 0, 16);
            context.Transition(counter, ResourceStates.CopySource);
            context.CopyBuffer(Arguments, 4, counter, 0, 4);
            dispatched = true;
        }
        context.Transition(Arguments, ResourceStates.IndirectArgument);
        context.Transition(Segments, ResourceStates.NonPixelShaderResource);
        bindings.BindSegments(Segments, Capacity);
        pass.BindShader(context);
        bindings.BindHeaps(context);
        context.SetGraphicsDescriptorTables(bindings.ResourceTableStart, bindings.SamplerTableStart);
        context.DrawInstancedIndirect(signature, Arguments);
        return true;
    }
    /// <summary>
    ///     Checks borrowed owners before reusing a shared rendering-index generation.
    /// </summary>
    /// <returns>Whether every shared borrowed owner still matches its immutable source snapshot.</returns>
    internal bool CanReuse() => Inputs.Where(input => input.Candidates.Length > 0).Zip(batches)
        .All(pair => pair.Second.Shared == (pair.First.First.CanShare() && pair.First.Second.CanShare()));
    /// <summary>
    ///     Creates a typed uint SRV over the actual render index allocation.
    /// </summary>
    /// <param name="buffer">The borrowed render index buffer.</param>
    /// <param name="descriptor">The host-owned descriptor slot.</param>
    /// <param name="count">The number of original indices.</param>
    private unsafe void CreateIndexView(SilkD3D12Resource buffer, SilkD3D12Descriptor descriptor, uint count) {
        var description = new ShaderResourceViewDesc {
            Format = Format.FormatR32Uint, ViewDimension = SrvDimension.Buffer, Shader4ComponentMapping = 5768,
            Buffer = new BufferSrv { NumElements = count }
        };
        device.NativeDevice.CreateShaderResourceView(buffer.Handle, in description, descriptor.CpuHandle);
    }
    /// <summary>
    ///     Creates and owns an immutable upload allocation.
    /// </summary>
    /// <typeparam name="T">The exact shader element type.</typeparam>
    /// <param name="values">The immutable source payload.</param>
    /// <returns>The host-device upload resource.</returns>
    private SilkD3D12Resource Upload<T>(ReadOnlySpan<T> values) where T : unmanaged {
        var bytes = MemoryMarshal.AsBytes(values);
        var buffer = Own(device.CreateBuffer(checked((ulong)Math.Max(4, bytes.Length)), HeapType.Upload));
        buffer.Write(bytes);
        return buffer;
    }
    /// <summary>
    ///     Registers an allocation immediately so partial initialization also releases it.
    /// </summary>
    /// <typeparam name="T">The owned resource type.</typeparam>
    /// <param name="resource">The newly created allocation.</param>
    /// <returns>The same allocation.</returns>
    private T Own<T>(T resource) where T : IDisposable { owned.Add(resource); return resource; }
    /// <summary>Releases one completed generation and all of its descriptors exactly once.</summary>
    public void Dispose() {
        if (IsDisposed) return;
        for (var index = owned.Count - 1; index >= 0; index--) owned[index].Dispose();
        owned.Clear();
        IsDisposed = true;
    }
    /// <summary>
    ///     Retains independent per-dispatch bindings until the host fence completes.
    /// </summary>
    /// <param name="Bindings">The immutable root descriptor tables.</param>
    /// <param name="Count">The valid candidate count.</param>
    /// <param name="Shared">Whether this batch consumes actual rendering index buffers.</param>
    private sealed record Batch(SilkD3D12GraphicsBindings Bindings, uint Count, bool Shared);
}

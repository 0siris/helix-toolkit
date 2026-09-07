/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Model;
using Silk.NET.Direct3D12;

namespace HelixToolkit.SharpDX.Core.Native;

/// <summary>
///     Owns the per-core Direct3D 12 resources used by the bone-skinning stream-output precompute pass.
/// </summary>
internal sealed class SilkD3D12BoneSkinResources : IDisposable {
    /// <summary>
    ///     The b-register containing morph-target count and pitch.
    /// </summary>
    internal const int MorphTargetConstantRegister = 9;

    /// <summary>
    ///     The t-register containing bone matrices.
    /// </summary>
    internal const int BoneMatrixRegister = 40;

    /// <summary>
    ///     The t-register containing morph-target weights.
    /// </summary>
    internal const int MorphTargetWeightRegister = 60;

    /// <summary>
    ///     The t-register containing compact morph-target deltas.
    /// </summary>
    internal const int MorphTargetDeltaRegister = 61;

    /// <summary>
    ///     The t-register containing compact-delta offsets.
    /// </summary>
    internal const int MorphTargetOffsetRegister = 62;

    /// <summary>
    ///     The Direct3D 12 device used to allocate and describe resources.
    /// </summary>
    private readonly SilkD3D12Device device;

    /// <summary>
    ///     The borrowed per-core descriptor table.
    /// </summary>
    private readonly SilkD3D12GraphicsBindings bindings;

    /// <summary>
    ///     The b9 morph-target constants.
    /// </summary>
    private readonly SilkD3D12ConstantBuffer morphTargetConstants;

    /// <summary>
    ///     The reusable upload source used to reset the stream-output filled-size counter.
    /// </summary>
    private readonly SilkD3D12Resource filledSizeReset;

    /// <summary>
    ///     The GPU-written stream-output filled-size counter.
    /// </summary>
    private readonly SilkD3D12Resource filledSize;

    /// <summary>
    ///     The vertex bone identifiers and weights used by the input assembler.
    /// </summary>
    private SilkD3D12Resource? boneIds;

    /// <summary>
    ///     The t40 bone-matrix upload buffer.
    /// </summary>
    private SilkD3D12Resource? boneMatrices;

    /// <summary>
    ///     The t60 morph-target-weight upload buffer.
    /// </summary>
    private SilkD3D12Resource? morphTargetWeights;

    /// <summary>
    ///     The t61 compact morph-target-delta upload buffer.
    /// </summary>
    private SilkD3D12Resource? morphTargetDeltas;

    /// <summary>
    ///     The t62 compact-delta-offset upload buffer.
    /// </summary>
    private SilkD3D12Resource? morphTargetOffsets;

    /// <summary>
    ///     The default-heap stream-output vertex buffer.
    /// </summary>
    private SilkD3D12Resource? output;

    /// <summary>
    ///     Initializes an empty resource set over one existing per-core descriptor table.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="bindings">The borrowed graphics descriptor table.</param>
    internal SilkD3D12BoneSkinResources(
        SilkD3D12Device device,
        SilkD3D12GraphicsBindings bindings
    ) {
        device.GuardNotNull();
        bindings.GuardNotNull();
        this.device = device;
        this.bindings = bindings;
        morphTargetConstants = new SilkD3D12ConstantBuffer(device,
            bindings.ConstantBuffer(MorphTargetConstantRegister),
            16);
        SilkD3D12Resource? createdReset = null;
        try {
            createdReset = device.CreateBuffer(sizeof(uint), HeapType.Upload);
            createdReset.Write(new byte[sizeof(uint)]);
            var createdFilledSize = device.CreateBuffer(sizeof(uint));
            filledSizeReset = createdReset;
            filledSize = createdFilledSize;
        } catch {
            createdReset?.Dispose();
            morphTargetConstants.Dispose();
            throw;
        }
    }

    /// <summary>
    ///     Gets whether every owned resource has been released.
    /// </summary>
    internal bool IsDisposed { get; private set; }

    /// <summary>
    ///     Gets the logical stream-output byte size for the current mesh.
    /// </summary>
    internal ulong OutputSizeInBytes { get; private set; }

    /// <summary>
    ///     Gets the current stream-output allocation.
    /// </summary>
    internal SilkD3D12Resource Output => output
        ?? throw new InvalidOperationException("Bone-skinning resources have not been initialized.");

    /// <summary>
    ///     Gets the b9 resource for focused byte-layout verification.
    /// </summary>
    internal SilkD3D12Resource MorphTargetConstantResource => morphTargetConstants.Resource;

    /// <summary>
    ///     Gets the vertex bone identifiers and weights.
    /// </summary>
    internal SilkD3D12Resource BoneIdResource => boneIds
        ?? throw new InvalidOperationException("Bone-skinning resources have not been initialized.");

    /// <summary>
    ///     Gets the GPU-written stream-output byte count.
    /// </summary>
    internal SilkD3D12Resource FilledSizeResource => filledSize;

    /// <summary>
    ///     Gets the t40 resource for focused upload verification.
    /// </summary>
    internal SilkD3D12Resource? BoneMatrixResource => boneMatrices;

    /// <summary>
    ///     Gets the t60 resource for focused upload verification.
    /// </summary>
    internal SilkD3D12Resource? MorphTargetWeightResource => morphTargetWeights;

    /// <summary>
    ///     Gets the t61 resource for focused upload verification.
    /// </summary>
    internal SilkD3D12Resource? MorphTargetDeltaResource => morphTargetDeltas;

    /// <summary>
    ///     Gets the t62 resource for focused upload verification.
    /// </summary>
    internal SilkD3D12Resource? MorphTargetOffsetResource => morphTargetOffsets;

    /// <summary>
    ///     Uploads the current skinning payload and creates or reuses the matching stream-output allocation.
    /// </summary>
    /// <param name="vertexBoneIds">The vertex bone identifiers and weights.</param>
    /// <param name="matrices">The current bone matrices.</param>
    /// <param name="morphTargets">The current morph-target payload.</param>
    /// <param name="vertexCount">The number of output vertices.</param>
    internal void Update(
        ReadOnlySpan<BoneIds> vertexBoneIds,
        ReadOnlySpan<Matrix> matrices,
        MorphTargetUploaderCore morphTargets,
        int vertexCount
    ) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        morphTargets.GuardNotNull();
        vertexCount.Guard().Range(1, int.MaxValue);
        ValidateBones(vertexBoneIds, matrices, vertexCount);
        ValidateMorphTargets(morphTargets, vertexCount);

        UpdateUploadBuffer(ref boneIds, vertexBoneIds);
        UpdateStructuredBuffer(ref boneMatrices, matrices, BoneMatrixRegister);
        if (morphTargets.HasMorphTarget) {
            UpdateStructuredBuffer(ref morphTargetWeights,
                morphTargets.D3D12Weights[..morphTargets.MorphTargetCount],
                MorphTargetWeightRegister);
            UpdateStructuredBuffer(ref morphTargetDeltas,
                morphTargets.D3D12Deltas,
                MorphTargetDeltaRegister);
            UpdateStructuredBuffer(ref morphTargetOffsets,
                morphTargets.D3D12Offsets,
                MorphTargetOffsetRegister);
        } else {
            ClearStructuredBuffer(ref morphTargetWeights, MorphTargetWeightRegister);
            ClearStructuredBuffer(ref morphTargetDeltas, MorphTargetDeltaRegister);
            ClearStructuredBuffer(ref morphTargetOffsets, MorphTargetOffsetRegister);
        }

        Span<int> constants = stackalloc int[4] {
            morphTargets.MorphTargetCount,
            morphTargets.MorphTargetPitch,
            0,
            0
        };
        morphTargetConstants.Write(constants);

        OutputSizeInBytes = checked((ulong) vertexCount * DefaultVertex.SizeInBytes);
        if (output is null || output.SizeInBytes < OutputSizeInBytes) {
            var replacement = device.CreateBuffer(OutputSizeInBytes);
            output?.Dispose();
            output = replacement;
        }
    }

    /// <summary>
    ///     Transitions and binds the current vertex allocation as the stream-output target.
    /// </summary>
    /// <param name="context">The open command context.</param>
    internal void BindOutput(SilkD3D12CommandContext context) {
        context.GuardNotNull();
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        context.Transition(filledSize, ResourceStates.CopyDest);
        context.CopyBuffer(filledSize, 0, filledSizeReset, 0, sizeof(uint));
        context.Transition(Output, ResourceStates.StreamOut);
        context.Transition(filledSize, ResourceStates.StreamOut);
        context.SetStreamOutputTarget(Output, filledSize, OutputSizeInBytes);
    }

    /// <summary>
    ///     Unbinds the stream-output target and transitions it for vertex input.
    /// </summary>
    /// <param name="context">The open command context.</param>
    internal void UnbindOutput(SilkD3D12CommandContext context) {
        context.GuardNotNull();
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        context.SetStreamOutputTarget(null);
        context.Transition(Output, ResourceStates.VertexAndConstantBuffer);
    }

    /// <summary>
    ///     Releases the output, structured uploads, and b9 allocation.
    /// </summary>
    public void Dispose() {
        if (IsDisposed) return;
        output?.Dispose();
        boneIds?.Dispose();
        morphTargetOffsets?.Dispose();
        morphTargetDeltas?.Dispose();
        morphTargetWeights?.Dispose();
        boneMatrices?.Dispose();
        filledSize.Dispose();
        filledSizeReset.Dispose();
        morphTargetConstants.Dispose();
        IsDisposed = true;
    }

    /// <summary>
    ///     Creates, grows, or updates one structured upload buffer and its borrowed SRV.
    /// </summary>
    /// <typeparam name="T">The unmanaged structured element type.</typeparam>
    /// <param name="resource">The current owned resource.</param>
    /// <param name="values">The values to upload.</param>
    /// <param name="shaderRegister">The destination t-register.</param>
    private void UpdateStructuredBuffer<T>(
        ref SilkD3D12Resource? resource,
        ReadOnlySpan<T> values,
        int shaderRegister
    ) where T : unmanaged {
        if (values.IsEmpty) {
            ClearStructuredBuffer(ref resource, shaderRegister);
            return;
        }

        var bytes = MemoryMarshal.AsBytes(values);
        if (resource is null || resource.SizeInBytes < (ulong) bytes.Length) {
            var replacement = device.CreateBuffer(checked((ulong) bytes.Length), HeapType.Upload);
            try {
                replacement.Write(bytes);
                device.CreateStructuredBufferShaderResourceView(replacement,
                    bindings.ShaderResource(shaderRegister),
                    checked((uint) values.Length),
                    checked((uint) Unsafe.SizeOf<T>()));
            } catch {
                replacement.Dispose();
                throw;
            }
            resource?.Dispose();
            resource = replacement;
            return;
        }

        resource.Write(bytes);
        device.CreateStructuredBufferShaderResourceView(resource,
            bindings.ShaderResource(shaderRegister),
            checked((uint) values.Length),
            checked((uint) Unsafe.SizeOf<T>()));
    }

    /// <summary>
    ///     Releases one optional structured buffer and restores its null descriptor.
    /// </summary>
    /// <param name="resource">The current owned resource.</param>
    /// <param name="shaderRegister">The destination t-register.</param>
    private void ClearStructuredBuffer(ref SilkD3D12Resource? resource, int shaderRegister) {
        device.CreateNullShaderResourceView(bindings.ShaderResource(shaderRegister));
        resource?.Dispose();
        resource = null;
    }

    /// <summary>
    ///     Creates, grows, or updates one input-assembler upload buffer.
    /// </summary>
    /// <typeparam name="T">The unmanaged vertex element type.</typeparam>
    /// <param name="resource">The current owned resource.</param>
    /// <param name="values">The values to upload.</param>
    private void UpdateUploadBuffer<T>(ref SilkD3D12Resource? resource, ReadOnlySpan<T> values)
        where T : unmanaged {
        var bytes = MemoryMarshal.AsBytes(values);
        if (resource is null || resource.SizeInBytes < (ulong) bytes.Length) {
            var replacement = device.CreateBuffer(checked((ulong) bytes.Length), HeapType.Upload);
            try {
                replacement.Write(bytes);
            } catch {
                replacement.Dispose();
                throw;
            }
            resource?.Dispose();
            resource = replacement;
            return;
        }
        resource.Write(bytes);
    }

    /// <summary>
    ///     Validates input-assembler bone data before the GPU can index the structured matrix buffer.
    /// </summary>
    /// <param name="vertexBoneIds">The vertex bone identifiers and weights.</param>
    /// <param name="matrices">The available bone matrices.</param>
    /// <param name="vertexCount">The required vertex count.</param>
    private static void ValidateBones(
        ReadOnlySpan<BoneIds> vertexBoneIds,
        ReadOnlySpan<Matrix> matrices,
        int vertexCount
    ) {
        if (vertexBoneIds.Length != vertexCount)
            throw new InvalidOperationException("Bone-skinned geometry requires one bone identifier per vertex.");
        if (matrices.IsEmpty)
            throw new InvalidOperationException("GPU bone skinning requires at least one bone matrix.");
        foreach (var boneId in vertexBoneIds) {
            ValidateBoneIndex(boneId.Bone1, matrices.Length);
            ValidateBoneIndex(boneId.Bone2, matrices.Length);
            ValidateBoneIndex(boneId.Bone3, matrices.Length);
            ValidateBoneIndex(boneId.Bone4, matrices.Length);
        }
    }

    /// <summary>
    ///     Validates one bone index used unconditionally by the skinning shader.
    /// </summary>
    /// <param name="index">The matrix index.</param>
    /// <param name="matrixCount">The available matrix count.</param>
    private static void ValidateBoneIndex(int index, int matrixCount) {
        if ((uint) index >= (uint) matrixCount)
            throw new ArgumentOutOfRangeException(nameof(index), "A bone index is outside the matrix buffer.");
    }

    /// <summary>
    ///     Validates the existing compact morph-target payload against the output mesh.
    /// </summary>
    /// <param name="morphTargets">The morph-target owner.</param>
    /// <param name="vertexCount">The number of output vertices.</param>
    private static void ValidateMorphTargets(MorphTargetUploaderCore morphTargets, int vertexCount) {
        if (!morphTargets.HasMorphTarget) return;
        if (morphTargets.MorphTargetPitch != vertexCount)
            throw new InvalidOperationException("The morph-target pitch must match the stream-output vertex count.");
        if (morphTargets.D3D12Weights.Length < morphTargets.MorphTargetCount)
            throw new InvalidOperationException("Every morph target requires one weight.");
        if (morphTargets.D3D12Offsets.Length !=
            checked(morphTargets.MorphTargetCount * morphTargets.MorphTargetPitch))
            throw new InvalidOperationException("The morph-target offset table is incomplete.");
        if (morphTargets.D3D12Deltas.Length < 3)
            throw new InvalidOperationException("The morph-target delta buffer is incomplete.");
        foreach (var offset in morphTargets.D3D12Offsets)
            if ((uint) offset > (uint) (morphTargets.D3D12Deltas.Length - 3))
                throw new InvalidOperationException("A morph-target offset is outside the delta buffer.");
    }
}

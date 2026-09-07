/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.InteropServices;
using HelixToolkit.SharpDX.Core.Core.Buffers;
using HelixToolkit.SharpDX.Core.DefaultShaders;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Model.Lights;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core.Core;

public class BoneSkinRenderCore : MeshRenderCore {
    private readonly BoneUploaderCore internalBoneBuffer = new();
    private readonly MorphTargetUploaderCore internalMtBuffer = new();

    private int boneSkinSbSlot;
    private bool matricsChanged = true;

    private bool mtChanged;
    private int mtDeltasBSlot;
    private int mtOffsetsBSlot;
    private int mtWeightsBSlot;
    private IBoneSkinPreComputehBufferModel? preComputeBoneBuffer;
    private ShaderPass preComputeBoneSkinPass = ShaderPass.NullPass;

    private BoneUploaderCore? sharedBoneBuffer;

    /// <summary>
    ///     Records the existing bone-skinned mesh through the productive Direct3D 12 mesh path.
    /// </summary>
    /// <param name="context">The open Direct3D 12 command context.</param>
    /// <param name="resources">The render-host resource manager.</param>
    /// <param name="pass">The selected Direct3D 12 material pass.</param>
    /// <param name="bindings">The in-flight mesh bindings.</param>
    /// <param name="transforms">The global camera and viewport transforms.</param>
    /// <param name="lights">The optional shared light model.</param>
    /// <param name="environmentMap">The optional shared environment cube map.</param>
    /// <returns>Whether the skinned mesh draw was recorded.</returns>
    internal override bool TryRenderD3D12(
        SilkD3D12CommandContext context,
        SilkD3D12ResourceManager resources,
        ShaderPass pass,
        SilkD3D12MeshBindings bindings,
        in GlobalTransformStruct transforms,
        LightsBufferModel? lights = null,
        TextureModel? environmentMap = null
    ) => TryRenderD3D12(context,
        resources,
        pass,
        bindings,
        in transforms,
        lights,
        environmentMap,
        null);

    /// <summary>
    ///     Records GPU bone/morph precompute followed by the productive material draw.
    /// </summary>
    /// <param name="context">The open Direct3D 12 command context.</param>
    /// <param name="resources">The render-host resource manager.</param>
    /// <param name="pass">The selected Direct3D 12 material pass.</param>
    /// <param name="bindings">The in-flight mesh bindings.</param>
    /// <param name="transforms">The global camera and viewport transforms.</param>
    /// <param name="lights">The optional shared light model.</param>
    /// <param name="environmentMap">The optional shared environment cube map.</param>
    /// <param name="preComputePass">The existing stream-output precompute pass.</param>
    /// <returns>Whether the skinned mesh draw was recorded.</returns>
    internal bool TryRenderD3D12(
        SilkD3D12CommandContext context,
        SilkD3D12ResourceManager resources,
        ShaderPass pass,
        SilkD3D12MeshBindings bindings,
        in GlobalTransformStruct transforms,
        LightsBufferModel? lights,
        TextureModel? environmentMap,
        ShaderPass? preComputePass
    ) {
        if (!CanRenderFlag || D3D12Material is null ||
            preComputeBoneBuffer is not BoneSkinPreComputeBufferModel preCompute)
            return false;

        OnUpdatePerModelStructD3D12();
        if (lights is not null) bindings.UpdateLights(lights);
        bindings.Update(context, resources, in transforms, in ModelStruct, D3D12Material, environmentMap);
        var source = preCompute.SourceMeshBuffer;
        var buffers = resources.GetOrCreate(source);
        var boneMatrices = (SharedBoneBuffer ?? internalBoneBuffer).BoneMatrices;
        SilkD3D12BoneSkinResources? skinning = null;
        if (boneMatrices.Length > 0) {
            if (preComputePass is null || preComputePass.IsNull) return false;
            if (!preComputePass.IsD3D12)
                throw new ArgumentException("The precompute pass must own a Direct3D 12 pipeline.",
                    nameof(preComputePass));
            if (source.Geometry is not BoneSkinnedMeshGeometry3D {
                Positions: { Count: > 0 } positions,
                VertexBoneIds: { } boneIds
            } || positions.Count != boneIds.Count)
                throw new InvalidOperationException(
                    "Bone-skinned geometry requires one bone identifier per vertex.");

            ReadOnlySpan<BoneIds> boneIdSpan;
            if (boneIds is BoneIds[] boneIdArray)
                boneIdSpan = boneIdArray;
            else if (boneIds is List<BoneIds> boneIdList)
                boneIdSpan = CollectionsMarshal.AsSpan(boneIdList);
            else
                boneIdSpan = boneIds.ToArray();

            skinning = bindings.UpdateBoneSkinning(boneIdSpan,
                boneMatrices,
                internalMtBuffer,
                positions.Count);
            preComputePass.BindShader(context);
            context.SetGraphicsDescriptorTables(bindings.ResourceTableStart, bindings.SamplerTableStart);
            buffers.BindBoneSkinningInput(context, skinning.BoneIdResource);
            skinning.BindOutput(context);
            try {
                context.DrawInstanced(buffers.VertexCount);
            } finally {
                skinning.UnbindOutput(context);
            }
        }

        pass.BindShader(context);
        context.SetGraphicsDescriptorTables(bindings.ResourceTableStart, bindings.SamplerTableStart);
        var instances = InstanceBuffer is IElementsBufferModel<Matrix> matrixInstances
            ? resources.GetOrCreate(matrixInstances)
            : null;
        if (skinning is null) {
            if (instances is null)
                DrawIndexed(context, buffers, topology: pass.Topology);
            else
                DrawIndexed(context, buffers, instances, topology: pass.Topology);
        } else {
            var instanceSlot = buffers.BindSkinned(context, skinning.Output, skinning.OutputSizeInBytes);
            if (instances is not null) instances.Bind(context, instanceSlot);
            context.SetPrimitiveTopology(pass.Topology);
            context.DrawIndexedInstanced(buffers.IndexCount, instances?.ElementCount ?? 1);
        }
        matricsChanged = false;
        mtChanged = false;
        return true;
    }

    public BoneSkinRenderCore() {
        internalBoneBuffer.BoneChanged += OnBoneChanged;
    }

    public Matrix[] BoneMatrices {
        get => internalBoneBuffer.BoneMatrices;
        set => internalBoneBuffer.BoneMatrices = value;
    }

    public float[] MorphTargetWeights {
        get => internalMtBuffer.MorphTargetWeights;
        set {
            internalMtBuffer.MorphTargetWeights = value;
            mtChanged = true;
        }
    }

    public BoneUploaderCore? SharedBoneBuffer {
        get => sharedBoneBuffer;
        set {
            var old = sharedBoneBuffer;
            if (Set(ref sharedBoneBuffer, value)) {
                old?.BoneChanged -= OnBoneChanged;
                value?.BoneChanged += OnBoneChanged;
                matricsChanged = true;
            }
        }
    }

    private void OnBoneChanged(object? sender, EventArgs e) {
        matricsChanged = true;
        RaiseInvalidateRender();
    }

    protected override void OnGeometryBufferChanged(IAttachableBufferModel? buffer) {
        base.OnGeometryBufferChanged(buffer);
        preComputeBoneBuffer = buffer as IBoneSkinPreComputehBufferModel;
    }

    public bool InitializeMorphTargets(MorphTargetVertex[] targets, int pitch)
        => internalMtBuffer.InitializeMorphTargets(targets, pitch);

    /// <summary>
    ///     Copies the current CPU-prepared skinned positions into the destination array.
    /// </summary>
    internal int CopySkinnedToArray(Vector3[] destination) {
        if (preComputeBoneBuffer is not BoneSkinPreComputeBufferModel source) return 0;
        return SilkD3D12DefaultMeshBuffers.CopySkinnedPositions(source.SourceMeshBuffer,
            (SharedBoneBuffer ?? internalBoneBuffer).BoneMatrices,
            internalMtBuffer,
            destination);
    }

    public void SetWeight(int i, float w) {
        mtChanged = true;
        internalMtBuffer.SetWeight(i, w);
    }

    public void InvalidateBoneMatrices()
        => matricsChanged = true;

    public void InvalidateMorphTargetWeights() {
        mtChanged = true;
        internalMtBuffer.InvalidateWeight();
    }
}

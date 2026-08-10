/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Render;
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
    private ShaderPass preComputeBoneSkinPass;

    private BoneUploaderCore sharedBoneBuffer;

    public BoneSkinRenderCore() {
        NeedUpdate = true;
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

    public BoneUploaderCore SharedBoneBuffer {
        get => sharedBoneBuffer;
        set {
            var old = sharedBoneBuffer;
            if (Set(ref sharedBoneBuffer, value)) {
                old.BoneChanged -= OnBoneChanged;
                value.BoneChanged += OnBoneChanged;
                matricsChanged = true;
            }
        }
    }

    protected override bool OnAttach(IRenderTechnique technique) {
        if (!base.OnAttach(technique)) 
            return false;
        
        matricsChanged = true;
        preComputeBoneSkinPass = technique[DefaultPassNames.PreComputeMeshBoneSkinned];
        boneSkinSbSlot = preComputeBoneSkinPass.VertexShader.ShaderResourceViewMapping
                                               .GetMapping(DefaultBufferNames.BoneSkinSb).Slot;
            
        mtWeightsBSlot = preComputeBoneSkinPass.VertexShader.ShaderResourceViewMapping
                                               .GetMapping(DefaultBufferNames.MtWeightsB).Slot;
            
        mtDeltasBSlot = preComputeBoneSkinPass.VertexShader.ShaderResourceViewMapping
                                              .GetMapping(DefaultBufferNames.MtDeltasB).Slot;
            
        mtOffsetsBSlot = preComputeBoneSkinPass.VertexShader.ShaderResourceViewMapping
                                               .GetMapping(DefaultBufferNames.MtOffsetsB).Slot;
        
        internalBoneBuffer.Attach(technique);
        internalMtBuffer.Attach(technique);
        return true;

    }

    private void OnBoneChanged(object? sender, EventArgs e) {
        matricsChanged = true;
        RaiseInvalidateRender();
    }

    protected override void OnGeometryBufferChanged(IAttachableBufferModel? buffer) {
        base.OnGeometryBufferChanged(buffer);
        preComputeBoneBuffer = buffer as IBoneSkinPreComputehBufferModel;
    }

    protected override void OnUpdate(RenderContext context, DeviceContextProxy deviceContext) {
        //Skip if not ready
        if (preComputeBoneSkinPass.IsNull || preComputeBoneBuffer is not {CanPreCompute: true})
            return;

        //Skip if not necessary
        if (!matricsChanged && !mtChanged)
            return;

        var boneBuffer = sharedBoneBuffer ?? internalBoneBuffer;

        if (boneBuffer.BoneMatrices.Length == 0 && !mtChanged) {
            preComputeBoneBuffer.ResetSkinnedVertexBuffer(deviceContext);
        } else {
            GeometryBuffer.UpdateBuffers(deviceContext, EffectTechnique.EffectsManager);
            preComputeBoneBuffer.BindSkinnedVertexBufferToOutput(deviceContext);
            boneBuffer.Update(context, deviceContext);
            internalMtBuffer.Update(context, deviceContext);
            preComputeBoneSkinPass.BindShader(deviceContext);
            boneBuffer.BindBuffer(deviceContext, boneSkinSbSlot);
            internalMtBuffer.BindBuffers(deviceContext, mtWeightsBSlot, mtDeltasBSlot, mtOffsetsBSlot);
            deviceContext.Draw(GeometryBuffer.VertexBuffer[0].ElementCount, 0);
            preComputeBoneBuffer.UnBindSkinnedVertexBufferToOutput(deviceContext);
        }

        matricsChanged = false;
    }

    protected override void OnDetach() {
        preComputeBoneBuffer = null;
        internalBoneBuffer.Detach();
        internalMtBuffer.Detach();
        base.OnDetach();
    }

    public int CopySkinnedToArray(DeviceContextProxy context, Vector3[] array) 
        => preComputeBoneBuffer.CopySkinnedToArray(context, array);

    public bool InitializeMorphTargets(MorphTargetVertex[] targets, int pitch) 
        => internalMtBuffer.InitializeMorphTargets(targets, pitch);

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
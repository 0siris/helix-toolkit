/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core {
    namespace Core {
        public class BoneSkinRenderCore : MeshRenderCore {
            private readonly BoneUploaderCore internalBoneBuffer = new();
            private readonly MorphTargetUploaderCore internalMTBuffer = new();

            private int boneSkinSBSlot;
            private bool matricsChanged = true;

            private bool mtChanged;
            private int mtDeltasBSlot;
            private int mtOffsetsBSlot;
            private int mtWeightsBSlot;
            private IBoneSkinPreComputehBufferModel preComputeBoneBuffer;
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
                get => internalMTBuffer.MorphTargetWeights;
                set {
                    internalMTBuffer.MorphTargetWeights = value;
                    mtChanged = true;
                }
            }

            public BoneUploaderCore SharedBoneBuffer {
                get => sharedBoneBuffer;
                set {
                    var old = sharedBoneBuffer;
                    if (Set(ref sharedBoneBuffer, value)) {
                        if (old != null) old.BoneChanged -= OnBoneChanged;
                        if (value != null) value.BoneChanged += OnBoneChanged;
                        matricsChanged = true;
                    }
                }
            }

            protected override bool OnAttach(IRenderTechnique technique) {
                if (base.OnAttach(technique)) {
                    matricsChanged = true;
                    preComputeBoneSkinPass = technique[DefaultPassNames.PreComputeMeshBoneSkinned];
                    boneSkinSBSlot = preComputeBoneSkinPass.VertexShader.ShaderResourceViewMapping
                                                           .GetMapping(DefaultBufferNames.BoneSkinSB).Slot;
                    mtWeightsBSlot = preComputeBoneSkinPass.VertexShader.ShaderResourceViewMapping
                                                           .GetMapping(DefaultBufferNames.MTWeightsB).Slot;
                    mtDeltasBSlot = preComputeBoneSkinPass.VertexShader.ShaderResourceViewMapping
                                                          .GetMapping(DefaultBufferNames.MTDeltasB).Slot;
                    mtOffsetsBSlot = preComputeBoneSkinPass.VertexShader.ShaderResourceViewMapping
                                                           .GetMapping(DefaultBufferNames.MTOffsetsB).Slot;
                    internalBoneBuffer.Attach(technique);
                    internalMTBuffer.Attach(technique);
                    return true;
                }

                return false;
            }

            private void OnBoneChanged(object sender, EventArgs e) {
                matricsChanged = true;
                RaiseInvalidateRender();
            }

            protected override void OnGeometryBufferChanged(IAttachableBufferModel buffer) {
                base.OnGeometryBufferChanged(buffer);
                preComputeBoneBuffer = buffer as IBoneSkinPreComputehBufferModel;
            }

            protected override void OnUpdate(RenderContext context, DeviceContextProxy deviceContext) {
                //Skip if not ready
                if (preComputeBoneSkinPass.IsNULL || preComputeBoneBuffer == null ||
                    !preComputeBoneBuffer.CanPreCompute)
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
                    internalMTBuffer.Update(context, deviceContext);
                    preComputeBoneSkinPass.BindShader(deviceContext);
                    boneBuffer.BindBuffer(deviceContext, boneSkinSBSlot);
                    internalMTBuffer.BindBuffers(deviceContext, mtWeightsBSlot, mtDeltasBSlot, mtOffsetsBSlot);
                    deviceContext.Draw(GeometryBuffer.VertexBuffer[0].ElementCount, 0);
                    preComputeBoneBuffer.UnBindSkinnedVertexBufferToOutput(deviceContext);
                }

                matricsChanged = false;
            }

            protected override void OnDetach() {
                preComputeBoneBuffer = null;
                internalBoneBuffer.Detach();
                internalMTBuffer.Detach();
                base.OnDetach();
            }

            public int CopySkinnedToArray(DeviceContextProxy context, Vector3[] array) {
                return preComputeBoneBuffer.CopySkinnedToArray(context, array);
            }

            public bool InitializeMorphTargets(MorphTargetVertex[] targets, int pitch) {
                return internalMTBuffer.InitializeMorphTargets(targets, pitch);
            }

            public void SetWeight(int i, float w) {
                mtChanged = true;
                internalMTBuffer.SetWeight(i, w);
            }

            public void InvalidateBoneMatrices() {
                matricsChanged = true;
            }

            public void InvalidateMorphTargetWeights() {
                mtChanged = true;
                internalMTBuffer.InvalidateWeight();
            }
        }
    }
}

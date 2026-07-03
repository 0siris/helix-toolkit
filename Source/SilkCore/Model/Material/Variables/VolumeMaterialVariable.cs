/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core {
    namespace Model {
        public class VolumeMaterialVariable<T> : MaterialVariable {
            private readonly VolumeTextureMaterialCoreBase<T> material;
            private readonly int samplerSlot;
            private readonly int texSlot, gradientSlot;
            private readonly ShaderPass volumePass;


            public Func<VolumeTextureMaterialCoreBase<T>, IEffectsManager, ShaderResourceViewProxy> OnCreateTexture;
            private SamplerStateProxy sampler;
            private ShaderResourceViewProxy texture;
            private ShaderResourceViewProxy transferMap;

            public VolumeMaterialVariable(
                IEffectsManager manager,
                IRenderTechnique technique,
                VolumeTextureMaterialCoreBase<T> material,
                string volumePassName = DefaultPassNames.Default
            )
                : base(manager, technique, DefaultVolumeConstantBufferDesc, material) {
                this.material = material;
                volumePass = technique[volumePassName];
                texSlot = volumePass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(DefaultBufferNames.VolumeTB);
                gradientSlot =
                    volumePass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(DefaultBufferNames
                        .ColorStripe1DXTB);
                samplerSlot =
                    volumePass.PixelShader.SamplerMapping.TryGetBindSlot(DefaultSamplerStateNames.VolumeSampler);
            }

            protected override void OnInitialPropertyBindings() {
                base.OnInitialPropertyBindings();
                AddPropertyBinding(nameof(VolumeTextureMaterialCoreBase<T>.VolumeTexture),
                                   () => { UpdateTexture(material); });
                AddPropertyBinding(nameof(IVolumeTextureMaterial.Sampler),
                                   () => {
                                       var newSampler = EffectsManager.StateManager.Register(material.Sampler);
                                       RemoveAndDispose(ref sampler);
                                       sampler = newSampler;
                                   });
                AddPropertyBinding(nameof(IVolumeTextureMaterial.SampleDistance),
                                   () => UpdateStepSize());
                AddPropertyBinding(nameof(IVolumeTextureMaterial.MaxIterations),
                                   () => WriteValue(VolumeParamsStruct.MaxIterations, material.MaxIterations));
                AddPropertyBinding(nameof(IVolumeTextureMaterial.IterationOffset),
                                   () => WriteValue(VolumeParamsStruct.IterationOffset, material.IterationOffset));
                AddPropertyBinding(nameof(IVolumeTextureMaterial.IsoValue),
                                   () => WriteValue(VolumeParamsStruct.IsoValue, (float) material.IsoValue));
                AddPropertyBinding(nameof(IVolumeTextureMaterial.Color),
                                   () => WriteValue(VolumeParamsStruct.Color, material.Color));
                AddPropertyBinding(nameof(IVolumeTextureMaterial.TransferMap),
                                   () => UpdateGradientMap());
                AddPropertyBinding(nameof(IVolumeTextureMaterial.EnablePlaneAlignment),
                                   () => WriteValue(VolumeParamsStruct.EnablePlaneAlignment,
                                                    material.EnablePlaneAlignment));
            }

            private void UpdateStepSize() {
                if (texture?.Resource is Texture3D texture3D) {
                    var desc = texture3D.Description;
                    var maxSize = Math.Max(desc.Width, Math.Max(desc.Height, desc.Depth));
                    var steps = 1f / maxSize * (float) material.SampleDistance;
                    WriteValue(VolumeParamsStruct.StepSize, steps);
                } else {
                    WriteValue(VolumeParamsStruct.StepSize, 1);
                }

                WriteValue(VolumeParamsStruct.ActualSampleDistance, (float) material.SampleDistance);
                WriteValue(VolumeParamsStruct.BaseSampleDistance, 1.0f);
            }

            private void UpdateTexture(VolumeTextureMaterialCoreBase<T> material) {
                var newTexture = OnCreateTexture(material, EffectsManager);
                RemoveAndDispose(ref texture);
                texture = newTexture;
                if (texture != null) UpdateStepSize();
            }

            public void UpdateGradientMap() {
                RemoveAndDispose(ref transferMap);
                if (material.TransferMap != null)
                    transferMap = ShaderResourceViewProxy.CreateViewFromColorArray(EffectsManager.NativeDeviceResources,
                        material.TransferMap);
                WriteValue(VolumeParamsStruct.HasGradientMapX, material.TransferMap != null);
            }

            public override bool BindMaterialResources(
                RenderContext context,
                DeviceContextProxy deviceContext,
                ShaderPass shaderPass
            ) {
                if (texture != null) {
                    shaderPass.PixelShader.BindTexture(deviceContext, texSlot, texture);
                    shaderPass.PixelShader.BindTexture(deviceContext, gradientSlot, transferMap);
                    shaderPass.PixelShader.BindSampler(deviceContext, samplerSlot, sampler);
                    return true;
                }

                return false;
            }

            public override void Draw(
                DeviceContextProxy deviceContext,
                IAttachableBufferModel bufferModel,
                int instanceCount
            ) {
                DrawIndexed(deviceContext, bufferModel.IndexBuffer.ElementCount, instanceCount);
            }

            public override ShaderPass GetPass(RenderType renderType, RenderContext context) {
                return volumePass;
            }

            public override ShaderPass GetShadowPass(RenderType renderType, RenderContext context) {
                return ShaderPass.NullPass;
            }

            public override ShaderPass GetWireframePass(RenderType renderType, RenderContext context) {
                return ShaderPass.NullPass;
            }

            public override ShaderPass GetDepthPass(RenderType renderType, RenderContext context) {
                return ShaderPass.NullPass;
            }

            protected override void OnDispose(bool disposeManagedResources) {
                RemoveAndDispose(ref texture);
                RemoveAndDispose(ref transferMap);
                RemoveAndDispose(ref sampler);
                base.OnDispose(disposeManagedResources);
            }
        }
    }
}

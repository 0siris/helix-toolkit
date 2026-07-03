/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core.Components;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core {
    namespace Core {
        public class DrawScreenQuadCore : RenderCore {
            private readonly ConstantBufferComponent modelCB;

            public ScreenQuadModelStruct ModelStruct;

            private ShaderPass pass;
            private string passName = DefaultPassNames.Default;
            private SamplerStateProxy sampler;
            private SamplerStateDescription samplerDescription = DefaultSamplers.LinearSamplerClampAni1;
            private int samplerSlot;

            private TextureModel texture;
            private ShaderResourceViewProxy textureProxy;
            private int textureSlot;

            public DrawScreenQuadCore() : base(RenderType.Opaque) {
                modelCB = AddComponent(new ConstantBufferComponent(
                                           new ConstantBufferDescription(
                                               DefaultBufferNames.ScreenQuadCB,
                                               ScreenQuadModelStruct.SizeInBytes)));
                ModelStruct = new ScreenQuadModelStruct {
                    TopLeft = new Vector4(-1, 1, 1, 1),
                    TopRight = new Vector4(1, 1, 1, 1),
                    BottomLeft = new Vector4(-1, -1, 1, 1),
                    BottomRight = new Vector4(1, -1, 1, 1),
                    TexTopLeft = new Vector2(0, 1),
                    TexTopRight = new Vector2(1, 1),
                    TexBottomLeft = new Vector2(0, 0),
                    TexBottomRight = new Vector2(1, 0)
                };
            }

            public string PassName {
                get => passName;
                set {
                    if (SetAffectsRender(ref passName, value) && IsAttached) {
                        pass = EffectTechnique[value];
                        textureSlot =
                            pass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(DefaultBufferNames.DiffuseMapTB);
                        samplerSlot =
                            pass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(DefaultSamplerStateNames
                                .SurfaceSampler);
                    }
                }
            }

            /// <summary>
            ///     Gets or sets the texture.
            /// </summary>
            /// <value>
            ///     The texture.
            /// </value>
            public TextureModel Texture {
                get => texture;
                set {
                    if (SetAffectsRender(ref texture, value) && IsAttached) UpdateTexture(value);
                }
            }

            /// <summary>
            ///     Gets or sets the sampler description.
            /// </summary>
            /// <value>
            ///     The sampler description.
            /// </value>
            public SamplerStateDescription SamplerDescription {
                get => samplerDescription;
                set {
                    if (SetAffectsRender(ref samplerDescription, value) && IsAttached) { }
                }
            }

            private void UpdateTexture(TextureModel texture) {
                var newTexture = texture == null
                                     ? null
                                     : EffectTechnique.EffectsManager.MaterialTextureManager.Register(texture);
                RemoveAndDispose(ref textureProxy);
                textureProxy = newTexture;
            }

            private void UpdateSampler() {
                var newSampler = EffectTechnique.EffectsManager.StateManager.Register(samplerDescription);
                RemoveAndDispose(ref sampler);
                sampler = newSampler;
            }

            public override void Render(RenderContext context, DeviceContextProxy deviceContext) {
                if (pass.IsNULL) return;
                ModelStruct.mWorld = ModelMatrix;
                modelCB.Upload(deviceContext, ref ModelStruct);
                pass.BindShader(deviceContext);
                pass.BindStates(deviceContext,
                                StateType.BlendState | StateType.DepthStencilState | StateType.RasterState);
                pass.PixelShader.BindSampler(deviceContext, samplerSlot, sampler);
                pass.PixelShader.BindTexture(deviceContext, textureSlot, textureProxy);
                deviceContext.Draw(4, 0);
            }

            protected override bool OnAttach(IRenderTechnique technique) {
                pass = technique[passName];
                textureSlot =
                    pass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(DefaultBufferNames.DiffuseMapTB);
                samplerSlot = pass.PixelShader.SamplerMapping.TryGetBindSlot(DefaultSamplerStateNames.SurfaceSampler);
                UpdateTexture(texture);
                UpdateSampler();
                return true;
            }

            protected override void OnDetach() {
                RemoveAndDispose(ref textureProxy);
                RemoveAndDispose(ref sampler);
            }
        }
    }
}

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
        public interface IPostEffectBloom : IPostEffect {
            Color4 ThresholdColor { get; set; }

            float BloomExtractIntensity { get; set; }

            float BloomPassIntensity { get; set; }

            float BloomCombineSaturation { get; set; }

            float BloomCombineIntensity { get; set; }

            int NumberOfBlurPass { get; set; }
        }

        /// <summary>
        ///     Outline blur effect
        ///     <para>
        ///         Must not put in shared model across multiple viewport, otherwise may causes performance issue if each
        ///         viewport sizes are different.
        ///     </para>
        /// </summary>
        public class PostEffectBloomCore : RenderCore, IPostEffectBloom {
            /// <summary>
            ///     Initializes a new instance of the <see cref="PostEffectMeshOutlineBlurCore" /> class.
            /// </summary>
            public PostEffectBloomCore() : base(RenderType.GlobalEffect) {
                modelCB = AddComponent(new ConstantBufferComponent(
                                           new ConstantBufferDescription(
                                               DefaultBufferNames.BorderEffectCB,
                                               BorderEffectStruct.SizeInBytes)));
                ThresholdColor = new Color4(0.8f, 0.8f, 0.8f, 0f);
                BloomExtractIntensity = 1f;
                BloomPassIntensity = 0.95f;
                BloomCombineIntensity = 0.7f;
                BloomCombineSaturation = 0.7f;
            }

            protected override bool OnAttach(IRenderTechnique technique) {
                screenQuadPass = technique.GetPass(DefaultPassNames.ScreenQuad);
                screenQuadCopy = technique.GetPass(DefaultPassNames.ScreenQuadCopy);
                blurPassVertical = technique.GetPass(DefaultPassNames.EffectBlurVertical);
                blurPassHorizontal = technique.GetPass(DefaultPassNames.EffectBlurHorizontal);
                screenOutlinePass = technique.GetPass(DefaultPassNames.MeshOutline);
                textureSlot =
                    screenOutlinePass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(DefaultBufferNames
                        .DiffuseMapTB);
                samplerSlot =
                    screenOutlinePass.PixelShader.SamplerMapping.TryGetBindSlot(DefaultSamplerStateNames
                        .SurfaceSampler);
                sampler = technique.EffectsManager.StateManager.Register(DefaultSamplers.LinearSamplerClampAni1);
                blurCore = new PostEffectBlurCore(blurPassVertical,
                                                  blurPassHorizontal,
                                                  textureSlot,
                                                  samplerSlot,
                                                  DefaultSamplers.LinearSamplerClampAni1,
                                                  technique.EffectsManager);
                return true;
            }

            protected override bool OnUpdateCanRenderFlag() {
                return IsAttached && !string.IsNullOrEmpty(EffectName);
            }

            public override void Render(RenderContext context, DeviceContextProxy deviceContext) {
                var buffer = context.RenderHost.RenderBuffer;

            #region Do Bloom Pass

                modelCB.Upload(deviceContext, ref modelStruct);
                //Extract bloom samples
                deviceContext.SetRenderTarget(buffer.FullResPPBuffer.NextRTV);

                screenQuadPass.PixelShader.BindTexture(deviceContext, textureSlot, buffer.FullResPPBuffer.CurrentSRV);
                screenQuadPass.PixelShader.BindSampler(deviceContext, samplerSlot, sampler);
                screenQuadPass.BindShader(deviceContext);
                screenQuadPass.BindStates(deviceContext, StateType.All);
                deviceContext.Draw(4, 0);
                var viewport = context.Viewport;
                // Down sampling
                for (var i = 0; i < numberOfBlurPass; ++i)
                    blurCore.Run(context,
                                 deviceContext,
                                 buffer.FullResPPBuffer.NextRTV,
                                 ref viewport,
                                 PostEffectBlurCore.BlurDepth.Two,
                                 ref modelStruct);

            #endregion

            #region Draw outline onto original target

                BindTarget(null,
                           buffer.FullResPPBuffer.CurrentRTV,
                           deviceContext,
                           buffer.TargetWidth,
                           buffer.TargetHeight,
                           false);
                screenOutlinePass.PixelShader.BindTexture(deviceContext, textureSlot, buffer.FullResPPBuffer.NextSRV);
                screenOutlinePass.BindShader(deviceContext);
                screenOutlinePass.BindStates(deviceContext, StateType.All);
                deviceContext.Draw(4, 0);
                screenOutlinePass.PixelShader.BindTexture(deviceContext, textureSlot, null);

            #endregion
            }

            protected override void OnDetach() {
                RemoveAndDispose(ref sampler);
                RemoveAndDispose(ref blurCore);
            }

            private static void BindTarget(
                DepthStencilView dsv,
                RenderTargetView targetView,
                DeviceContextProxy context,
                int width,
                int height,
                bool clear = true
            ) {
                if (clear) context.ClearRenderTargetView(targetView, Color.Transparent);
                context.SetRenderTargets(dsv, new[] {targetView});
                context.SetViewport(0, 0, width, height);
                context.SetScissorRectangle(0, 0, width, height);
            }

        #region Variables

            private SamplerStateProxy sampler;
            private ShaderPass screenQuadPass;

            private ShaderPass screenQuadCopy;

            private ShaderPass blurPassVertical;

            private ShaderPass blurPassHorizontal;

            private ShaderPass screenOutlinePass;

            private int textureSlot;

            private int samplerSlot;

            private readonly ConstantBufferComponent modelCB;

            private BorderEffectStruct modelStruct;

            private PostEffectBlurCore blurCore;

        #endregion

        #region Properties

            private string effectName = DefaultRenderTechniqueNames.PostEffectBloom;

            /// <summary>
            ///     Gets or sets the name of the effect.
            /// </summary>
            /// <value>
            ///     The name of the effect.
            /// </value>
            public string EffectName {
                get => effectName;
                set => SetAffectsCanRenderFlag(ref effectName, value);
            }

            /// <summary>
            ///     Gets or sets the color of the border.
            /// </summary>
            /// <value>
            ///     The color of the border.
            /// </value>
            public Color4 ThresholdColor {
                get => modelStruct.Color;
                set => SetAffectsRender(ref modelStruct.Color, value);
            }

            public float BloomExtractIntensity {
                get => modelStruct.Param.M11;
                set {
                    var current = modelStruct.Param.M11;
                    if (SetAffectsRender(ref current, value)) modelStruct.Param.M11 = current;
                }
            }

            public float BloomPassIntensity {
                get => modelStruct.Param.M12;
                set {
                    var current = modelStruct.Param.M12;
                    if (SetAffectsRender(ref current, value)) modelStruct.Param.M12 = current;
                }
            }

            public float BloomCombineSaturation {
                get => modelStruct.Param.M13;
                set {
                    var current = modelStruct.Param.M13;
                    if (SetAffectsRender(ref current, value)) modelStruct.Param.M13 = current;
                }
            }

            public float BloomCombineIntensity {
                get => modelStruct.Param.M14;
                set {
                    var current = modelStruct.Param.M14;
                    if (SetAffectsRender(ref current, value)) modelStruct.Param.M14 = current;
                }
            }

            private int numberOfBlurPass = 1;

            /// <summary>
            ///     Gets or sets the number of blur pass.
            /// </summary>
            /// <value>
            ///     The number of blur pass.
            /// </value>
            public int NumberOfBlurPass {
                get => numberOfBlurPass;
                set => SetAffectsRender(ref numberOfBlurPass, value);
            }

        #endregion
        }
    }
}

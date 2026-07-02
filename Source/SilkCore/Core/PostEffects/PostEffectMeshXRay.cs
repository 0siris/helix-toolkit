/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core.Components;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core
{
    namespace Core
    {
        /// <summary>
        /// </summary>
        public interface IPostEffectMeshXRay : IPostEffect
        {
            /// <summary>
            ///     Gets or sets the color.
            /// </summary>
            /// <value>
            ///     The color.
            /// </value>
            Color4 Color { get; set; }

            /// <summary>
            ///     Gets or sets the outline fading factor.
            /// </summary>
            /// <value>
            ///     The outline fading factor.
            /// </value>
            float OutlineFadingFactor { get; set; }

            /// <summary>
            ///     Gets or sets a value indicating whether [double pass]. Double pass uses stencil buffer to reduce overlapping
            ///     artifacts
            /// </summary>
            /// <value>
            ///     <c>true</c> if [double pass]; otherwise, <c>false</c>.
            /// </value>
            bool EnableDoublePass { get; set; }
        }

        /// <summary>
        /// </summary>
        public class PostEffectMeshXRayCore : RenderCore, IPostEffectMeshXRay
        {
            /// <summary>
            ///     Initializes a new instance of the <see cref="PostEffectMeshXRayCore" /> class.
            /// </summary>
            public PostEffectMeshXRayCore() : base(RenderType.PostEffect)
            {
                modelCB = AddComponent(new ConstantBufferComponent(
                    new ConstantBufferDescription(DefaultBufferNames.BorderEffectCB, BorderEffectStruct.SizeInBytes)));
                Color = new Color4(0, 0, 1, 1);
            }


            protected override bool OnAttach(IRenderTechnique technique)
            {
                return true;
            }

            protected override void OnDetach()
            {
            }

            /// <summary>
            ///     Called when [render].
            /// </summary>
            /// <param name="context">The context.</param>
            /// <param name="deviceContext">The device context.</param>
            public override void Render(RenderContext context, DeviceContextProxy deviceContext)
            {
                var buffer = context.RenderHost.RenderBuffer;
                var dPass = EnableDoublePass;
                var depthStencilBuffer = buffer.DepthStencilBufferNoMSAA;
                deviceContext.SetRenderTarget(depthStencilBuffer, buffer.FullResPPBuffer.CurrentRTV);
                var viewport = context.Viewport;
                deviceContext.SetViewport(ref viewport);
                deviceContext.SetScissorRectangle(ref viewport);
                deviceContext.ClearDepthStencilView(depthStencilBuffer, DepthStencilClearFlags.Stencil);
                if (dPass)
                {
                    for (var i = 0; i < context.RenderHost.PerFrameNodesWithPostEffect.Count; ++i)
                    {
                        var mesh = context.RenderHost.PerFrameNodesWithPostEffect[i];
                        if (mesh.TryGetPostEffect(EffectName, out var effect))
                        {
                            currentCores.Add(new KeyValuePair<SceneNode, IEffectAttributes>(mesh, effect));
                            context.CustomPassName = DefaultPassNames.EffectMeshXRayP1;
                            var pass = mesh.EffectTechnique[DefaultPassNames.EffectMeshXRayP1];
                            if (pass.IsNULL) continue;
                            pass.BindShader(deviceContext);
                            pass.BindStates(deviceContext, StateType.BlendState | StateType.DepthStencilState);
                            mesh.RenderCustom(context, deviceContext);
                        }
                    }

                    modelCB.Upload(deviceContext, ref modelStruct);
                    for (var i = 0; i < currentCores.Count; ++i)
                    {
                        var mesh = currentCores[i];
                        var effect = mesh.Value;
                        var color = Color;
                        if (effect.TryGetAttribute(EffectAttributeNames.ColorAttributeName, out var attribute) &&
                            attribute is string colorStr) color = colorStr.ToColor4();
                        if (modelStruct.Color != color)
                        {
                            modelStruct.Color = color;
                            modelCB.Upload(deviceContext, ref modelStruct);
                        }

                        context.CustomPassName = DefaultPassNames.EffectMeshXRayP2;
                        var pass = mesh.Key.EffectTechnique[DefaultPassNames.EffectMeshXRayP2];
                        if (pass.IsNULL) continue;
                        pass.BindShader(deviceContext);
                        pass.BindStates(deviceContext, StateType.BlendState | StateType.DepthStencilState);
                        mesh.Key.RenderCustom(context, deviceContext);
                    }

                    currentCores.Clear();
                }
                else
                {
                    modelCB.Upload(deviceContext, ref modelStruct);
                    for (var i = 0; i < context.RenderHost.PerFrameNodesWithPostEffect.Count; ++i)
                    {
                        var mesh = context.RenderHost.PerFrameNodesWithPostEffect[i];
                        if (mesh.TryGetPostEffect(EffectName, out var effect))
                        {
                            var color = Color;
                            if (effect.TryGetAttribute(EffectAttributeNames.ColorAttributeName, out var attribute) &&
                                attribute is string colorStr) color = colorStr.ToColor4();
                            if (modelStruct.Color != color)
                            {
                                modelStruct.Color = color;
                                modelCB.Upload(deviceContext, ref modelStruct);
                            }

                            context.CustomPassName = DefaultPassNames.EffectMeshXRayP2;
                            var pass = mesh.EffectTechnique[DefaultPassNames.EffectMeshXRayP2];
                            if (pass.IsNULL) continue;
                            pass.BindShader(deviceContext);
                            pass.BindStates(deviceContext, StateType.BlendState);
                            deviceContext.SetDepthStencilState(pass.DepthStencilState);
                            mesh.RenderCustom(context, deviceContext);
                        }
                    }
                }
            }

            protected override bool OnUpdateCanRenderFlag()
            {
                return IsAttached && !string.IsNullOrEmpty(EffectName);
            }

            #region Variables

            private readonly List<KeyValuePair<SceneNode, IEffectAttributes>> currentCores = new();
            private readonly ConstantBufferComponent modelCB;
            private BorderEffectStruct modelStruct;

            #endregion

            #region Properties

            private string effectName = DefaultRenderTechniqueNames.PostEffectMeshXRay;

            /// <summary>
            ///     Gets or sets the name of the effect.
            /// </summary>
            /// <value>
            ///     The name of the effect.
            /// </value>
            public string EffectName
            {
                get => effectName;
                set => SetAffectsCanRenderFlag(ref effectName, value);
            }

            /// <summary>
            ///     Gets or sets the color of the border.
            /// </summary>
            /// <value>
            ///     The color of the border.
            /// </value>
            public Color4 Color
            {
                get => modelStruct.Color;
                set => SetAffectsRender(ref modelStruct.Color, value);
            }

            /// <summary>
            ///     Outline fading
            /// </summary>
            public float OutlineFadingFactor
            {
                get => modelStruct.Param.M11;
                set
                {
                    var current = modelStruct.Param.M11;
                    if (SetAffectsRender(ref current, value)) modelStruct.Param.M11 = current;
                }
            }

            private bool doublePass;

            /// <summary>
            ///     Gets or sets a value indicating whether [double pass]. Double pass uses stencil buffer to reduce overlapping
            ///     artifacts
            /// </summary>
            /// <value>
            ///     <c>true</c> if [double pass]; otherwise, <c>false</c>.
            /// </value>
            public bool EnableDoublePass
            {
                get => doublePass;
                set => SetAffectsRender(ref doublePass, value);
            }

            #endregion
        }
    }
}
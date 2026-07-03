/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core.Components;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core {
    namespace Core {
        public interface IPostEffectMeshXRayGrid : IPostEffect {
            Color4 Color { get; set; }

            int GridDensity { get; set; }

            float DimmingFactor { get; set; }

            float BlendingFactor { get; set; }

            string XRayDrawingPassName { get; set; }

            bool UseDepthOcclusion { get; set; }
        }

        /// <summary>
        /// </summary>
        public class PostEffectMeshXRayGridCore : RenderCore, IPostEffectMeshXRayGrid {
            /// <summary>
            ///     Initializes a new instance of the <see cref="PostEffectMeshXRayGridCore" /> class.
            /// </summary>
            public PostEffectMeshXRayGridCore() : base(RenderType.PostEffect) {
                modelCB = AddComponent(new ConstantBufferComponent(
                                           new ConstantBufferDescription(
                                               DefaultBufferNames.BorderEffectCB,
                                               BorderEffectStruct.SizeInBytes)));
                Color = new Color4(0, 0, 1, 1);
            }

            protected override bool OnAttach(IRenderTechnique technique) {
                return true;
            }

            protected override void OnDetach() { }

            protected override bool OnUpdateCanRenderFlag() {
                return IsAttached && !string.IsNullOrEmpty(EffectName);
            }

            /// <summary>
            ///     Called when [render].
            /// </summary>
            /// <param name="context">The context.</param>
            /// <param name="deviceContext">The device context.</param>
            public override void Render(RenderContext context, DeviceContextProxy deviceContext) {
                var buffer = context.RenderHost.RenderBuffer;
                var depthStencilBuffer = buffer.DepthStencilBufferNoMSAA;
                deviceContext.SetRenderTarget(depthStencilBuffer, buffer.FullResPPBuffer.CurrentRTV);
                var viewport = context.Viewport;
                deviceContext.SetViewport(ref viewport);
                deviceContext.SetScissorRectangle(ref viewport);
                //First pass, draw onto stencil buffer
                for (var i = 0; i < context.RenderHost.PerFrameNodesWithPostEffect.Count; ++i) {
                    var mesh = context.RenderHost.PerFrameNodesWithPostEffect[i];
                    if (mesh.TryGetPostEffect(EffectName, out var effect)) {
                        currentCores.Add(new KeyValuePair<SceneNode, IEffectAttributes>(mesh, effect));
                        context.CustomPassName = DefaultPassNames.EffectMeshXRayGridP1;
                        var pass = mesh.EffectTechnique[DefaultPassNames.EffectMeshXRayGridP1];
                        if (pass.IsNULL) continue;
                        pass.BindShader(deviceContext);
                        pass.BindStates(deviceContext, StateType.BlendState | StateType.DepthStencilState);
                        mesh.RenderCustom(context, deviceContext);
                    }
                }

                //Second pass, remove not covered part from stencil buffer
                if (UseDepthOcclusion)
                    for (var i = 0; i < currentCores.Count; ++i) {
                        var mesh = currentCores[i].Key;
                        context.CustomPassName = DefaultPassNames.EffectMeshXRayGridP2;
                        var pass = mesh.EffectTechnique[DefaultPassNames.EffectMeshXRayGridP2];
                        if (pass.IsNULL) continue;
                        pass.BindShader(deviceContext);
                        pass.BindStates(deviceContext, StateType.BlendState | StateType.DepthStencilState);
                        mesh.RenderCustom(context, deviceContext);
                    }

                OnUpdatePerModelStruct(context);
                modelCB.Upload(deviceContext, ref modelStruct);
                //Thrid pass, draw mesh with grid overlay
                for (var i = 0; i < currentCores.Count; ++i) {
                    var mesh = currentCores[i].Key;
                    var color = Color;
                    if (currentCores[i].Value
                                       .TryGetAttribute(EffectAttributeNames.ColorAttributeName, out var attribute) &&
                        attribute is string colorStr) color = colorStr.ToColor4();
                    if (modelStruct.Color != color) {
                        modelStruct.Color = color;
                        modelCB.Upload(deviceContext, ref modelStruct);
                    }

                    context.CustomPassName = XRayDrawingPassName;
                    var pass = mesh.EffectTechnique[XRayDrawingPassName];
                    if (pass.IsNULL) continue;
                    pass.BindShader(deviceContext);
                    pass.BindStates(deviceContext, StateType.BlendState | StateType.DepthStencilState);
                    if (mesh.RenderCore is IMaterialRenderParams material)
                        material.MaterialVariables.BindMaterialResources(context, deviceContext, pass);
                    mesh.RenderCustom(context, deviceContext);
                }

                currentCores.Clear();
            }

            private void OnUpdatePerModelStruct(RenderContext context) {
                modelStruct.Param.M11 = gridDensity;
                modelStruct.Param.M12 = dimmingFactor;
                modelStruct.Param.M13 = blendingFactor;
            }

        #region Variables

            private readonly List<KeyValuePair<SceneNode, IEffectAttributes>> currentCores = new();
            private readonly ConstantBufferComponent modelCB;
            private BorderEffectStruct modelStruct;

        #endregion

        #region Properties

            private string effectName = DefaultRenderTechniqueNames.PostEffectMeshXRayGrid;

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
            public Color4 Color {
                get => modelStruct.Color;
                set => SetAffectsRender(ref modelStruct.Color, value);
            }

            private int gridDensity = 8;

            /// <summary>
            ///     Gets or sets the grid density.
            /// </summary>
            /// <value>
            ///     The grid density.
            /// </value>
            public int GridDensity {
                get => gridDensity;
                set => SetAffectsRender(ref gridDensity, value);
            }

            private float dimmingFactor = 0.8f;

            /// <summary>
            ///     Gets or sets the dim factor on original color
            /// </summary>
            /// <value>
            ///     The dim factor.
            /// </value>
            public float DimmingFactor {
                get => dimmingFactor;
                set => SetAffectsRender(ref dimmingFactor, value);
            }

            private float blendingFactor = 1f;

            /// <summary>
            ///     Gets or sets the blending factor for grid and original mesh color blending
            /// </summary>
            /// <value>
            ///     The blending factor.
            /// </value>
            public float BlendingFactor {
                get => blendingFactor;
                set => SetAffectsRender(ref blendingFactor, value);
            }

            /// <summary>
            ///     Gets or sets the name of the x ray drawing pass. This is the final pass to draw mesh and grid overlay onto render
            ///     target
            /// </summary>
            /// <value>
            ///     The name of the x ray drawing pass.
            /// </value>
            public string XRayDrawingPassName { get; set; } = DefaultPassNames.EffectMeshXRayGridP3;

            private bool useDepthOcclusion = true;

            /// <summary>
            ///     Uses the scene depth buffer to hide x-ray parts that are not occluded.
            ///     Disable this for overlays that must stay visible after OIT rendering.
            /// </summary>
            public bool UseDepthOcclusion {
                get => useDepthOcclusion;
                set => SetAffectsRender(ref useDepthOcclusion, value);
            }

        #endregion
        }
    }
}

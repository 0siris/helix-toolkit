/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core {
    namespace Core {
        public class MeshRenderCore : GeometryRenderCore, IMeshRenderParams, IDynamicReflectable {
            protected ModelStruct modelStruct = new() { World = Matrix.Identity };

            protected override bool CreateRasterState(RasterizerStateDescription description, bool force) {
                if (base.CreateRasterState(description, force)) {
                    var wireframeDesc = description;
                    wireframeDesc.FillMode = FillMode.Wireframe;
                    wireframeDesc.DepthBias = -100;
                    wireframeDesc.SlopeScaledDepthBias = -2f;
                    wireframeDesc.DepthBiasClamp = -0.00008f;
                    var newState = EffectTechnique.EffectsManager.StateManager.Register(wireframeDesc);
                    RemoveAndDispose(ref rasterStateWireframe);
                    rasterStateWireframe = newState;
                    return true;
                }

                return false;
            }

            protected override void OnDetach() {
                RemoveAndDispose(ref rasterStateWireframe);
                base.OnDetach();
            }

            protected override bool OnUpdateCanRenderFlag() {
                return base.OnUpdateCanRenderFlag() && materialVariables != EmptyMaterialVariable.EmptyVariable;
            }

            protected virtual void OnUpdatePerModelStruct(RenderContext context) {
                modelStruct.World = ModelMatrix;
                modelStruct.HasInstances = InstanceBuffer.HasElements ? 1 : 0;
                modelStruct.Batched = Batched ? 1 : 0;
            }

            protected override void OnRender(RenderContext context, DeviceContextProxy deviceContext) {
                var pass = MaterialVariables.GetPass(RenderType, context);
                if (pass.IsNULL) return;
                OnUpdatePerModelStruct(context);
                if (!materialVariables.UpdateMaterialStruct(deviceContext, ref modelStruct)) return;
                pass.BindShader(deviceContext);
                pass.BindStates(deviceContext, DefaultStateBinding);
                if (!materialVariables.BindMaterialResources(context, deviceContext, pass)) return;

                DynamicReflector?.BindCubeMap(deviceContext);
                materialVariables.Draw(deviceContext, GeometryBuffer, InstanceBuffer.ElementCount);
                DynamicReflector?.UnBindCubeMap(deviceContext);

                if (RenderWireframe) {
                    pass = materialVariables.GetWireframePass(RenderType, context);
                    if (pass.IsNULL) return;
                    pass.BindShader(deviceContext, false);
                    pass.BindStates(deviceContext, DefaultStateBinding);
                    deviceContext.SetRasterState(RasterStateWireframe);
                    materialVariables.Draw(deviceContext, GeometryBuffer, InstanceBuffer.ElementCount);
                }
            }

            protected override void OnRenderCustom(RenderContext context, DeviceContextProxy deviceContext) {
                if (!materialVariables.UpdateMaterialStruct(deviceContext, ref modelStruct)) return;
                materialVariables.Draw(deviceContext, GeometryBuffer, InstanceBuffer.ElementCount);
            }

            protected override void OnRenderShadow(RenderContext context, DeviceContextProxy deviceContext) {
                var pass = materialVariables.GetShadowPass(RenderType, context);
                if (pass.IsNULL) return;
                var v = new SimpleMeshStruct {
                    World = ModelMatrix,
                    HasInstances = InstanceBuffer.HasElements ? 1 : 0
                };
                if (!materialVariables.UpdateNonMaterialStruct(deviceContext, ref v)) return;
                pass.BindShader(deviceContext);
                pass.BindStates(deviceContext, ShadowStateBinding);
                materialVariables.Draw(deviceContext, GeometryBuffer, InstanceBuffer.ElementCount);
            }

            protected override void OnRenderDepth(
                RenderContext context,
                DeviceContextProxy deviceContext,
                ShaderPass customPass
            ) {
                var pass = customPass ?? materialVariables.GetDepthPass(RenderType, context);
                if (pass.IsNULL) return;
                var v = new SimpleMeshStruct {
                    World = ModelMatrix,
                    HasInstances = InstanceBuffer.HasElements ? 1 : 0
                };
                if (!materialVariables.UpdateNonMaterialStruct(deviceContext, ref v)) return;
                pass.BindShader(deviceContext);
                pass.BindStates(deviceContext, ShadowStateBinding);
                materialVariables.Draw(deviceContext, GeometryBuffer, InstanceBuffer.ElementCount);
            }

            #region Variables

            /// <summary>
            ///     Gets the raster state wireframe.
            /// </summary>
            /// <value>
            ///     The raster state wireframe.
            /// </value>
            protected RasterizerStateProxy RasterStateWireframe => rasterStateWireframe;

            private RasterizerStateProxy rasterStateWireframe;

            #endregion

            #region Properties

            /// <summary>
            /// </summary>
            public bool InvertNormal {
                get => modelStruct.InvertNormal == 1;
                set => SetAffectsRender(ref modelStruct.InvertNormal, value ? 1 : 0);
            }

            private bool renderWireframe;

            /// <summary>
            ///     Gets or sets a value indicating whether [render wireframe].
            /// </summary>
            /// <value>
            ///     <c>true</c> if [render wireframe]; otherwise, <c>false</c>.
            /// </value>
            public bool RenderWireframe {
                get => renderWireframe;
                set => SetAffectsRender(ref renderWireframe, value);
            }

            /// <summary>
            ///     Gets or sets the color of the wireframe.
            /// </summary>
            /// <value>
            ///     The color of the wireframe.
            /// </value>
            public Color4 WireframeColor {
                get => modelStruct.WireframeColor;
                set => SetAffectsRender(ref modelStruct.WireframeColor, value);
            }


            /// <summary>
            ///     Gets or sets the dynamic reflector.
            /// </summary>
            /// <value>
            ///     The dynamic reflector.
            /// </value>
            public IDynamicReflector DynamicReflector { get; set; }

            /// <summary>
            ///     Gets or sets a value indicating whether this <see cref="MeshRenderCore" /> is batched.
            /// </summary>
            /// <value>
            ///     <c>true</c> if batched; otherwise, <c>false</c>.
            /// </value>
            public bool Batched { get; set; } = false;

            private MaterialVariable materialVariables = EmptyMaterialVariable.EmptyVariable;

            /// <summary>
            ///     Used to wrap all material resources
            /// </summary>
            public MaterialVariable MaterialVariables {
                get => materialVariables;
                set {
                    if (SetAffectsCanRenderFlag(ref materialVariables, value))
                        materialVariables ??= EmptyMaterialVariable.EmptyVariable;
                }
            }

            #endregion
        }
    }
}

/*
The MIT License(MIT)
Copyright(c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Core;

namespace HelixToolkit.SharpDX.Core {
    namespace Model.Scene {
        /// <summary>
        /// </summary>
        public class MeshNode : MaterialGeometryNode, IDynamicReflectable {
            /// <summary>
            ///     Called when [create render core].
            /// </summary>
            /// <returns></returns>
            protected override RenderCore OnCreateRenderCore() {
                return new MeshRenderCore();
            }

            /// <summary>
            ///     Called when [create buffer model].
            /// </summary>
            /// <param name="modelGuid"></param>
            /// <param name="geometry"></param>
            /// <returns></returns>
            protected override IAttachableBufferModel OnCreateBufferModel(Guid modelGuid, Geometry3D geometry) {
                return geometry != null && geometry.IsDynamic
                           ? EffectsManager.GeometryBufferManager.Register<DynamicMeshGeometryBufferModel>(
                               modelGuid,
                               geometry)
                           : EffectsManager.GeometryBufferManager
                                           .Register<DefaultMeshGeometryBufferModel>(modelGuid, geometry);
            }

            /// <summary>
            ///     Create raster state description.
            /// </summary>
            /// <returns></returns>
            protected override RasterizerStateDescription CreateRasterState() {
                return new RasterizerStateDescription {
                    FillMode = FillMode,
                    CullMode = CullMode,
                    DepthBias = DepthBias,
                    DepthBiasClamp = -1000,
                    SlopeScaledDepthBias = SlopeScaledDepthBias,
                    IsDepthClipEnabled = IsDepthClipEnabled,
                    IsFrontCounterClockwise = FrontCCW,
                    IsMultisampleEnabled = IsMSAAEnabled,
                    IsScissorEnabled = !IsThrowingShadow && IsScissorEnabled
                };
            }

            protected override bool OnCheckGeometry(Geometry3D geometry) {
                return base.OnCheckGeometry(geometry) && geometry is MeshGeometry3D;
            }

            protected override bool OnHitTest(
                HitTestContext context,
                Matrix totalModelMatrix,
                ref List<HitTestResult> hits
            ) {
                return (Geometry as MeshGeometry3D).HitTest(context, totalModelMatrix, ref hits, WrapperSource);
            }

            #region Properties

            private bool frontCCW = true;

            /// <summary>
            ///     Gets or sets a value indicating whether [front CCW].
            /// </summary>
            /// <value>
            ///     <c>true</c> if [front CCW]; otherwise, <c>false</c>.
            /// </value>
            public bool FrontCCW {
                get => frontCCW;
                set {
                    if (Set(ref frontCCW, value)) OnRasterStateChanged();
                }
            }

            private CullMode cullMode = CullMode.None;

            /// <summary>
            ///     Gets or sets the cull mode.
            /// </summary>
            /// <value>
            ///     The cull mode.
            /// </value>
            public CullMode CullMode {
                get => cullMode;
                set {
                    if (Set(ref cullMode, value)) OnRasterStateChanged();
                }
            }

            /// <summary>
            ///     Gets or sets a value indicating whether [invert normal].
            /// </summary>
            /// <value>
            ///     <c>true</c> if [invert normal]; otherwise, <c>false</c>.
            /// </value>
            public bool InvertNormal {
                get => (RenderCore as IInvertNormal).InvertNormal;
                set => (RenderCore as IInvertNormal).InvertNormal = value;
            }

            /// <summary>
            ///     Gets or sets the color of the wireframe.
            /// </summary>
            /// <value>
            ///     The color of the wireframe.
            /// </value>
            public Color4 WireframeColor {
                get => (RenderCore as IMeshRenderParams).WireframeColor;
                set => (RenderCore as IMeshRenderParams).WireframeColor = value;
            }

            /// <summary>
            ///     Gets or sets a value indicating whether [render wireframe].
            /// </summary>
            /// <value>
            ///     <c>true</c> if [render wireframe]; otherwise, <c>false</c>.
            /// </value>
            public bool RenderWireframe {
                get => (RenderCore as IMeshRenderParams).RenderWireframe;
                set => (RenderCore as IMeshRenderParams).RenderWireframe = value;
            }

            /// <summary>
            ///     Gets or sets the dynamic reflector.
            /// </summary>
            /// <value>
            ///     The dynamic reflector.
            /// </value>
            public IDynamicReflector DynamicReflector {
                get => (RenderCore as IDynamicReflectable).DynamicReflector;
                set => (RenderCore as IDynamicReflectable).DynamicReflector = value;
            }

            #endregion
        }
    }
}

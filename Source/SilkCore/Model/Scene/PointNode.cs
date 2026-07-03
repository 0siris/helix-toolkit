/*
The MIT License(MIT)
Copyright(c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Core;

namespace HelixToolkit.SharpDX.Core {
    namespace Model.Scene {
        /// <summary>
        /// </summary>
        public class PointNode : MaterialGeometryNode {
            /// <summary>
            ///     Distances the ray to point.
            /// </summary>
            /// <param name="r">The r.</param>
            /// <param name="p">The p.</param>
            /// <returns></returns>
            public static double DistanceRayToPoint(Ray r, Vector3 p) {
                var v = r.Direction;
                var w = p - r.Position;

                var c1 = SilkMath.Dot(w, v);
                var c2 = SilkMath.Dot(v, v);
                var b = c1 / c2;

                var pb = r.Position + v * b;
                return (p - pb).Length;
            }

            /// <summary>
            ///     Called when [create buffer model].
            /// </summary>
            /// <param name="modelGuid"></param>
            /// <param name="geometry"></param>
            /// <returns></returns>
            protected override IAttachableBufferModel OnCreateBufferModel(Guid modelGuid, Geometry3D geometry) {
                return geometry != null && geometry.IsDynamic
                           ? EffectsManager.GeometryBufferManager.Register<DynamicPointGeometryBufferModel>(modelGuid,
                               geometry)
                           : EffectsManager.GeometryBufferManager.Register<DefaultPointGeometryBufferModel>(modelGuid,
                               geometry);
            }

            /// <summary>
            ///     Called when [create render core].
            /// </summary>
            /// <returns></returns>
            protected override RenderCore OnCreateRenderCore() {
                return new PointLineRenderCore();
            }

            /// <summary>
            ///     Create raster state description.
            /// </summary>
            /// <returns></returns>
            protected override RasterizerStateDescription CreateRasterState() {
                return new RasterizerStateDescription {
                    FillMode = FillMode,
                    CullMode = CullMode.None,
                    DepthBias = DepthBias,
                    DepthBiasClamp = -1000,
                    SlopeScaledDepthBias = SlopeScaledDepthBias,
                    IsDepthClipEnabled = IsDepthClipEnabled,
                    IsFrontCounterClockwise = true,
                    IsMultisampleEnabled = false,
                    IsScissorEnabled = IsThrowingShadow ? false : IsScissorEnabled
                };
            }

            protected override IRenderTechnique OnCreateRenderTechnique(IEffectsManager effectsManager) {
                return effectsManager[DefaultRenderTechniqueNames.Points];
            }

            /// <summary>
            ///     <para>Determine if this can be rendered.</para>
            /// </summary>
            /// <param name="context"></param>
            /// <returns></returns>
            protected override bool CanRender(RenderContext context) {
                if (base.CanRender(context)) return !context.RenderHost.IsDeferredLighting;

                return false;
            }

            protected override bool OnCheckGeometry(Geometry3D geometry) {
                return base.OnCheckGeometry(geometry) && geometry is PointGeometry3D;
            }

            protected override bool PreHitTestOnBounds(HitTestContext context) {
                var center = BoundsSphereWithTransform.Center;
                var centerSp = context.RenderMatrices.Project(center);
                if (centerSp.X >= 0 && centerSp.Y >= 0
                                    && (centerSp - context.HitPointSP).Length <= hitTestThickness)
                    return true;
                return base.PreHitTestOnBounds(context);
            }

            protected override bool OnHitTest(
                HitTestContext context,
                Matrix totalModelMatrix,
                ref List<HitTestResult> hits
            ) {
                return (Geometry as PointGeometry3D).HitTest(context,
                                                             totalModelMatrix,
                                                             ref hits,
                                                             WrapperSource,
                                                             (float) HitTestThickness);
            }

        #region Properties

            private double hitTestThickness = 4;

            /// <summary>
            ///     Used only for point/line hit test
            /// </summary>
            public double HitTestThickness {
                get => hitTestThickness;
                set => Set(ref hitTestThickness, value);
            }

        #endregion
        }
    }
}

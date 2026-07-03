/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core;

namespace HelixToolkit.SharpDX.Core {
    namespace Model.Scene {
        /// <summary>
        /// </summary>
        public class NodePostEffectXRayGrid : SceneNode {
            /// <summary>
            ///     Called when [create render core].
            /// </summary>
            /// <returns></returns>
            protected override RenderCore OnCreateRenderCore() {
                return new PostEffectMeshXRayGridCore();
            }

            public sealed override bool HitTest(HitTestContext context, ref List<HitTestResult> hits) {
                return false;
            }

            protected sealed override bool OnHitTest(
                HitTestContext context,
                Matrix totalModelMatrix,
                ref List<HitTestResult> hits
            ) {
                return false;
            }

        #region Properties

            /// <summary>
            ///     Gets or sets the name of the effect.
            /// </summary>
            /// <value>
            ///     The name of the effect.
            /// </value>
            public string EffectName {
                get => (RenderCore as IPostEffect).EffectName;
                set => (RenderCore as IPostEffect).EffectName = value;
            }

            /// <summary>
            ///     Gets or sets the color.
            /// </summary>
            /// <value>
            ///     The color.
            /// </value>
            public Color4 Color {
                get => (RenderCore as IPostEffectMeshXRayGrid).Color;
                set => (RenderCore as IPostEffectMeshXRayGrid).Color = value;
            }

            /// <summary>
            ///     Gets or sets the grid density.
            /// </summary>
            /// <value>
            ///     The grid density.
            /// </value>
            public int GridDensity {
                get => (RenderCore as IPostEffectMeshXRayGrid).GridDensity;
                set => (RenderCore as IPostEffectMeshXRayGrid).GridDensity = value;
            }

            /// <summary>
            ///     Gets or sets the dimming factor.
            /// </summary>
            /// <value>
            ///     The dimming factor.
            /// </value>
            public float DimmingFactor {
                get => (RenderCore as IPostEffectMeshXRayGrid).DimmingFactor;
                set => (RenderCore as IPostEffectMeshXRayGrid).DimmingFactor = value;
            }

            /// <summary>
            ///     Gets or sets the blending factor for grid and original mesh color blending
            /// </summary>
            /// <value>
            ///     The blending factor.
            /// </value>
            public float BlendingFactor {
                get => (RenderCore as IPostEffectMeshXRayGrid).BlendingFactor;
                set => (RenderCore as IPostEffectMeshXRayGrid).BlendingFactor = value;
            }

            /// <summary>
            ///     Gets or sets the name of the x ray drawing pass. This is the final pass to draw mesh and grid overlay onto render
            ///     target
            /// </summary>
            /// <value>
            ///     The name of the x ray drawing pass.
            /// </value>
            public string XRayDrawingPassName {
                get => (RenderCore as IPostEffectMeshXRayGrid).XRayDrawingPassName;
                set => (RenderCore as IPostEffectMeshXRayGrid).XRayDrawingPassName = value;
            }

            /// <summary>
            ///     Gets or sets whether the x-ray grid uses the scene depth buffer to remove visible parts.
            /// </summary>
            public bool UseDepthOcclusion {
                get => (RenderCore as IPostEffectMeshXRayGrid).UseDepthOcclusion;
                set => (RenderCore as IPostEffectMeshXRayGrid).UseDepthOcclusion = value;
            }

        #endregion
        }
    }
}

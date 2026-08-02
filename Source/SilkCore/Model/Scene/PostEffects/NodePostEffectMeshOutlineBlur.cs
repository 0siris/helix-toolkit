/*
The MIT License(MIT)
Copyright(c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Core;

namespace HelixToolkit.SharpDX.Core {
    namespace Model.Scene {
        /// <summary>
        /// </summary>
        public class NodePostEffectMeshOutlineBlur : SceneNode {
            /// <summary>
            ///     Called when [create render core].
            /// </summary>
            /// <returns></returns>
            protected override RenderCore OnCreateRenderCore() {
                return new PostEffectMeshOutlineBlurCore();
            }

            protected override IRenderTechnique OnCreateRenderTechnique(IEffectsManager effectsManager) {
                return effectsManager[DefaultRenderTechniqueNames.PostEffectMeshOutlineBlur];
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
                get => (RenderCore as IPostEffectOutlineBlur).EffectName;
                set => (RenderCore as IPostEffectOutlineBlur).EffectName = value;
            }

            /// <summary>
            ///     Gets or sets the color.
            /// </summary>
            /// <value>
            ///     The color.
            /// </value>
            public Color4 Color {
                get => (RenderCore as IPostEffectOutlineBlur).Color;
                set => (RenderCore as IPostEffectOutlineBlur).Color = value;
            }

            /// <summary>
            ///     Gets or sets the scale x.
            /// </summary>
            /// <value>
            ///     The scale x.
            /// </value>
            public float ScaleX {
                get => (RenderCore as IPostEffectOutlineBlur).ScaleX;
                set => (RenderCore as IPostEffectOutlineBlur).ScaleX = value;
            }

            /// <summary>
            ///     Gets or sets the scale y.
            /// </summary>
            /// <value>
            ///     The scale y.
            /// </value>
            public float ScaleY {
                get => (RenderCore as IPostEffectOutlineBlur).ScaleY;
                set => (RenderCore as IPostEffectOutlineBlur).ScaleY = value;
            }

            /// <summary>
            ///     Gets or sets the number of blur pass.
            /// </summary>
            /// <value>
            ///     The number of blur pass.
            /// </value>
            public int NumberOfBlurPass {
                get => (RenderCore as IPostEffectOutlineBlur).NumberOfBlurPass;
                set => (RenderCore as IPostEffectOutlineBlur).NumberOfBlurPass = value;
            }

            #endregion
        }
    }
}

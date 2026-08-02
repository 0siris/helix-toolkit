/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core;

namespace HelixToolkit.SharpDX.Core {
    namespace Model.Scene {
        /// <summary>
        /// </summary>
        public class NodePostEffectBorderHighlight : NodePostEffectMeshOutlineBlur {
            /// <summary>
            ///     Initializes a new instance of the <see cref="NodePostEffectBorderHighlight" /> class.
            /// </summary>
            public NodePostEffectBorderHighlight() {
                EffectName = DefaultRenderTechniqueNames.PostEffectMeshBorderHighlight;
            }

            /// <summary>
            ///     Gets or sets the draw mode.
            /// </summary>
            /// <value>
            ///     The draw mode.
            /// </value>
            public OutlineMode DrawMode {
                get => (RenderCore as PostEffectMeshOutlineBlurCore).DrawMode;
                set => (RenderCore as PostEffectMeshOutlineBlurCore).DrawMode = value;
            }

            protected override IRenderTechnique OnCreateRenderTechnique(IEffectsManager effectsManager) {
                return effectsManager[DefaultRenderTechniqueNames.PostEffectMeshBorderHighlight];
            }

            /// <summary>
            ///     Called when [create render core].
            /// </summary>
            /// <returns></returns>
            protected override RenderCore OnCreateRenderCore() {
                return new PostEffectMeshOutlineBlurCore(false);
            }
        }
    }
}

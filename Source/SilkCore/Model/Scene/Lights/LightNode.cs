/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core;

namespace HelixToolkit.SharpDX.Core {
    namespace Model.Scene {
        /// <summary>
        /// </summary>
        public abstract class LightNode : SceneNode, ILight3D {
            /// <summary>
            ///     Gets or sets the color.
            /// </summary>
            /// <value>
            ///     The color.
            /// </value>
            public Color4 Color {
                get => (RenderCore as LightCoreBase).Color;
                set => (RenderCore as LightCoreBase).Color = value;
            }

            /// <summary>
            ///     Gets the type of the light.
            /// </summary>
            /// <value>
            ///     The type of the light.
            /// </value>
            public LightType LightType => (RenderCore as LightCoreBase).LightType;

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
        }
    }
}

/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Core;

namespace HelixToolkit.SharpDX.Core {
    namespace Model.Scene {
        /// <summary>
        /// </summary>
        public class SpotLightNode : PointLightNode {
            /// <summary>
            ///     Gets or sets the direction.
            /// </summary>
            /// <value>
            ///     The direction.
            /// </value>
            public Vector3 Direction {
                get => (RenderCore as SpotLightCore).Direction;
                set => (RenderCore as SpotLightCore).Direction = value;
            }

            /// <summary>
            ///     Gets or sets the fall off.
            /// </summary>
            /// <value>
            ///     The fall off.
            /// </value>
            public float FallOff {
                get => (RenderCore as SpotLightCore).FallOff;
                set => (RenderCore as SpotLightCore).FallOff = value;
            }

            /// <summary>
            ///     Gets or sets the inner angle.
            /// </summary>
            /// <value>
            ///     The inner angle.
            /// </value>
            public float InnerAngle {
                get => (RenderCore as SpotLightCore).InnerAngle;
                set => (RenderCore as SpotLightCore).InnerAngle = value;
            }

            /// <summary>
            ///     Gets or sets the outer angle.
            /// </summary>
            /// <value>
            ///     The outer angle.
            /// </value>
            public float OuterAngle {
                get => (RenderCore as SpotLightCore).OuterAngle;
                set => (RenderCore as SpotLightCore).OuterAngle = value;
            }

            /// <summary>
            ///     Called when [create render core].
            /// </summary>
            /// <returns></returns>
            protected override RenderCore OnCreateRenderCore() {
                return new SpotLightCore();
            }
        }
    }
}

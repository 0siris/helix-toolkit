/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Model;

namespace HelixToolkit.SharpDX.Core {
    namespace Core {
        /// <summary>
        /// </summary>
        public class SpotLightCore : PointLightCore {
            private Vector3 direction;

            private float fallOff = 1;

            private float innerAngle = 5;

            private float outerAngle = 45;

            /// <summary>
            ///     Initializes a new instance of the <see cref="SpotLightCore" /> class.
            /// </summary>
            public SpotLightCore() {
                LightType = LightType.Spot;
            }

            /// <summary>
            ///     Gets or sets the direction.
            /// </summary>
            /// <value>
            ///     The direction.
            /// </value>
            public Vector3 Direction {
                get => direction;
                set => SetAffectsRender(ref direction, value);
            }

            /// <summary>
            ///     Gets or sets the fall off.
            /// </summary>
            /// <value>
            ///     The fall off.
            /// </value>
            public float FallOff {
                get => fallOff;
                set => SetAffectsRender(ref fallOff, value);
            }

            /// <summary>
            ///     Gets or sets the inner angle.
            /// </summary>
            /// <value>
            ///     The inner angle.
            /// </value>
            public float InnerAngle {
                get => innerAngle;
                set => SetAffectsRender(ref innerAngle, value);
            }

            /// <summary>
            ///     Gets or sets the outer angle.
            /// </summary>
            /// <value>
            ///     The outer angle.
            /// </value>
            public float OuterAngle {
                get => outerAngle;
                set => SetAffectsRender(ref outerAngle, value);
            }

            /// <summary>
            ///     Called when [render].
            /// </summary>
            /// <param name="lightScene">The light scene.</param>
            /// <param name="index">The index.</param>
            protected override void OnRender(Light3DSceneShared lightScene, int index) {
                base.OnRender(lightScene, index);
                lightScene.LightModels.Lights[index].LightDir =
                    SilkMath.TransformNormal(direction, ModelMatrix).Normalized().ToVector4(0);
                lightScene.LightModels.Lights[index].LightSpot = new Vector4(
                    (float)Math.Cos(outerAngle / 360.0f * Math.PI),
                    (float)Math.Cos(innerAngle / 360.0f * Math.PI),
                    fallOff,
                    0);
            }
        }
    }
}

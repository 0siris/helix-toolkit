/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Model;

namespace HelixToolkit.SharpDX.Core
{
    namespace Core
    {
        /// <summary>
        /// </summary>
        public class PointLightCore : LightCoreBase
        {
            private Vector3 attenuation = new(1, 0, 0);
            private Vector3 position;

            private float range = 1000;

            /// <summary>
            ///     Initializes a new instance of the <see cref="PointLightCore" /> class.
            /// </summary>
            public PointLightCore()
            {
                LightType = LightType.Point;
            }

            /// <summary>
            ///     Gets or sets the position.
            /// </summary>
            /// <value>
            ///     The position.
            /// </value>
            public Vector3 Position
            {
                get => position;
                set => SetAffectsRender(ref position, value);
            }

            /// <summary>
            ///     Gets or sets the attenuation.
            /// </summary>
            /// <value>
            ///     The attenuation.
            /// </value>
            public Vector3 Attenuation
            {
                get => attenuation;
                set => SetAffectsRender(ref attenuation, value);
            }

            /// <summary>
            ///     Gets or sets the range.
            /// </summary>
            /// <value>
            ///     The range.
            /// </value>
            public float Range
            {
                get => range;
                set => SetAffectsRender(ref range, value);
            }

            /// <summary>
            ///     Called when [render].
            /// </summary>
            /// <param name="lightScene">The light scene.</param>
            /// <param name="index">The index.</param>
            protected override void OnRender(Light3DSceneShared lightScene, int index)
            {
                base.OnRender(lightScene, index);
                lightScene.LightModels.Lights[index].LightPos = (position + ModelMatrix.Row4.ToVector3()).ToVector4();
                lightScene.LightModels.Lights[index].LightAtt = attenuation.ToVector4(range);
            }
        }
    }
}
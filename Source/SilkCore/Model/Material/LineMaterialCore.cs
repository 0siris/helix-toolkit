/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.Serialization;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core {
    namespace Model {
        [DataContract]
        public class LineMaterialCore : MaterialCore, ILineRenderParams {
            public override MaterialVariable CreateMaterialVariables(
                IEffectsManager manager,
                IRenderTechnique technique
            ) {
                return new LineMaterialVariable(manager, technique, this);
            }

            #region Properties

            private float thickness = 0.5f;

            /// <summary>
            /// </summary>
            public float Thickness {
                get => thickness;
                set => Set(ref thickness, value);
            }

            private float smoothness;

            /// <summary>
            /// </summary>
            public float Smoothness {
                get => smoothness;
                set => Set(ref smoothness, value);
            }

            private Color4 lineColor = Color.Blue;

            /// <summary>
            ///     Final Line Color = LineColor * PerVertexLineColor
            /// </summary>
            public Color4 LineColor {
                get => lineColor;
                set => Set(ref lineColor, value);
            }

            private bool enableDistanceFading;

            public bool EnableDistanceFading {
                get => enableDistanceFading;
                set => Set(ref enableDistanceFading, value);
            }

            private float fadingNearDistance = 100;

            public float FadingNearDistance {
                get => fadingNearDistance;
                set => Set(ref fadingNearDistance, value);
            }

            private float fadingFarDistance;

            public float FadingFarDistance {
                get => fadingFarDistance;
                set => Set(ref fadingFarDistance, value);
            }

            private bool fixedSize = true;

            /// <summary>
            ///     Gets or sets a value indicating whether [fixed size].
            /// </summary>
            /// <value>
            ///     <c>true</c> if [fixed size]; otherwise, <c>false</c>.
            /// </value>
            public bool FixedSize {
                get => fixedSize;
                set => Set(ref fixedSize, value);
            }

            private TextureModel texture;

            /// <summary>
            ///     Gets or sets the texture.
            /// </summary>
            /// <value>
            ///     The texture.
            /// </value>
            public TextureModel Texture {
                get => texture;
                set => Set(ref texture, value);
            }

            private float textureScale = 1;

            /// <summary>
            ///     Gets or sets the texture scale.
            /// </summary>
            /// <value>
            ///     The texture scale.
            /// </value>
            public float TextureScale {
                get => textureScale;
                set => Set(ref textureScale, value);
            }

            private float alphaThreshold = 0.2f;

            /// <summary>
            ///     Gets or sets the alpha threshold. Pixel with color alpha value smaller than threshold will be set to transparent.
            ///     <para>This is used to avoid sampler color interpolation effects.</para>
            /// </summary>
            /// <value>
            ///     The alpha threshold
            /// </value>
            public float AlphaThreshold {
                get => alphaThreshold;
                set => Set(ref alphaThreshold, value);
            }


            private SamplerStateDescription samplerDescription = DefaultSamplers.LineSamplerUWrapVClamp;

            /// <summary>
            ///     Billboard texture sampler description
            /// </summary>
            public SamplerStateDescription SamplerDescription {
                get => samplerDescription;
                set => Set(ref samplerDescription, value);
            }

            #endregion
        }
    }
}

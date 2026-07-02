/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.Serialization;
using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core
{
    namespace Model
    {
        [DataContract]
        public class ColorStripeMaterialCore : MaterialCore
        {
            private SamplerStateDescription colorStripeSampler = DefaultSamplers.LinearSamplerClampAni1;

            private IList<Color4> colorStripeX;

            private bool colorStripeXEnabled = true;

            private IList<Color4> colorStripeY;

            private bool colorStripeYEnabled = true;
            private Color4 diffuseColor = Color.White;

            /// <summary>
            ///     Gets or sets the color of the diffuse.
            /// </summary>
            /// <value>
            ///     The color of the diffuse.
            /// </value>
            public Color4 DiffuseColor
            {
                get => diffuseColor;
                set => Set(ref diffuseColor, value);
            }

            /// <summary>
            ///     Gets or sets the color stripe x. Use texture coordinate X for sampling
            /// </summary>
            /// <value>
            ///     The color stripe x.
            /// </value>
            public IList<Color4> ColorStripeX
            {
                get => colorStripeX;
                set => Set(ref colorStripeX, value);
            }

            /// <summary>
            ///     Gets or sets the color stripe y. Use texture coordinate Y for sampling
            /// </summary>
            /// <value>
            ///     The color stripe y.
            /// </value>
            public IList<Color4> ColorStripeY
            {
                get => colorStripeY;
                set => Set(ref colorStripeY, value);
            }

            /// <summary>
            ///     Gets or sets a value indicating whether [color stripe x enabled].
            /// </summary>
            /// <value>
            ///     <c>true</c> if [color stripe x enabled]; otherwise, <c>false</c>.
            /// </value>
            public bool ColorStripeXEnabled
            {
                get => colorStripeXEnabled;
                set => Set(ref colorStripeXEnabled, value);
            }

            /// <summary>
            ///     Gets or sets a value indicating whether [color stripe y enabled].
            /// </summary>
            /// <value>
            ///     <c>true</c> if [color stripe y enabled]; otherwise, <c>false</c>.
            /// </value>
            public bool ColorStripeYEnabled
            {
                get => colorStripeYEnabled;
                set => Set(ref colorStripeYEnabled, value);
            }

            /// <summary>
            ///     Gets or sets the DiffuseMapSampler.
            /// </summary>
            /// <value>
            ///     DiffuseMapSampler
            /// </value>
            public SamplerStateDescription ColorStripeSampler
            {
                get => colorStripeSampler;
                set => Set(ref colorStripeSampler, value);
            }

            public override MaterialVariable CreateMaterialVariables(IEffectsManager manager,
                IRenderTechnique technique)
            {
                return new ColorStripeMaterialVariables(manager, technique, this);
            }
        }
    }
}
/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Core;

namespace HelixToolkit.SharpDX.Core {
    namespace Model {
        public class PointMaterialCore : MaterialCore, IPointRenderParams {
            private float blendingFactor;

            private bool enableColorBlending;
            private bool enableDistanceFading;

            private float fadingFarDistance = 100;

            private float fadingNearDistance;

            private PointFigure figure = PointFigure.Rect;

            private float figureRatio = 0.25f;

            private bool fixedSize = true;

            private float height = 0.5f;

            private Color4 pointColor = Color.Black;
            private float width = 0.5f;

            public bool EnableDistanceFading {
                get => enableDistanceFading;
                set => Set(ref enableDistanceFading, value);
            }

            public float FadingNearDistance {
                get => fadingNearDistance;
                set => Set(ref fadingNearDistance, value);
            }

            public float FadingFarDistance {
                get => fadingFarDistance;
                set => Set(ref fadingFarDistance, value);
            }

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

            /// <summary>
            ///     Gets or sets a value indicating whether [enable blending].
            ///     <para>
            ///         Once enabled, final color
            ///         = <see cref="BlendingFactor" /> * <see cref="PointColor" /> + (1 - <see cref="BlendingFactor" />) * Vertex
            ///         Color.
            ///     </para>
            /// </summary>
            /// <value>
            ///     <c>true</c> if [enable blending]; otherwise, <c>false</c>.
            /// </value>
            public bool EnableColorBlending {
                get => enableColorBlending;
                set => Set(ref enableColorBlending, value);
            }

            /// <summary>
            ///     Gets or sets the blending factor.
            ///     <para>Used when <see cref="EnableColorBlending" /> = true.</para>
            /// </summary>
            /// <value>
            ///     The blending factor.
            /// </value>
            public float BlendingFactor {
                get => blendingFactor;
                set => Set(ref blendingFactor, value);
            }

            /// <summary>
            ///     Gets or sets the width.
            /// </summary>
            /// <value>
            ///     The width.
            /// </value>
            public float Width {
                get => width;
                set => Set(ref width, value);
            }

            /// <summary>
            ///     Gets or sets the height.
            /// </summary>
            /// <value>
            ///     The height.
            /// </value>
            public float Height {
                get => height;
                set => Set(ref height, value);
            }

            /// <summary>
            ///     Gets or sets the figure.
            /// </summary>
            /// <value>
            ///     The figure.
            /// </value>
            public PointFigure Figure {
                get => figure;
                set => Set(ref figure, value);
            }

            /// <summary>
            ///     Gets or sets the figure ratio.
            /// </summary>
            /// <value>
            ///     The figure ratio.
            /// </value>
            public float FigureRatio {
                get => figureRatio;
                set => Set(ref figureRatio, value);
            }

            /// <summary>
            ///     Final Point Color = PointColor * PerVertexPointColor
            /// </summary>
            public Color4 PointColor {
                get => pointColor;
                set => Set(ref pointColor, value);
            }

            public override MaterialVariable CreateMaterialVariables(
                IEffectsManager manager,
                IRenderTechnique technique
            ) {
                return new PointMaterialVariable(manager, technique, this);
            }
        }
    }
}

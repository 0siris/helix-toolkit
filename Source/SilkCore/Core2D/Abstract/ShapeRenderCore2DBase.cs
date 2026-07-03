/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


namespace HelixToolkit.SharpDX.Core {
    namespace Core2D {
        /// <summary>
        /// </summary>
        public abstract class ShapeRenderCore2DBase : RenderCore2DBase {
            private Brush fillBrush;

            private Brush strokeBrush;

            private StrokeStyle strokeStyle;

            /// <summary>
            ///     Gets or sets the fill brush.
            /// </summary>
            /// <value>
            ///     The fill brush.
            /// </value>
            public Brush FillBrush {
                get => fillBrush;
                set {
                    var old = fillBrush;
                    if (SetAffectsRender(ref fillBrush, value)) RemoveAndDispose(ref old);
                }
            }

            /// <summary>
            ///     Gets or sets the stroke brush.
            /// </summary>
            /// <value>
            ///     The stroke brush.
            /// </value>
            public Brush StrokeBrush {
                get => strokeBrush;
                set {
                    var old = strokeBrush;
                    if (SetAffectsRender(ref strokeBrush, value)) RemoveAndDispose(ref old);
                }
            }

            /// <summary>
            ///     Gets or sets the width of the stroke.
            /// </summary>
            /// <value>
            ///     The width of the stroke.
            /// </value>
            public float StrokeWidth { get; set; } = 1.0f;

            /// <summary>
            ///     Gets or sets the stroke style.
            /// </summary>
            /// <value>
            ///     The stroke style.
            /// </value>
            public StrokeStyle StrokeStyle {
                get => strokeStyle;
                set {
                    var old = strokeStyle;
                    if (SetAffectsRender(ref strokeStyle, value)) RemoveAndDispose(ref old);
                }
            }

            protected override void OnDetach() {
                RemoveAndDispose(ref fillBrush);
                RemoveAndDispose(ref strokeBrush);
                RemoveAndDispose(ref strokeStyle);
                base.OnDetach();
            }
        }
    }
}

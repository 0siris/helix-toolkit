/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/
#if !NETFX_CORE
namespace HelixToolkit.Wpf.SharpDX
#else
#if CORE
namespace HelixToolkit.SharpDX.Core
#else
namespace HelixToolkit.UWP
#endif
#endif
{
    namespace Core2D
    {
        /// <summary>
        /// 
        /// </summary>
        public abstract class ShapeRenderCore2DBase : RenderCore2DBase
        {
            private Brush fillBrush = null;
            /// <summary>
            /// Gets or sets the fill brush.
            /// </summary>
            /// <value>
            /// The fill brush.
            /// </value>
            public Brush FillBrush
            {
                set
                {
                    var old = fillBrush;
                    if (SetAffectsRender(ref fillBrush, value))
                    {
                        RemoveAndDispose(ref old);
                    }
                }
                get
                {
                    return fillBrush;
                }
            }

            private Brush strokeBrush = null;
            /// <summary>
            /// Gets or sets the stroke brush.
            /// </summary>
            /// <value>
            /// The stroke brush.
            /// </value>
            public Brush StrokeBrush
            {
                set
                {
                    var old = strokeBrush;
                    if (SetAffectsRender(ref strokeBrush, value))
                    {
                        RemoveAndDispose(ref old);
                    }
                }
                get
                {
                    return strokeBrush;
                }
            }
            /// <summary>
            /// Gets or sets the width of the stroke.
            /// </summary>
            /// <value>
            /// The width of the stroke.
            /// </value>
            public float StrokeWidth
            {
                set; get;
            } = 1.0f;

            private StrokeStyle strokeStyle = null;
            /// <summary>
            /// Gets or sets the stroke style.
            /// </summary>
            /// <value>
            /// The stroke style.
            /// </value>
            public StrokeStyle StrokeStyle
            {
                set
                {
                    var old = strokeStyle;
                    if (SetAffectsRender(ref strokeStyle, value))
                    {
                        RemoveAndDispose(ref old);
                    }
                }
                get
                {
                    return strokeStyle;
                }
            }

            protected override void OnDetach()
            {
                RemoveAndDispose(ref fillBrush);
                RemoveAndDispose(ref strokeBrush);
                RemoveAndDispose(ref strokeStyle);
                base.OnDetach();
            }
        }
    }
}

/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/
//#define DEBUGBOUNDS
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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
        public class ImageRenderCore2D : RenderCore2DBase
        {
            private Bitmap bitmap;
            /// <summary>
            /// Gets or sets the bitmap.
            /// </summary>
            /// <value>
            /// The bitmap.
            /// </value>
            public Bitmap Bitmap
            {
                set
                {
                    var old = bitmap;
                    if (SetAffectsRender(ref bitmap, value))
                    {
                        RemoveAndDispose(ref old);
                        if (value != null)
                        {
                            ImageSize = bitmap.Size;
                        }
                        else
                        {
                            ImageSize = new Size2F();
                        }
                    }
                }
                get
                {
                    return bitmap;
                }
            }
            /// <summary>
            /// Gets or sets the size of the image.
            /// </summary>
            /// <value>
            /// The size of the image.
            /// </value>
            public Size2F ImageSize
            {
                private set; get;
            }

            private float opacity = 1;
            /// <summary>
            /// Gets or sets the opacity.
            /// </summary>
            /// <value>
            /// The opacity.
            /// </value>
            public float Opacity
            {
                set
                {
                    SetAffectsRender(ref opacity, value);
                }
                get
                {
                    return opacity;
                }
            }

            private BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.Linear;
            /// <summary>
            /// Gets or sets the interpolation mode.
            /// </summary>
            /// <value>
            /// The interpolation mode.
            /// </value>
            public BitmapInterpolationMode InterpolationMode
            {
                set
                {
                    Set(ref interpolationMode, value);
                }
                get
                {
                    return interpolationMode;
                }
            }

            protected override bool CanRender(RenderContext2D context)
            {
                return base.CanRender(context) && Bitmap != null;
            }

            protected override void OnRender(RenderContext2D context)
            {
                context.DeviceContext.DrawBitmap(Bitmap, LayoutBound, Opacity, InterpolationMode);
            }

            protected override void OnDetach()
            {
                RemoveAndDispose(ref bitmap);
                base.OnDetach();
            }
        }
    }
}

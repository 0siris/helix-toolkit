/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/
//#define DEBUGBOUNDS

namespace HelixToolkit.SharpDX.Core {
    namespace Core2D {
        public class ImageRenderCore2D : RenderCore2DBase {
            private Bitmap bitmap;

            private BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.Linear;

            private float opacity = 1;

            /// <summary>
            ///     Gets or sets the bitmap.
            /// </summary>
            /// <value>
            ///     The bitmap.
            /// </value>
            public Bitmap Bitmap {
                get => bitmap;
                set {
                    var old = bitmap;
                    if (SetAffectsRender(ref bitmap, value)) {
                        RemoveAndDispose(ref old);
                        if (value != null)
                            ImageSize = bitmap.Size;
                        else
                            ImageSize = new Size2F();
                    }
                }
            }

            /// <summary>
            ///     Gets or sets the size of the image.
            /// </summary>
            /// <value>
            ///     The size of the image.
            /// </value>
            public Size2F ImageSize { get; private set; }

            /// <summary>
            ///     Gets or sets the opacity.
            /// </summary>
            /// <value>
            ///     The opacity.
            /// </value>
            public float Opacity {
                get => opacity;
                set => SetAffectsRender(ref opacity, value);
            }

            /// <summary>
            ///     Gets or sets the interpolation mode.
            /// </summary>
            /// <value>
            ///     The interpolation mode.
            /// </value>
            public BitmapInterpolationMode InterpolationMode {
                get => interpolationMode;
                set => Set(ref interpolationMode, value);
            }

            protected override bool CanRender(RenderContext2D context) {
                return base.CanRender(context) && Bitmap != null;
            }

            protected override void OnRender(RenderContext2D context) {
                context.DeviceContext.DrawBitmap(Bitmap, LayoutBound, Opacity, InterpolationMode);
            }

            protected override void OnDetach() {
                RemoveAndDispose(ref bitmap);
                base.OnDetach();
            }
        }
    }
}

/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/
//#define DEBUGBOUNDS

using HelixToolkit.SharpDX.Core.Core2D.Abstract;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render;

namespace HelixToolkit.SharpDX.Core.Core2D;
public class ImageRenderCore2D : RenderCore2DBase {
    private Bitmap? bitmap;

    /// <summary>
    ///     Gets or sets the bitmap.
    /// </summary>
    /// <value>
    ///     The bitmap.
    /// </value>
    public Bitmap? Bitmap {
        get => bitmap;
        set {
            var old = bitmap;
            if (SetAffectsRender(ref bitmap, value)) {
                RemoveAndDispose(ref old);
                ImageSize = value is { } newBitmap ? newBitmap.Size : new Size2F();
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
        get;
        set => SetAffectsRender(ref field, value);
    } = 1;

    /// <summary>
    ///     Gets or sets the interpolation mode.
    /// </summary>
    /// <value>
    ///     The interpolation mode.
    /// </value>
    public BitmapInterpolationMode InterpolationMode {
        get;
        set => Set(ref field, value);
    } = BitmapInterpolationMode.Linear;

    protected override bool CanRender(RenderContext2D context) => base.CanRender(context) && Bitmap != null;

    protected override void OnRender(RenderContext2D context) {
        if (Bitmap is { } bitmap)
            context.DeviceContext.DrawBitmap(bitmap, LayoutBound, Opacity, InterpolationMode);
    }

    protected override void OnDetach() {
        RemoveAndDispose(ref bitmap);
        base.OnDetach();
    }
}

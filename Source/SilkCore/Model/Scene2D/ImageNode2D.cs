/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core2D;
using HelixToolkit.SharpDX.Core.Core2D.Abstract;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Scene2D.Abstract;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Utilities;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HelixToolkit.SharpDX.Core.Model.Scene2D;
/// <summary>
///     Displays an encoded image and measures its decoded frame dimensions.
/// </summary>
public class ImageNode2D : SceneNode2D {
    private ImageRenderCore2D ImageCore => RenderCore as ImageRenderCore2D
        ?? throw new InvalidOperationException("Image render core is not initialized.");

    public Stream? ImageStream {
        get;
        set {
            if (SetAffectsMeasure(ref field, value)) BitmapChanged = true;
        }
    }

    public float Opacity {
        get => ImageCore.Opacity;
        set => ImageCore.Opacity = value;
    }

    protected bool BitmapChanged { get; private set; } = true;

    protected override RenderCore2D CreateRenderCore() => new ImageRenderCore2D();

    protected override bool OnAttach() {
        if (base.OnAttach()) {
            BitmapChanged = true;
            return true;
        }

        return false;
    }

    private void LoadBitmap(RenderContext2D context, Stream? stream) {
        ImageCore.Bitmap = stream is null ? null : OnLoadImage(context, stream);
    }

    /// <summary>
    ///     Decodes the original first image frame as premultiplied BGRA pixels.
    /// </summary>
    /// <param name="context">The current Scene2D context.</param>
    /// <param name="stream">The encoded image stream.</param>
    /// <returns>The decoded bitmap with the actual frame dimensions.</returns>
    protected virtual Bitmap OnLoadImage(RenderContext2D context, Stream stream) {
        var position = stream.CanSeek ? stream.Position : 0;
        try {
            if (stream.CanSeek) stream.Position = 0;
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad);
            var frame = decoder.Frames[0];
            var converter = new FormatConvertedBitmap(frame, PixelFormats.Pbgra32, null, 0);
            var pixels = new byte[checked(frame.PixelWidth * frame.PixelHeight * 4)];
            converter.CopyPixels(pixels, checked(frame.PixelWidth * 4), 0);
            return new Bitmap(new Size2F(frame.PixelWidth, frame.PixelHeight), pixels: pixels);
        } finally {
            if (stream.CanSeek) stream.Position = position;
        }
    }

    public override void Update(RenderContext2D context) {
        base.Update(context);
        if (BitmapChanged) {
            LoadBitmap(context, ImageStream);
            BitmapChanged = false;
            InvalidateMeasure();
        }
    }

    protected override Size2F MeasureOverride(Size2F availableSize) {
        if (ImageStream is not null) {
            var imageSize = ImageCore.ImageSize;
            imageSize.Width *= DpiScale;
            imageSize.Height *= DpiScale;
            if (Width == 0 && Height == 0)
                return new Size2F(Math.Min(availableSize.Width, imageSize.Width),
                                  Math.Min(availableSize.Height, imageSize.Height));

            if (imageSize.Width == 0 || imageSize.Height == 0) return availableSize;

            var aspectRatio = imageSize.Width / imageSize.Height;
            if (Width == 0) {
                var height = Math.Min(availableSize.Height, Height) * DpiScale;
                return new Size2F(height * aspectRatio, height);
            }

            var width = Math.Min(availableSize.Width, Width) * DpiScale;
            return new Size2F(width, width / aspectRatio);
        }

        return new Size2F(Math.Max(0, Width * DpiScale), Math.Max(0, Height * DpiScale));
    }

protected override bool OnHitTest(ref Vector2 mousePoint, out HitTest2DResult? hitResult) {
        hitResult = null;
        if (LayoutBoundWithTransform.Contains(mousePoint)) {
            hitResult = new HitTest2DResult(this);
            return true;
        }

        return false;
    }
}

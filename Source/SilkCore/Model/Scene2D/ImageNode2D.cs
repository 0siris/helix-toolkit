/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core2D;

namespace HelixToolkit.SharpDX.Core.Model.Scene2D;
public class ImageNode2D : SceneNode2D {
    private Stream imageStream;

    public Stream ImageStream {
        get => imageStream;
        set {
            if (SetAffectsMeasure(ref imageStream, value)) bitmapChanged = true;
        }
    }

    public float Opacity {
        get => (RenderCore as ImageRenderCore2D).Opacity;
        set => (RenderCore as ImageRenderCore2D).Opacity = value;
    }

    protected bool bitmapChanged { get; private set; } = true;

    protected override RenderCore2D CreateRenderCore() {
        return new ImageRenderCore2D();
    }

    protected override bool OnAttach(IRenderHost host) {
        if (base.OnAttach(host)) {
            bitmapChanged = true;
            return true;
        }

        return false;
    }

    private void LoadBitmap(RenderContext2D context, Stream stream) {
        (RenderCore as ImageRenderCore2D).Bitmap = stream == null ? null : OnLoadImage(context, stream);
    }

    protected virtual Bitmap OnLoadImage(RenderContext2D context, Stream stream) {
        return new Bitmap(default);
    }

    public override void Update(RenderContext2D context) {
        base.Update(context);
        if (bitmapChanged) {
            LoadBitmap(context, ImageStream);
            bitmapChanged = false;
        }
    }

    protected override Size2F MeasureOverride(Size2F availableSize) {
        if (ImageStream != null) {
            var imageSize = (RenderCore as ImageRenderCore2D).ImageSize;
            imageSize.Width *= DpiScale;
            imageSize.Height *= DpiScale;
            if (Width == 0 && Height == 0)
                return new Size2F(Math.Min(availableSize.Width, imageSize.Width),
                                  Math.Min(availableSize.Height, imageSize.Height));

            if (imageSize.Width == 0 || imageSize.Height == 0) return availableSize;

            var aspectRatio = imageSize.Width / imageSize.Height;
            if (Width == 0) {
                var height = Math.Min(availableSize.Height, Height) * DpiScale;
                return new Size2F(height / aspectRatio, height);
            }

            var width = Math.Min(availableSize.Width, Width) * DpiScale;
            return new Size2F(width, width * aspectRatio);
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

/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core2D;

namespace HelixToolkit.SharpDX.Core.Model.Scene2D;
public abstract class ContentNode2D : PresenterNode2D {
    public HorizontalAlignment HorizontalContentAlignment {
        get;
        set {
            if (Set(ref field, value)) InvalidateMeasure();
        }
    } = HorizontalAlignment.Center;

    public VerticalAlignment VerticalContentAlignment {
        get;
        set {
            if (Set(ref field, value)) InvalidateMeasure();
        }
    } = VerticalAlignment.Center;

    public Brush Background {
        get => (RenderCore as BorderRenderCore2D).Background;
        set => (RenderCore as BorderRenderCore2D).Background = value;
    }

    protected override RenderCore2D CreateRenderCore() {
        return new BorderRenderCore2D();
    }

protected override bool OnHitTest(ref Vector2 mousePoint, out HitTest2DResult? hitResult) {
        if (Content != null && LayoutBoundWithTransform.Contains(mousePoint))
            return Content.HitTest(mousePoint, out hitResult);

        hitResult = null;
        return false;
    }

    protected override Size2F MeasureOverride(Size2F availableSize) {
        var maxContentSize = new Size2F();
        foreach (var item in Items)
            if (item is SceneNode2D e) {
                e.HorizontalAlignment = HorizontalContentAlignment;
                e.VerticalAlignment = VerticalContentAlignment;
                e.Measure(availableSize);
                maxContentSize.Width = Math.Max(maxContentSize.Width, e.DesiredSize.X);
                maxContentSize.Height = Math.Max(maxContentSize.Height, e.DesiredSize.Y);
            }

        if (HorizontalAlignment == HorizontalAlignment.Center) {
            availableSize.Width = Math.Min(availableSize.Width, maxContentSize.Width);
        } else {
            if (float.IsInfinity(availableSize.Width)) {
                if (float.IsInfinity(Width))
                    availableSize.Width = maxContentSize.Width;
                else
                    availableSize.Width = Width;
            }
        }

        if (VerticalAlignment == VerticalAlignment.Center) {
            availableSize.Height = Math.Min(availableSize.Height, maxContentSize.Height);
        } else {
            if (float.IsInfinity(availableSize.Height)) {
                if (float.IsInfinity(Height))
                    availableSize.Height = maxContentSize.Height;
                else
                    availableSize.Height = Height;
            }
        }

        return availableSize;
    }
}

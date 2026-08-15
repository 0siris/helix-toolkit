/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core2D;
using HelixToolkit.SharpDX.Core.Core2D.Abstract;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Model.Scene2D.Abstract;
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
        get => (RenderCore as BorderRenderCore2D)?.Background
            ?? throw new InvalidOperationException("The border render core has not been created.");
        set {
            if (RenderCore is BorderRenderCore2D core)
                core.Background = value;
        }
    }

    protected override RenderCore2D CreateRenderCore() => new BorderRenderCore2D();

    protected override bool OnHitTest(ref Vector2 mousePoint, out HitTest2DResult? hitResult) {
        if (Content is { } content && LayoutBoundWithTransform.Contains(mousePoint))
            return content.HitTest(mousePoint, out hitResult);

        hitResult = null;
        return false;
    }

    protected override Size2F MeasureOverride(Size2F availableSize) {
        var maxContentSize = new Size2F();
        foreach (var e in Items) {
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

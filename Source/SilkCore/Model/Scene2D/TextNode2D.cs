/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Core2D;
using HelixToolkit.SharpDX.Core.Core2D.Abstract;
using HelixToolkit.SharpDX.Core.Model.Scene2D.Abstract;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Model.Scene2D;
public class TextNode2D : SceneNode2D {
    private TextRenderCore2D? textRenderable;

    private TextRenderCore2D TextCore => textRenderable
        ?? throw new InvalidOperationException("Text render core is not initialized.");

    public string Text {
        get;
        set {
            if (SetAffectsMeasure(ref field, value)) TextCore.Text = value;
        }
    } = string.Empty;

    public Brush? Foreground {
        get => TextCore.Foreground;
        set => TextCore.Foreground = value;
    }

    public Brush? Background {
        get => TextCore.Background;
        set => TextCore.Background = value;
    }

    public int FontSize {
        get => TextCore.FontSize;
        set => TextCore.FontSize = value;
    }

    public FontWeight FontWeight {
        get => TextCore.FontWeight;
        set => TextCore.FontWeight = value;
    }

    public FontStyle FontStyle {
        get => TextCore.FontStyle;
        set => TextCore.FontStyle = value;
    }

    public TextAlignment TextAlignment {
        get => TextCore.TextAlignment;
        set => TextCore.TextAlignment = value;
    }

    public FlowDirection FlowDirection {
        get => TextCore.FlowDirection;
        set => TextCore.FlowDirection = value;
    }

    public string FontFamily {
        get => TextCore.FontFamily;
        set => TextCore.FontFamily = value;
    }

    protected override RenderCore2D CreateRenderCore() {
        textRenderable = new TextRenderCore2D();
        return textRenderable;
    }

protected override bool OnHitTest(ref Vector2 mousePoint, out HitTest2DResult? hitResult) {
        hitResult = null;
        if (LayoutBoundWithTransform.Contains(mousePoint)) {
            hitResult = new HitTest2DResult(WrapperSource);
            return true;
        }

        return false;
    }

    protected override Size2F MeasureOverride(Size2F availableSize) {
        TextCore.MaxWidth = availableSize.Width;
        TextCore.MaxHeight = availableSize.Height;
        var metrices = TextCore.Metrices;
        return new Size2F(metrices.WidthIncludingTrailingWhitespace, metrices.Height);
    }

    protected override RectangleF ArrangeOverride(RectangleF finalSize) {
        TextCore.MaxWidth = finalSize.Width;
        TextCore.MaxHeight = finalSize.Height;
        return finalSize;
    }
}

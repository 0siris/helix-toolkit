/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core2D;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Scene2D.Abstract;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render;

namespace HelixToolkit.SharpDX.Core.Model.Scene2D;
public class BorderNode2D : ContentNode2D {
    private BorderRenderCore2D BorderCore => RenderCore as BorderRenderCore2D
        ?? throw new InvalidOperationException("Border render core is not initialized.");

    private bool strokeStyleChanged = true;

    public float CornerRadius {
        get => BorderCore.CornerRadius;
        set => BorderCore.CornerRadius = value;
    }

    public Thickness Padding {
        get;
        set => SetAffectsMeasure(ref field, value);
    } = new(0);

    public Brush? BorderBrush {
        get => BorderCore.StrokeBrush;
        set => BorderCore.StrokeBrush = value;
    }

    public CapStyle StrokeDashCap {
        get;
        set {
            if (SetAffectsRender(ref field, value)) strokeStyleChanged = true;
        }
    } = CapStyle.Flat;

    public CapStyle StrokeStartLineCap {
        get;
        set {
            if (SetAffectsRender(ref field, value)) strokeStyleChanged = true;
        }
    } = CapStyle.Flat;

    public CapStyle StrokeEndLineCap {
        get;
        set {
            if (SetAffectsRender(ref field, value)) strokeStyleChanged = true;
        }
    } = CapStyle.Flat;

    public DashStyle StrokeDashStyle {
        get;
        set {
            if (SetAffectsRender(ref field, value)) strokeStyleChanged = true;
        }
    } = DashStyle.Solid;

    public float StrokeDashOffset {
        get;
        set {
            if (SetAffectsRender(ref field, value)) strokeStyleChanged = true;
        }
    }

    public LineJoin StrokeLineJoin {
        get;
        set {
            if (SetAffectsRender(ref field, value)) strokeStyleChanged = true;
        }
    } = LineJoin.Miter;

    public float StrokeMiterLimit {
        get;
        set {
            if (SetAffectsRender(ref field, value)) strokeStyleChanged = true;
        }
    } = 1;

    public Thickness BorderThickness {
        get;
        set {
            if (SetAffectsMeasure(ref field, value))
                BorderCore.BorderThickness = value;
        }
    }

    protected override bool OnAttach() {
        if (base.OnAttach()) {
            strokeStyleChanged = true;
            return true;
        }

        return false;
    }

    public override void Update(RenderContext2D context) {
        base.Update(context);
        if (strokeStyleChanged) {
            BorderCore.StrokeStyle = new StrokeStyle(context.DeviceResources.Factory2D,
                new StrokeStyleProperties {
                    DashCap = StrokeDashCap,
                    StartCap = StrokeStartLineCap,
                    EndCap = StrokeEndLineCap,
                    DashOffset = StrokeDashOffset,
                    LineJoin = StrokeLineJoin,
                    MiterLimit = Math.Max(1, StrokeMiterLimit),
                    DashStyle = StrokeDashStyle
                });
            strokeStyleChanged = false;
        }
    }

    protected override Size2F MeasureOverride(Size2F availableSize) {
        if (Content != null) {
            var margin = new Size2F(
                BorderThickness.Left / 2 + Padding.Left + BorderThickness.Right / 2 + Padding.Right,
                BorderThickness.Top / 2 + Padding.Top + BorderThickness.Bottom / 2 + Padding.Bottom);
            margin.Width *= DpiScale;
            margin.Height *= DpiScale;
            var childAvail = new Size2F(Math.Max(0, availableSize.Width - margin.Width),
                                        Math.Max(0, availableSize.Height - margin.Height));

            var size = base.MeasureOverride(childAvail);
            if (Width != float.PositiveInfinity && Height != float.PositiveInfinity) return availableSize;

            if (Width != float.PositiveInfinity) size.Width = Width * DpiScale;
            if (Height != float.PositiveInfinity) size.Height = Height * DpiScale;
            return size;
        } else {
            var size = new Size2F(BorderThickness.Left / 2 + Padding.Left + BorderThickness.Right / 2 +
                                  Padding.Right +
                                  MarginWidthHeight.X + Width == float.PositiveInfinity
                                      ? 0
                                      : Width,
                                  BorderThickness.Top / 2 + Padding.Top + BorderThickness.Bottom / 2 +
                                  Padding.Bottom +
                                  MarginWidthHeight.Y + Height == float.PositiveInfinity
                                      ? 0
                                      : Height);
            size.Width *= DpiScale;
            size.Height *= DpiScale;
            return size;
        }
    }

    protected override RectangleF ArrangeOverride(RectangleF finalSize) {
        var contentRect = new RectangleF(finalSize.Left, finalSize.Top, finalSize.Width, finalSize.Height);
        contentRect.Left += (BorderThickness.Left / 2 + Padding.Left) * DpiScale;
        contentRect.Right -= (BorderThickness.Right / 2 + Padding.Right) * DpiScale;
        contentRect.Top += (BorderThickness.Top / 2 + Padding.Top) * DpiScale;
        contentRect.Bottom -= (BorderThickness.Bottom / 2 + Padding.Bottom) * DpiScale;
        base.ArrangeOverride(contentRect);
        return finalSize;
    }
}

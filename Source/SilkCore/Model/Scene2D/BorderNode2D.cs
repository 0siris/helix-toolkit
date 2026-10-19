using HelixToolkit.SharpDX.Core;
/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/
using HelixToolkit.SharpDX.Core.Core2D;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Native;

namespace HelixToolkit.SharpDX.Core.Model.Scene2D;

/// <summary>Arranges content inside a rounded border with lazily updated stroke resources.</summary>
public class BorderNode2D : ContentNode2D {
    /// <summary>Gets the border-specific rendering state.</summary>
    private BorderRenderCore2D BorderCore => RenderCore as BorderRenderCore2D
                                             ?? throw new InvalidOperationException("Border render core is not initialized.");

    /// <summary>Tracks whether the stroke style must be recreated from the current factory.</summary>
    private bool strokeStyleChanged = true;

    /// <summary>Gets or sets the border corner radius.</summary>
    public float CornerRadius {
        get => BorderCore.CornerRadius;
        set => BorderCore.CornerRadius = value;
    }

    /// <summary>Gets or sets the space between the border and its content.</summary>
    public Thickness Padding {
        get;
        set => SetAffectsMeasure(ref field, value);
    } = new(0);

    /// <summary>Gets or sets the border stroke brush.</summary>
    public Brush? BorderBrush {
        get => BorderCore.StrokeBrush;
        set => BorderCore.StrokeBrush = value;
    }

    /// <summary>Gets or sets the cap used for stroke dashes.</summary>
    public CapStyle StrokeDashCap {
        get;
        set {
            if (SetAffectsRender(ref field, value)) strokeStyleChanged = true;
        }
    } = CapStyle.Flat;

    /// <summary>Gets or sets the cap at the beginning of the stroke.</summary>
    public CapStyle StrokeStartLineCap {
        get;
        set {
            if (SetAffectsRender(ref field, value)) strokeStyleChanged = true;
        }
    } = CapStyle.Flat;

    /// <summary>Gets or sets the cap at the end of the stroke.</summary>
    public CapStyle StrokeEndLineCap {
        get;
        set {
            if (SetAffectsRender(ref field, value)) strokeStyleChanged = true;
        }
    } = CapStyle.Flat;

    /// <summary>Gets or sets the predefined stroke dash pattern.</summary>
    public DashStyle StrokeDashStyle {
        get;
        set {
            if (SetAffectsRender(ref field, value)) strokeStyleChanged = true;
        }
    } = DashStyle.Solid;

    /// <summary>Gets or sets the stroke dash pattern offset.</summary>
    public float StrokeDashOffset {
        get;
        set {
            if (SetAffectsRender(ref field, value)) strokeStyleChanged = true;
        }
    }

    /// <summary>Gets or sets the join between stroke segments.</summary>
    public LineJoin StrokeLineJoin {
        get;
        set {
            if (SetAffectsRender(ref field, value)) strokeStyleChanged = true;
        }
    } = LineJoin.Miter;

    /// <summary>Gets or sets the maximum stroke miter length ratio.</summary>
    public float StrokeMiterLimit {
        get;
        set {
            if (SetAffectsRender(ref field, value)) strokeStyleChanged = true;
        }
    } = 1;

    /// <summary>Gets or sets the width of each border edge.</summary>
    public Thickness BorderThickness {
        get;
        set {
            if (SetAffectsMeasure(ref field, value))
                BorderCore.BorderThickness = value;
        }
    }

    /// <inheritdoc />
    protected override bool OnAttach() {
        if (base.OnAttach()) {
            strokeStyleChanged = true;
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public override void Update(RenderContext2D context) {
        base.Update(context);
        if (!CanContinueScenePreparation) return;
        if (strokeStyleChanged) {
            BorderCore.StrokeStyle = new StrokeStyle(
                context.DeviceResources.Factory2D,
                new StrokeStyleProperties {
                    DashCap = StrokeDashCap,
                    StartCap = StrokeStartLineCap,
                    EndCap = StrokeEndLineCap,
                    DashOffset = StrokeDashOffset,
                    LineJoin = StrokeLineJoin,
                    MiterLimit = Math.Max(1, StrokeMiterLimit),
                    DashStyle = StrokeDashStyle
                }
            );
            strokeStyleChanged = false;
        }
    }

    /// <inheritdoc />
    protected override Size2F MeasureOverride(Size2F availableSize) {
        if (Content != null) {
            var margin = new Size2F(
                BorderThickness.Left / 2 + Padding.Left + BorderThickness.Right / 2 + Padding.Right,
                BorderThickness.Top / 2 + Padding.Top + BorderThickness.Bottom / 2 + Padding.Bottom
            );
            margin.Width *= DpiScale;
            margin.Height *= DpiScale;
            var childAvail = new Size2F(
                Math.Max(0, availableSize.Width - margin.Width),
                Math.Max(0, availableSize.Height - margin.Height)
            );

            var size = base.MeasureOverride(childAvail);
            if (Width != float.PositiveInfinity && Height != float.PositiveInfinity) return availableSize;

            if (Width != float.PositiveInfinity) size.Width = Width * DpiScale;
            if (Height != float.PositiveInfinity) size.Height = Height * DpiScale;
            return size;
        } else {
            var size = new Size2F(
                BorderThickness.Left / 2 + Padding.Left + BorderThickness.Right / 2 +
                Padding.Right +
                MarginWidthHeight.X + Width == float.PositiveInfinity
                    ? 0
                    : Width,
                BorderThickness.Top / 2 + Padding.Top + BorderThickness.Bottom / 2 +
                Padding.Bottom +
                MarginWidthHeight.Y + Height == float.PositiveInfinity
                    ? 0
                    : Height
            );
            size.Width *= DpiScale;
            size.Height *= DpiScale;
            return size;
        }
    }

    /// <inheritdoc />
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
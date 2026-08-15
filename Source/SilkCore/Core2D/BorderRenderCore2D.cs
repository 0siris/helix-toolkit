/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core2D.Abstract;
using HelixToolkit.SharpDX.Core.Core2D.Models;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render;

namespace HelixToolkit.SharpDX.Core.Core2D;
/// <summary>
/// </summary>
public class BorderRenderCore2D : RenderCore2DBase {
    private readonly PathRenderCore2D[] borderRenderCore = [new(), new(), new(), new()];

    private Brush? background;

    private bool isBorderGeometryChanged;

    private Brush? strokeBrush;

    private StrokeStyle? strokeStyle;

    /// <summary>
    ///     Gets or sets the background.
    /// </summary>
    /// <value>
    ///     The background.
    /// </value>
    public Brush? Background {
        get => background;
        set {
            var old = background;
            if (SetAffectsRender(ref background, value)) RemoveAndDispose(ref old);
        }
    }

    /// <summary>
    ///     Gets or sets the stroke brush.
    /// </summary>
    /// <value>
    ///     The stroke brush.
    /// </value>
    public Brush? StrokeBrush {
        get => strokeBrush;
        set {
            var old = strokeBrush;
            if (SetAffectsRender(ref strokeBrush, value)) {
                RemoveAndDispose(ref old);
                foreach (var core in borderRenderCore)
                    core.StrokeBrush = value?.QueryInterface<Brush>();
            }
        }
    }

    /// <summary>
    ///     Gets or sets the stroke thickness.
    /// </summary>
    /// <value>
    ///     The stroke thickness.
    /// </value>
    public Vector4 BorderThickness {
        get;
        set => SetAffectsRender(ref field, value);
    } = Vector4.Zero;

    /// <summary>
    ///     Gets or sets the stroke style.
    /// </summary>
    /// <value>
    ///     The stroke style.
    /// </value>
    public StrokeStyle? StrokeStyle {
        get => strokeStyle;
        set {
            var old = strokeStyle;
            if (SetAffectsRender(ref strokeStyle, value)) {
                RemoveAndDispose(ref old);
                foreach (var core in borderRenderCore)
                    core.StrokeStyle = value?.QueryInterface<StrokeStyle>();
            }
        }
    }

    /// <summary>
    ///     Gets or sets the corner radius.
    /// </summary>
    /// <value>
    ///     The corner radius.
    /// </value>
    public float CornerRadius {
        get;
        set {
            if (SetAffectsRender(ref field, value)) isBorderGeometryChanged = true;
        }
    }

    protected override bool OnAttach(IRenderHost host) {
        if (base.OnAttach(host)) {
            isBorderGeometryChanged = true;
            foreach (var core in borderRenderCore) core.Attach(host);
            return true;
        }

        return false;
    }

    protected override void OnDetach() {
        foreach (var core in borderRenderCore) core.Detach();
        RemoveAndDispose(ref background);
        RemoveAndDispose(ref strokeBrush);
        RemoveAndDispose(ref strokeStyle);
        base.OnDetach();
    }

    protected override void OnLayoutBoundChanged(RectangleF layoutBound) {
        base.OnLayoutBoundChanged(layoutBound);
        isBorderGeometryChanged = true;
    }

    /// <summary>
    ///     Called when [render].
    /// </summary>
    /// <param name="context">The context.</param>
    protected override void OnRender(RenderContext2D context) {
        var roundRect = new RoundedRectangle { Rect = LayoutBound, RadiusX = CornerRadius, RadiusY = CornerRadius };
        if (Background is { } background) context.DeviceContext.FillRoundedRectangle(roundRect, background);
        var thickness = BorderThickness * context.DpiScale;
        if (thickness.LengthSquared() > 0
            && StrokeBrush is { } strokeBrush
            && StrokeStyle is { } strokeStyle) {
            if (thickness.X == thickness.Y && thickness.X == thickness.Z && thickness.X == thickness.W) {
                context.DeviceContext.DrawRoundedRectangle(roundRect, strokeBrush, thickness.X, strokeStyle);
            } else {
                if (isBorderGeometryChanged) {
                    var topLeft = LayoutBound.TopLeft + new Vector2(0, CornerRadius);
                    var topRight = LayoutBound.TopRight - new Vector2(CornerRadius, 0);
                    var bottomRight = LayoutBound.BottomRight - new Vector2(0, CornerRadius);
                    var bottomLeft = LayoutBound.BottomLeft + new Vector2(CornerRadius, 0);

                    if (thickness.X > 0) {
                        var figures = new List<Figure>();
                        var figure = new Figure(topLeft, false, false);
                        if (CornerRadius > 0)
                            figure.AddSegment(new ArcSegment(LayoutBound.TopLeft + new Vector2(CornerRadius, 0),
                                                             new Size2F(CornerRadius, CornerRadius),
                                                             0,
                                                             SweepDirection.Clockwise,
                                                             ArcSize.Small));
                        figure.AddSegment(new LineSegment(topRight));
                        figures.Add(figure);
                        borderRenderCore[0].Figures = figures;
                        borderRenderCore[0].StrokeWidth = thickness.X;
                    } else {
                        borderRenderCore[0].Figures = null;
                    }

                    if (thickness.Y > 0) {
                        var figures = new List<Figure>();
                        var figure = new Figure(topRight, false, false);
                        if (CornerRadius > 0)
                            figure.AddSegment(new ArcSegment(
                                                  LayoutBound.TopRight + new Vector2(0, CornerRadius),
                                                  new Size2F(CornerRadius, CornerRadius),
                                                  0,
                                                  SweepDirection.Clockwise,
                                                  ArcSize.Small));
                        figure.AddSegment(new LineSegment(bottomRight));
                        figures.Add(figure);
                        borderRenderCore[1].Figures = figures;
                        borderRenderCore[1].StrokeWidth = thickness.Y;
                    } else {
                        borderRenderCore[1].Figures = null;
                    }

                    if (thickness.Z > 0) {
                        var figures = new List<Figure>();
                        var figure = new Figure(bottomRight, false, false);
                        if (CornerRadius > 0)
                            figure.AddSegment(new ArcSegment(
                                                  LayoutBound.BottomRight - new Vector2(CornerRadius, 0),
                                                  new Size2F(CornerRadius, CornerRadius),
                                                  0,
                                                  SweepDirection.Clockwise,
                                                  ArcSize.Small));
                        figure.AddSegment(new LineSegment(bottomLeft));
                        figures.Add(figure);
                        borderRenderCore[2].Figures = figures;
                        borderRenderCore[2].StrokeWidth = thickness.Z;
                    } else {
                        borderRenderCore[2].Figures = null;
                    }

                    if (thickness.W > 0) {
                        var figures = new List<Figure>();
                        var figure = new Figure(bottomLeft, false, false);
                        if (CornerRadius > 0)
                            figure.AddSegment(new ArcSegment(
                                                  LayoutBound.BottomLeft - new Vector2(0, CornerRadius),
                                                  new Size2F(CornerRadius, CornerRadius),
                                                  0,
                                                  SweepDirection.Clockwise,
                                                  ArcSize.Small));
                        figure.AddSegment(new LineSegment(topLeft));
                        figures.Add(figure);
                        borderRenderCore[3].Figures = figures;
                        borderRenderCore[3].StrokeWidth = thickness.W;
                    } else {
                        borderRenderCore[3].Figures = null;
                    }

                    isBorderGeometryChanged = false;
                }

                foreach (var core in borderRenderCore) {
                    core.Transform = Transform;
                    core.LocalTransform = LocalTransform;
                    core.Render(context);
                }
            }
        }
    }
}

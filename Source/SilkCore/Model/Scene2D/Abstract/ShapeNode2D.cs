/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core2D.Abstract;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render;

namespace HelixToolkit.SharpDX.Core.Model.Scene2D.Abstract;
public abstract class ShapeNode2D : SceneNode2D {
    private bool strokeStyleChanged = true;

    private ShapeRenderCore2DBase ShapeCore
        => RenderCore as ShapeRenderCore2DBase
            ?? throw new InvalidOperationException("The shape render core has not been created.");

    public Brush? Fill {
        get => ShapeCore.FillBrush;
        set => ShapeCore.FillBrush = value;
    }

    public Brush? Stroke {
        get => ShapeCore.StrokeBrush;
        set => ShapeCore.StrokeBrush = value;
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

    public float StrokeThickness {
        get => ShapeCore.StrokeWidth / DpiScale;
        set => ShapeCore.StrokeWidth = value * DpiScale;
    }

    public float[] StrokeDashArray {
        get;
        set {
            if (SetAffectsRender(ref field, value)) strokeStyleChanged = true;
        }
    } = [];

    protected override RenderCore2D CreateRenderCore() {
        return CreateShapeRenderCore();
    }

    protected abstract ShapeRenderCore2DBase CreateShapeRenderCore();

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
            ShapeCore.StrokeStyle = new StrokeStyle(context.DeviceResources.Factory2D,
                                                          new StrokeStyleProperties {
                                                              DashCap = StrokeDashCap,
                                                              StartCap = StrokeStartLineCap,
                                                              EndCap = StrokeEndLineCap,
                                                              DashOffset = StrokeDashOffset,
                                                              LineJoin = StrokeLineJoin,
                                                              MiterLimit = Math.Max(1, StrokeMiterLimit),
                                                              DashStyle = StrokeDashStyle
                                                          },
                                                           StrokeDashArray);
            strokeStyleChanged = false;
        }
    }
}

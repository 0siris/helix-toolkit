/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core2D;

namespace HelixToolkit.SharpDX.Core.Model.Scene2D;
public abstract class ShapeNode2D : SceneNode2D {
    protected ShapeRenderCore2DBase shapeRenderable;

    private bool strokeStyleChanged = true;

    public Brush Fill {
        get => (RenderCore as ShapeRenderCore2DBase).FillBrush;
        set => (RenderCore as ShapeRenderCore2DBase).FillBrush = value;
    }

    public Brush Stroke {
        get => (RenderCore as ShapeRenderCore2DBase).StrokeBrush;
        set => (RenderCore as ShapeRenderCore2DBase).StrokeBrush = value;
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
        get => (RenderCore as ShapeRenderCore2DBase).StrokeWidth / DpiScale;
        set => (RenderCore as ShapeRenderCore2DBase).StrokeWidth = value * DpiScale;
    }

    public float[] StrokeDashArray {
        get;
        set {
            if (SetAffectsRender(ref field, value)) strokeStyleChanged = true;
        }
    }

    protected override RenderCore2D CreateRenderCore() {
        shapeRenderable = CreateShapeRenderCore();
        return shapeRenderable;
    }

    protected abstract ShapeRenderCore2DBase CreateShapeRenderCore();

    protected override bool OnAttach(IRenderHost host) {
        if (base.OnAttach(host)) {
            strokeStyleChanged = true;
            return true;
        }

        return false;
    }

    public override void Update(RenderContext2D context) {
        base.Update(context);
        if (strokeStyleChanged) {
            shapeRenderable.StrokeStyle = new StrokeStyle(context.DeviceResources.Factory2D,
                                                          new StrokeStyleProperties {
                                                              DashCap = StrokeDashCap,
                                                              StartCap = StrokeStartLineCap,
                                                              EndCap = StrokeEndLineCap,
                                                              DashOffset = StrokeDashOffset,
                                                              LineJoin = StrokeLineJoin,
                                                              MiterLimit = Math.Max(1, StrokeMiterLimit),
                                                              DashStyle = StrokeDashStyle
                                                          },
                                                          StrokeDashArray == null
                                                              ? []
                                                              : StrokeDashArray);
            strokeStyleChanged = false;
        }
    }
}

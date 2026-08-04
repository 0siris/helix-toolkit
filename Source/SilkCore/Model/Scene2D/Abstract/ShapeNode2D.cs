/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core2D;

namespace HelixToolkit.SharpDX.Core.Model.Scene2D;
public abstract class ShapeNode2D : SceneNode2D {
    protected ShapeRenderCore2DBase shapeRenderable;

    private float[] strokeDashArray;

    private CapStyle strokeDashCap = CapStyle.Flat;

    private float strokeDashOffset;

    private DashStyle strokeDashStyle = DashStyle.Solid;

    private CapStyle strokeEndLineCap = CapStyle.Flat;

    private LineJoin strokeLineJoin = LineJoin.Miter;

    private float strokeMiterLimit = 1;

    private CapStyle strokeStartLineCap = CapStyle.Flat;

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
        get => strokeDashCap;
        set {
            if (SetAffectsRender(ref strokeDashCap, value)) strokeStyleChanged = true;
        }
    }

    public CapStyle StrokeStartLineCap {
        get => strokeStartLineCap;
        set {
            if (SetAffectsRender(ref strokeStartLineCap, value)) strokeStyleChanged = true;
        }
    }

    public CapStyle StrokeEndLineCap {
        get => strokeEndLineCap;
        set {
            if (SetAffectsRender(ref strokeEndLineCap, value)) strokeStyleChanged = true;
        }
    }

    public DashStyle StrokeDashStyle {
        get => strokeDashStyle;
        set {
            if (SetAffectsRender(ref strokeDashStyle, value)) strokeStyleChanged = true;
        }
    }

    public float StrokeDashOffset {
        get => strokeDashOffset;
        set {
            if (SetAffectsRender(ref strokeDashOffset, value)) strokeStyleChanged = true;
        }
    }

    public LineJoin StrokeLineJoin {
        get => strokeLineJoin;
        set {
            if (SetAffectsRender(ref strokeLineJoin, value)) strokeStyleChanged = true;
        }
    }

    public float StrokeMiterLimit {
        get => strokeMiterLimit;
        set {
            if (SetAffectsRender(ref strokeMiterLimit, value)) strokeStyleChanged = true;
        }
    }

    public float StrokeThickness {
        get => (RenderCore as ShapeRenderCore2DBase).StrokeWidth / DpiScale;
        set => (RenderCore as ShapeRenderCore2DBase).StrokeWidth = value * DpiScale;
    }

    public float[] StrokeDashArray {
        get => strokeDashArray;
        set {
            if (SetAffectsRender(ref strokeDashArray, value)) strokeStyleChanged = true;
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

using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Core2D;
/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Native;

namespace HelixToolkit.SharpDX.Core.Model.Scene2D;
/// <summary>Provides shared shape brushes and lazily updated stroke resources.</summary>
public abstract class ShapeNode2D : SceneNode2D {
    /// <summary>Tracks whether the stroke style must be recreated from the current factory.</summary>
    private bool strokeStyleChanged = true;

    /// <summary>Gets the shape-specific rendering state.</summary>
    private ShapeRenderCore2DBase ShapeCore
        => RenderCore as ShapeRenderCore2DBase
            ?? throw new InvalidOperationException("The shape render core has not been created.");

    /// <summary>Gets or sets the shape fill brush.</summary>
    public Brush? Fill {
        get => ShapeCore.FillBrush;
        set => ShapeCore.FillBrush = value;
    }

    /// <summary>Gets or sets the shape stroke brush.</summary>
    public Brush? Stroke {
        get => ShapeCore.StrokeBrush;
        set => ShapeCore.StrokeBrush = value;
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

    /// <summary>Gets or sets the device-independent stroke thickness.</summary>
    public float StrokeThickness {
        get => ShapeCore.StrokeWidth / DpiScale;
        set => ShapeCore.StrokeWidth = value * DpiScale;
    }

    /// <summary>Gets or sets the custom stroke dash lengths.</summary>
    public float[] StrokeDashArray {
        get;
        set {
            if (SetAffectsRender(ref field, value)) strokeStyleChanged = true;
        }
    } = [];

    /// <inheritdoc />
    protected override RenderCore2D CreateRenderCore() {
        return CreateShapeRenderCore();
    }

    /// <summary>Creates the concrete shape rendering state.</summary>
    /// <returns>The new shape render core.</returns>
    protected abstract ShapeRenderCore2DBase CreateShapeRenderCore();

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

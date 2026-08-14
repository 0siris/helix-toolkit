/*
The MIT License(MIT)
Copyright(c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Core;

namespace HelixToolkit.SharpDX.Core.Model.Scene;

/// <summary>
/// </summary>
public class AxisPlaneGridNode : SceneNode {
    /// <summary>
    ///     Initializes a new instance of the <see cref="AxisPlaneGridNode" /> class.
    /// </summary>
    public AxisPlaneGridNode() {
        RenderOrder = 1000;
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [automatic spacing].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [automatic spacing]; otherwise, <c>false</c>.
    /// </value>
    public bool AutoSpacing {
        get => ((AxisPlaneGridCore) RenderCore).AutoSpacing;
        set => ((AxisPlaneGridCore) RenderCore).AutoSpacing = value;
    }

    /// <summary>
    ///     Gets or sets the automatic spacing rate.
    /// </summary>
    /// <value>
    ///     The automatic spacing rate.
    /// </value>
    public float AutoSpacingRate {
        get => ((AxisPlaneGridCore) RenderCore).AutoSpacingRate;
        set => ((AxisPlaneGridCore) RenderCore).AutoSpacingRate = value;
    }

    /// <summary>
    ///     Gets the acutal spacing.
    /// </summary>
    /// <value>
    ///     The acutal spacing.
    /// </value>
    public float AcutalSpacing => ((AxisPlaneGridCore) RenderCore).AcutalSpacing;

    /// <summary>
    ///     Gets or sets the grid spacing.
    /// </summary>
    /// <value>
    ///     The grid spacing.
    /// </value>
    public float GridSpacing {
        get => ((AxisPlaneGridCore) RenderCore).GridSpacing;
        set => ((AxisPlaneGridCore) RenderCore).GridSpacing = value;
    }

    /// <summary>
    ///     Gets or sets the grid thickness.
    /// </summary>
    /// <value>
    ///     The grid thickness.
    /// </value>
    public float GridThickness {
        get => ((AxisPlaneGridCore) RenderCore).GridThickness;
        set => ((AxisPlaneGridCore) RenderCore).GridThickness = value;
    }

    /// <summary>
    ///     Gets or sets the fading factor.
    /// </summary>
    /// <value>
    ///     The fading factor.
    /// </value>
    public float FadingFactor {
        get => ((AxisPlaneGridCore) RenderCore).FadingFactor;
        set => ((AxisPlaneGridCore) RenderCore).FadingFactor = value;
    }

    /// <summary>
    ///     Gets or sets the color of the plane.
    /// </summary>
    /// <value>
    ///     The color of the plane.
    /// </value>
    public Color4 PlaneColor {
        get => ((AxisPlaneGridCore) RenderCore).PlaneColor;
        set => ((AxisPlaneGridCore) RenderCore).PlaneColor = value;
    }

    /// <summary>
    ///     Gets or sets the color of the grid.
    /// </summary>
    /// <value>
    ///     The color of the grid.
    /// </value>
    public Color4 GridColor {
        get => ((AxisPlaneGridCore) RenderCore).GridColor;
        set => ((AxisPlaneGridCore) RenderCore).GridColor = value;
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [render shadow map].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [render shadow map]; otherwise, <c>false</c>.
    /// </value>
    public bool RenderShadowMap {
        get => ((AxisPlaneGridCore) RenderCore).RenderShadowMap;
        set => ((AxisPlaneGridCore) RenderCore).RenderShadowMap = value;
    }

    /// <summary>
    ///     Gets or sets up axis.
    /// </summary>
    /// <value>
    ///     Up axis.
    /// </value>
    public Axis UpAxis {
        get => ((AxisPlaneGridCore) RenderCore).UpAxis;
        set => ((AxisPlaneGridCore) RenderCore).UpAxis = value;
    }

    /// <summary>
    ///     Gets or sets the axis plane offset.
    /// </summary>
    /// <value>
    ///     The offset.
    /// </value>
    public float Offset {
        get => ((AxisPlaneGridCore) RenderCore).Offset;
        set => ((AxisPlaneGridCore) RenderCore).Offset = value;
    }

    /// <summary>
    ///     Gets or sets the type of the grid.
    /// </summary>
    /// <value>
    ///     The type of the grid.
    /// </value>
    public GridPattern GridPattern {
        get => ((AxisPlaneGridCore) RenderCore).GridPattern;
        set => ((AxisPlaneGridCore) RenderCore).GridPattern = value;
    }

    protected override RenderCore OnCreateRenderCore() => new AxisPlaneGridCore();

    protected override IRenderTechnique OnCreateRenderTechnique(IEffectsManager effectsManager) => effectsManager[DefaultRenderTechniqueNames.PlaneGrid];

    protected override bool OnHitTest(
        HitTestContext context,
        Matrix totalModelMatrix,
        ref List<HitTestResult> hits
    ) {
        var normal = Vector3.Zero;
        switch (UpAxis) {
            case Axis.X:
                normal = Vector3.UnitX;
                break;
            case Axis.Y:
                normal = Vector3.UnitY;
                break;
            case Axis.Z:
                normal = Vector3.UnitZ;
                break;
        }

        var plane = new Plane(normal, -Offset);
        var ray = context.RayWs;
        if (Collision.RayIntersectsPlane(ref ray, ref plane, out Vector3 point)) {
            var hitTestResult = new HitTestResult {
                IsValid = true,
                NormalAtHit = normal,
                Distance = (context.RayWs.Position - point).Length,
                PointHit = point,
                ModelHit = WrapperSource
            };
            hits.Add(hitTestResult);
            return true;
        }

        return false;
    }
}
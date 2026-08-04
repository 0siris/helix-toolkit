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
        get => (RenderCore as AxisPlaneGridCore).AutoSpacing;
        set => (RenderCore as AxisPlaneGridCore).AutoSpacing = value;
    }

    /// <summary>
    ///     Gets or sets the automatic spacing rate.
    /// </summary>
    /// <value>
    ///     The automatic spacing rate.
    /// </value>
    public float AutoSpacingRate {
        get => (RenderCore as AxisPlaneGridCore).AutoSpacingRate;
        set => (RenderCore as AxisPlaneGridCore).AutoSpacingRate = value;
    }

    /// <summary>
    ///     Gets the acutal spacing.
    /// </summary>
    /// <value>
    ///     The acutal spacing.
    /// </value>
    public float AcutalSpacing => (RenderCore as AxisPlaneGridCore).AcutalSpacing;

    /// <summary>
    ///     Gets or sets the grid spacing.
    /// </summary>
    /// <value>
    ///     The grid spacing.
    /// </value>
    public float GridSpacing {
        get => (RenderCore as AxisPlaneGridCore).GridSpacing;
        set => (RenderCore as AxisPlaneGridCore).GridSpacing = value;
    }

    /// <summary>
    ///     Gets or sets the grid thickness.
    /// </summary>
    /// <value>
    ///     The grid thickness.
    /// </value>
    public float GridThickness {
        get => (RenderCore as AxisPlaneGridCore).GridThickness;
        set => (RenderCore as AxisPlaneGridCore).GridThickness = value;
    }

    /// <summary>
    ///     Gets or sets the fading factor.
    /// </summary>
    /// <value>
    ///     The fading factor.
    /// </value>
    public float FadingFactor {
        get => (RenderCore as AxisPlaneGridCore).FadingFactor;
        set => (RenderCore as AxisPlaneGridCore).FadingFactor = value;
    }

    /// <summary>
    ///     Gets or sets the color of the plane.
    /// </summary>
    /// <value>
    ///     The color of the plane.
    /// </value>
    public Color4 PlaneColor {
        get => (RenderCore as AxisPlaneGridCore).PlaneColor;
        set => (RenderCore as AxisPlaneGridCore).PlaneColor = value;
    }

    /// <summary>
    ///     Gets or sets the color of the grid.
    /// </summary>
    /// <value>
    ///     The color of the grid.
    /// </value>
    public Color4 GridColor {
        get => (RenderCore as AxisPlaneGridCore).GridColor;
        set => (RenderCore as AxisPlaneGridCore).GridColor = value;
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [render shadow map].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [render shadow map]; otherwise, <c>false</c>.
    /// </value>
    public bool RenderShadowMap {
        get => (RenderCore as AxisPlaneGridCore).RenderShadowMap;
        set => (RenderCore as AxisPlaneGridCore).RenderShadowMap = value;
    }

    /// <summary>
    ///     Gets or sets up axis.
    /// </summary>
    /// <value>
    ///     Up axis.
    /// </value>
    public Axis UpAxis {
        get => (RenderCore as AxisPlaneGridCore).UpAxis;
        set => (RenderCore as AxisPlaneGridCore).UpAxis = value;
    }

    /// <summary>
    ///     Gets or sets the axis plane offset.
    /// </summary>
    /// <value>
    ///     The offset.
    /// </value>
    public float Offset {
        get => (RenderCore as AxisPlaneGridCore).Offset;
        set => (RenderCore as AxisPlaneGridCore).Offset = value;
    }

    /// <summary>
    ///     Gets or sets the type of the grid.
    /// </summary>
    /// <value>
    ///     The type of the grid.
    /// </value>
    public GridPattern GridPattern {
        get => (RenderCore as AxisPlaneGridCore).GridPattern;
        set => (RenderCore as AxisPlaneGridCore).GridPattern = value;
    }

    protected override RenderCore OnCreateRenderCore() {
        return new AxisPlaneGridCore();
    }

    protected override IRenderTechnique OnCreateRenderTechnique(IEffectsManager effectsManager) {
        return effectsManager[DefaultRenderTechniqueNames.PlaneGrid];
    }

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
        var ray = context.RayWS;
        if (Collision.RayIntersectsPlane(ref ray, ref plane, out Vector3 point)) {
            var hitTestResult = new HitTestResult {
                IsValid = true,
                NormalAtHit = normal,
                Distance = (context.RayWS.Position - point).Length,
                PointHit = point,
                ModelHit = WrapperSource
            };
            hits.Add(hitTestResult);
            return true;
        }

        return false;
    }
}

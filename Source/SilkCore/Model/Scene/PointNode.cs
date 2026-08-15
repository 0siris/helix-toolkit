/*
The MIT License(MIT)
Copyright(c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Core.Buffers;
using HelixToolkit.SharpDX.Core.Extensions;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Model.Scene;
/// <summary>
/// </summary>
public class PointNode : MaterialGeometryNode {
    /// <summary>
    ///     Distances the ray to point.
    /// </summary>
    /// <param name="r">The r.</param>
    /// <param name="p">The p.</param>
    /// <returns></returns>
    public static double DistanceRayToPoint(Ray r, Vector3 p) {
        var v = r.Direction;
        var w = p - r.Position;

        var c1 = SilkMath.Dot(w, v);
        var c2 = SilkMath.Dot(v, v);
        var b = c1 / c2;

        var pb = r.Position + v * b;
        return (p - pb).Length;
    }

    /// <summary>
    ///     Called when [create buffer model].
    /// </summary>
    /// <param name="modelGuid"></param>
    /// <param name="geometry"></param>
    /// <returns></returns>
    protected override IAttachableBufferModel OnCreateBufferModel(Guid modelGuid, Geometry3D? geometry) {
        var effectsManager = EffectsManager
            ?? throw new InvalidOperationException("An effects manager is required to create a point buffer.");
        return geometry is { IsDynamic: true }
            ? effectsManager.GeometryBufferManager.Register<DynamicPointGeometryBufferModel>(modelGuid, geometry)
            : effectsManager.GeometryBufferManager.Register<DefaultPointGeometryBufferModel>(modelGuid, geometry);
    }

    /// <summary>
    ///     Called when [create render core].
    /// </summary>
    /// <returns></returns>
    protected override RenderCore OnCreateRenderCore() => new PointLineRenderCore();

    /// <summary>
    ///     Create raster state description.
    /// </summary>
    /// <returns></returns>
    protected override RasterizerStateDescription CreateRasterState() => new() {
        FillMode = FillMode,
        CullMode = CullMode.None,
        DepthBias = DepthBias,
        DepthBiasClamp = -1000,
        SlopeScaledDepthBias = SlopeScaledDepthBias,
        IsDepthClipEnabled = IsDepthClipEnabled,
        IsFrontCounterClockwise = true,
        IsMultisampleEnabled = false,
        IsScissorEnabled = !IsThrowingShadow && IsScissorEnabled
    };

    protected override IRenderTechnique OnCreateRenderTechnique(IEffectsManager effectsManager) => effectsManager[DefaultRenderTechniqueNames.Points];

    /// <summary>
    ///     <para>Determine if this can be rendered.</para>
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    protected override bool CanRender(RenderContext context) {
        if (base.CanRender(context)) return !context.RenderHost.IsDeferredLighting;

        return false;
    }

    protected override bool OnCheckGeometry(Geometry3D? geometry) => base.OnCheckGeometry(geometry) && geometry is PointGeometry3D;

    protected override bool PreHitTestOnBounds(HitTestContext context) {
        var center = BoundsSphereWithTransform.Center;
        var centerSp = context.RenderMatrices.Project(center);
        if (centerSp is {X: >= 0, Y: >= 0}
            && (centerSp - context.HitPointSp).Length <= hitTestThickness)
            return true;
        return base.PreHitTestOnBounds(context);
    }

    protected override bool OnHitTest(
        HitTestContext context,
        Matrix totalModelMatrix,
        ref List<HitTestResult> hits
    )
    {
        if (Geometry is not PointGeometry3D geometry)
            return false;

        return geometry.HitTest(context, totalModelMatrix, ref hits, WrapperSource ?? this, (float)HitTestThickness);
    }

    #region Properties

    private double hitTestThickness = 4;

    /// <summary>
    ///     Used only for point/line hit test
    /// </summary>
    public double HitTestThickness {
        get => hitTestThickness;
        set => Set(ref hitTestThickness, value);
    }

    #endregion
}

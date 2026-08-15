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
public class LineNode : MaterialGeometryNode {
    /// <summary>
    ///     Used only for point/line hit test
    /// </summary>
    public double HitTestThickness {
        get;
        set => Set(ref field, value);
    } = 1;

    /// <summary>
    ///     Called when [create buffer model].
    /// </summary>
    /// <param name="modelGuid"></param>
    /// <param name="geometry"></param>
    /// <returns></returns>
    protected override IAttachableBufferModel OnCreateBufferModel(Guid modelGuid, Geometry3D? geometry) {
        var effectsManager = EffectsManager
            ?? throw new InvalidOperationException("An effects manager is required to create a line buffer.");
        return geometry is { IsDynamic: true }
            ? effectsManager.GeometryBufferManager.Register<DynamicLineGeometryBufferModel>(modelGuid, geometry)
            : effectsManager.GeometryBufferManager.Register<DefaultLineGeometryBufferModel>(modelGuid, geometry);
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

        IsMultisampleEnabled = IsMsaaEnabled,
        //IsAntialiasedLineEnabled = true, // Intel HD 3000 doesn't like this (#10051) and it's not needed
        IsScissorEnabled = !IsThrowingShadow && IsScissorEnabled
    };

    protected override IRenderTechnique OnCreateRenderTechnique(IEffectsManager effectsManager) => effectsManager[DefaultRenderTechniqueNames.Lines];

    protected override bool CanRender(RenderContext context) {
        if (base.CanRender(context)) return !context.RenderHost.IsDeferredLighting;

        return false;
    }

    protected override bool OnCheckGeometry(Geometry3D? geometry) => base.OnCheckGeometry(geometry) && geometry is LineGeometry3D;

    protected override bool OnHitTest(
        HitTestContext context,
        Matrix totalModelMatrix,
        ref List<HitTestResult> hits
    )
    {
        if (Geometry is not LineGeometry3D geometry)
            return false;

        return geometry.HitTest(context, totalModelMatrix, ref hits, WrapperSource ?? this, (float)HitTestThickness);
    }

    protected override bool PreHitTestOnBounds(HitTestContext context) {
        var rayWs = context.RayWs;
        return BoundsSphereWithTransform.Intersects(ref rayWs);
    }
}

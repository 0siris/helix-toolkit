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
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Model.Scene;
/// <summary>
/// </summary>
public class BillboardNode : MaterialGeometryNode {
    /// <summary>
    ///     Called when [create render core].
    /// </summary>
    /// <returns></returns>
    protected override RenderCore OnCreateRenderCore() => new PointLineRenderCore();

    /// <summary>
    ///     Called when [create buffer model].
    /// </summary>
    /// <param name="modelGuid"></param>
    /// <param name="geometry"></param>
    /// <returns></returns>
    protected override IAttachableBufferModel OnCreateBufferModel(Guid modelGuid, Geometry3D? geometry) {
        var effectsManager = EffectsManager
            ?? throw new InvalidOperationException("An effects manager is required to create a billboard buffer.");
        var buffer = geometry is { IsDynamic: true }
                         ? effectsManager.GeometryBufferManager.Register<DynamicBillboardBufferModel>(modelGuid, geometry)
                         : effectsManager.GeometryBufferManager.Register<DefaultBillboardBufferModel>(modelGuid, geometry);
        if (geometry is IBillboardText b && Material is IBillboardRenderParams m) m.Type = b.Type;
        return buffer;
    }

    protected override IRenderTechnique OnCreateRenderTechnique(IEffectsManager effectsManager) => effectsManager[DefaultRenderTechniqueNames.BillboardText];

    public override bool TestViewFrustum(ref BoundingFrustum viewFrustum) {
        if (!EnableViewFrustumCheck) return true;
        if (Geometry is IBillboardText {IsInitialized: false}) return true;
        return BoundingFrustumExtensions.Intersects(ref viewFrustum,
                                                    ref BoundManager
                                                        .BoundsSphereWithTransform); // viewFrustum.Intersects(ref sphere);
    }

    /// <summary>
    ///     Called when [check geometry].
    /// </summary>
    /// <param name="geometry">The geometry.</param>
    /// <returns></returns>
    protected override bool OnCheckGeometry(Geometry3D? geometry) => geometry is IBillboardText;

    /// <summary>
    ///     Create raster state description.
    /// </summary>
    /// <returns></returns>
    protected override RasterizerStateDescription CreateRasterState() => new() {
        FillMode = FillMode.Solid,
        CullMode = CullMode.None,
        DepthBias = DepthBias,
        DepthBiasClamp = -1000,
        SlopeScaledDepthBias = SlopeScaledDepthBias,
        IsDepthClipEnabled = true,
        IsFrontCounterClockwise = false,

        IsMultisampleEnabled = false,
        //IsAntialiasedLineEnabled = true,
        IsScissorEnabled = !IsThrowingShadow && IsScissorEnabled
    };

    /// <summary>
    ///     Called when [hit test].
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="totalModelMatrix">The total model matrix.</param>
    /// <param name="hits">The hits.</param>
    /// <returns></returns>
    protected override bool OnHitTest(
        HitTestContext context,
        Matrix totalModelMatrix,
        ref List<HitTestResult> hits
    ) {
        if (Material is BillboardMaterialCore c && Geometry is BillboardBase billboard)
            return billboard.HitTest(context, totalModelMatrix, ref hits, WrapperSource ?? this, c.FixedSize);

        return false;
    }

    protected override bool PreHitTestOnBounds(HitTestContext context) => true;
}

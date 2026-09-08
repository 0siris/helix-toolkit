/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Core.Batching;
using HelixToolkit.SharpDX.Core.Extensions;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Utilities;
using HelixToolkit.SharpDX.Core.Utilities.Octrees;
using HelixToolkit.SharpDX.Core.Utilities.Octrees.StaticOctrees;

namespace HelixToolkit.SharpDX.Core.Model.Scene;

/// <summary>
///     Static mesh batching. Supports multiple <see cref="Materials" />. All geometries are merged into single buffer for
///     rendering. Indivisual material color infomations are encoded into vertex buffer.
///     <para>
///         <see cref="Material" /> is used if <see cref="Materials" /> = null. And also used for shared material texture
///         binding.
///     </para>
/// </summary>
public class BatchedMeshNode : SceneNode, IHitable, IThrowingShadow, IBoundable, IApplyPostEffect {
    public BatchedMeshNode() {
        HasBound = true;
        TransformChanged += BatchedMeshNode_OnTransformChanged;
    }

    private void BatchedMeshNode_OnTransformChanged(object? sender, TransformArgs e) {
        UpdateBoundsWithTransform();
    }

    protected override RenderCore OnCreateRenderCore() => new MeshRenderCore {
        Batched = true
    };

    protected override IRenderTechnique OnCreateRenderTechnique(IEffectsManager effectsManager) => effectsManager[DefaultRenderTechniqueNames.MeshBatched];

    /// <summary>
    ///     Called when [raster state changed].
    /// </summary>
    protected virtual void OnRasterStateChanged() {
        if (IsAttached && RenderCore is IGeometryRenderCore r)
            r.RasterDescription = OnCreateRasterState?.Invoke() ?? CreateRasterState();
    }

    /// <summary>
    ///     Create raster state description.
    /// </summary>
    /// <returns></returns>
    protected virtual RasterizerStateDescription CreateRasterState() => new() {
        FillMode = FillMode,
        CullMode = CullMode,
        DepthBias = DepthBias,
        DepthBiasClamp = -1000,
        SlopeScaledDepthBias = SlopeScaledDepthBias,
        IsDepthClipEnabled = IsDepthClipEnabled,
        IsFrontCounterClockwise = FrontCcw,
        IsMultisampleEnabled = IsMsaaEnabled,
        IsScissorEnabled = !IsThrowingShadow && IsScissorEnabled
    };

    protected override bool OnAttach(IEffectsManager effectsManager) {
        if (base.OnAttach(effectsManager)) {
            var batchingBuffer = new DefaultStaticMeshBatchingBuffer {
                Geometries = Geometries,
                Materials = materials
            };
            BatchingBuffer = batchingBuffer;
            if (RenderCore is IGeometryRenderCore r) r.GeometryBuffer = batchingBuffer;
            if (RenderCore is MeshRenderCore core) core.D3D12Material = material;
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Called when [attached].
    /// </summary>
    protected override void OnAttached() {
        OnRasterStateChanged();
        base.OnAttached();
    }

    /// <summary>
    ///     Used to override Detach
    /// </summary>
    protected override void OnDetach() {
        RemoveAndDispose(ref BatchingBuffer);
        base.OnDetach();
    }

    protected override OrderKey OnUpdateRenderOrderKey() => OrderKey.Create(RenderOrder, 0);

    /// <summary>
    ///     <para>Determine if this can be rendered.</para>
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    protected override bool CanRender(RenderContext context) {
        if (base.CanRender(context) && Geometries is not null) return true;

        return false;
    }

    private void UpdateBounds() {
        var oldBound = originalBounds;
        var oldBoundSphere = originalBoundsSphere;
        var currentGeometries = geometries;
        if (currentGeometries is {Length: > 0}) {
            var b = currentGeometries[0].Geometry.Bound;
            var bs = currentGeometries[0].Geometry.BoundingSphere;
            foreach (var geo in currentGeometries) {
                b = BoundingBox.Merge(b, geo.Geometry.Bound.Transform(geo.ModelTransform));
                bs = BoundingSphereExtensions.Merge(bs,
                    geo.Geometry.BoundingSphere.TransformBoundingSphere(
                        geo.ModelTransform));
            }

            originalBounds = b;
            originalBoundsSphere = bs;
            BatchedGeometryOctree =
                new StaticBatchedGeometryBoundsOctree(currentGeometries, new OctreeBuildParameter());
        } else {
            originalBounds = MaxBound;
            originalBoundsSphere = MaxBoundSphere;
            BatchedGeometryOctree = null;
        }

        RaiseOnBoundChanged(new BoundChangeArgs<BoundingBox>(ref originalBounds, ref oldBound));
        RaiseOnBoundSphereChanged(
            new BoundChangeArgs<BoundingSphere>(ref originalBoundsSphere, ref oldBoundSphere));
        UpdateBoundsWithTransform();
    }

    private void UpdateBoundsWithTransform() {
        var old = boundsWithTransform;
        boundsWithTransform = originalBounds == MaxBound
            ? MaxBound
            : originalBounds.Transform(TotalModelMatrixInternal);
        
        var oldBs = boundsSphereWithTransform;
        boundsSphereWithTransform = originalBoundsSphere == MaxBoundSphere
            ? MaxBoundSphere
            : originalBoundsSphere.TransformBoundingSphere(TotalModelMatrixInternal);
        RaiseOnTransformBoundChanged(new BoundChangeArgs<BoundingBox>(ref boundsWithTransform, ref old));
        RaiseOnTransformBoundSphereChanged(
            new BoundChangeArgs<BoundingSphere>(ref boundsSphereWithTransform, ref oldBs));
    }

    /// <summary>
    ///     Views the frustum test.
    /// </summary>
    /// <param name="viewFrustum">The view frustum.</param>
    /// <returns></returns>
    public override bool TestViewFrustum(ref BoundingFrustum viewFrustum) {
        if (!EnableViewFrustumCheck) return true;
        return BoundingFrustumExtensions.IsInOrIntersectFrustum(ref viewFrustum,
            ref boundsWithTransform,
            ref boundsSphereWithTransform);
    }

    /// <summary>
    ///     Determines whether this instance [can hit test] the specified context.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns>
    ///     <c>true</c> if this instance [can hit test] the specified context; otherwise, <c>false</c>.
    /// </returns>
    protected override bool CanHitTest(HitTestContext? context)
        => base.CanHitTest(context) && Geometries is {Length: > 0} && Materials is {Length: > 0};

    /// <summary>
    ///     Updates the not render.
    /// </summary>
    /// <param name="context">The context.</param>
    public override void UpdateNotRender(RenderContext context) {
        base.UpdateNotRender(context);
        if (IsHitTestVisible && context.AutoUpdateOctree && Geometries is { } currentGeometries)
            foreach (var geometry in currentGeometries)
                geometry.Geometry?.UpdateOctree();

        if (BatchedGeometryOctree is {TreeBuilt: false})
            BatchedGeometryOctree.BuildTree();
    }

    protected override bool OnHitTest(
        HitTestContext context,
        Matrix totalModelMatrix,
        ref List<HitTestResult> hits
    ) {
        var rayWs = context.RayWs;
        if (rayWs.Intersects(boundsWithTransform) && rayWs.Intersects(boundsSphereWithTransform)) {
            var source = WrapperSource.AsNotNull("Hit-test source must be initialized.");
            if (BatchedGeometryOctree is {TreeBuilt: true})
                return BatchedGeometryOctree.HitTest(context, source, null, totalModelMatrix, ref hits);

            var isHit = false;
            var currentGeometries = Geometries.AsNotNull("Batched geometries must be initialized.");
            for (var i = 0; i < currentGeometries.Length; ++i) {
                var currCount = hits.Count;
                ref var geo = ref currentGeometries[i];
                if (geo.Geometry is MeshGeometry3D mesh) {
                    var hasHit = mesh.HitTest(context,
                        geo.ModelTransform * totalModelMatrix,
                        ref hits,
                        source);
                    if (hasHit && currCount < hits.Count) {
                        var newCount = hits.Count;
                        for (var j = currCount; j < newCount; ++j)
                            hits.Add(new BatchedMeshHitTestResult(i, ref geo, hits[j]));
                        hits.RemoveRange(currCount, newCount - currCount);
                    }

                    isHit |= hasHit;
                }
            }

            return isHit;
        }

        return false;
    }

    #region Properties

    private BatchedMeshGeometryConfig[]? geometries;

    public BatchedMeshGeometryConfig[]? Geometries {
        get => geometries;
        set {
            if (SetAffectsRender(ref geometries, value)) {
                if (IsAttached)
                    BatchingBuffer.AsNotNull()
                        .Geometries = value;
                UpdateBounds();
            }
        }
    }

    private PhongMaterialCore[]? materials;

    public PhongMaterialCore[]? Materials {
        get => materials;
        set {
            if (SetAffectsRender(ref materials, value) && IsAttached) {
                var batchingBuffer = BatchingBuffer.AsNotNull();
                batchingBuffer.Materials = value;
                if (value is null && Material is PhongMaterialCore p) batchingBuffer.Materials = [p];
            }
        }
    }

    #region Bound

    private BoundingBox originalBounds;

    /// <summary>
    ///     Gets the original bound from the geometry. Same as <see cref="Geometry3D.Bound" />
    /// </summary>
    /// <value>
    ///     The original bound.
    /// </value>
    public override BoundingBox OriginalBounds => originalBounds;

    private BoundingSphere originalBoundsSphere;

    /// <summary>
    ///     Gets the original bound sphere from the geometry. Same as <see cref="Geometry3D.BoundingSphere" />
    /// </summary>
    /// <value>
    ///     The original bound sphere.
    /// </value>
    public override BoundingSphere OriginalBoundsSphere => originalBoundsSphere;

    /// <summary>
    ///     Gets the bounds. Usually same as <see cref="OriginalBounds" />. If have instances, the bound will enclose all
    ///     instances.
    /// </summary>
    /// <value>
    ///     The bounds.
    /// </value>
    public override BoundingBox Bounds => originalBounds;

    private BoundingBox boundsWithTransform;

    /// <summary>
    ///     Gets the bounds with transform. Usually same as <see cref="Bounds" />. If have transform, the bound is the
    ///     transformed <see cref="Bounds" />
    /// </summary>
    /// <value>
    ///     The bounds with transform.
    /// </value>
    public override BoundingBox BoundsWithTransform => boundsWithTransform;

    /// <summary>
    ///     Gets the bounds sphere. Usually same as <see cref="OriginalBoundsSphere" />. If have instances, the bound sphere
    ///     will enclose all instances.
    /// </summary>
    /// <value>
    ///     The bounds sphere.
    /// </value>
    public override BoundingSphere BoundsSphere => originalBoundsSphere;

    private BoundingSphere boundsSphereWithTransform;

    /// <summary>
    ///     Gets the bounds sphere with transform. If have transform, the bound is the transformed <see cref="BoundsSphere" />
    /// </summary>
    /// <value>
    ///     The bounds sphere with transform.
    /// </value>
    public override BoundingSphere BoundsSphereWithTransform => boundsSphereWithTransform;

    #endregion

    #region Rasterizer parameters

    /// <summary>
    ///     Gets or sets the depth bias.
    /// </summary>
    /// <value>
    ///     The depth bias.
    /// </value>
    public int DepthBias {
        get;
        set {
            if (Set(ref field, value)) OnRasterStateChanged();
        }
    }

    /// <summary>
    ///     Gets or sets the slope scaled depth bias.
    /// </summary>
    /// <value>
    ///     The slope scaled depth bias.
    /// </value>
    public float SlopeScaledDepthBias {
        get;
        set {
            if (Set(ref field, value)) OnRasterStateChanged();
        }
    }

    /// <summary>
    ///     Gets or sets a value indicating whether Multisampling Anti-Aliasing enabled.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance is msaa enabled; otherwise, <c>false</c>.
    /// </value>
    public bool IsMsaaEnabled {
        get { return field = true; }
        set {
            if (Set(ref field, value)) OnRasterStateChanged();
        }
    } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether this instance is scissor enabled.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance is scissor enabled; otherwise, <c>false</c>.
    /// </value>
    public bool IsScissorEnabled {
        get;
        set {
            if (Set(ref field, value)) OnRasterStateChanged();
        }
    } = true;

    /// <summary>
    ///     Gets or sets the fill mode.
    /// </summary>
    /// <value>
    ///     The fill mode.
    /// </value>
    public FillMode FillMode {
        get;
        set {
            if (Set(ref field, value)) OnRasterStateChanged();
        }
    } = FillMode.Solid;

    /// <summary>
    ///     Gets or sets a value indicating whether this instance is depth clip enabled.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance is depth clip enabled; otherwise, <c>false</c>.
    /// </value>
    public bool IsDepthClipEnabled {
        get;
        set {
            if (Set(ref field, value)) OnRasterStateChanged();
        }
    } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether [front CCW].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [front CCW]; otherwise, <c>false</c>.
    /// </value>
    public bool FrontCcw {
        get;
        set {
            if (Set(ref field, value)) OnRasterStateChanged();
        }
    } = true;

    /// <summary>
    ///     Gets or sets the cull mode.
    /// </summary>
    /// <value>
    ///     The cull mode.
    /// </value>
    public CullMode CullMode {
        get;
        set {
            if (Set(ref field, value)) OnRasterStateChanged();
        }
    } = CullMode.None;

    #endregion Rasterizer parameters

    /// <summary>
    ///     Gets or sets a value indicating whether [enable view frustum check].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [enable view frustum check]; otherwise, <c>false</c>.
    /// </value>
    public bool EnableViewFrustumCheck {
        get => field && HasBound;
        set => Set(ref field, value);
    } = true;

    /// <summary>
    ///     Gets or sets the post effects.
    /// </summary>
    /// <value>
    ///     The post effects.
    /// </value>
    public string? PostEffects {
        get;
        set {
            if (Set(ref field, value)) {
                ClearPostEffect();
                if (value is { Length: > 0 } effects)
                    foreach (var effect in EffectAttributes.Parse(effects))
                        AddPostEffect(effect);
            }
        }
    } = string.Empty;

    /// <summary>
    ///     Gets or sets a value indicating whether this instance is throwing shadow.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance is throwing shadow; otherwise, <c>false</c>.
    /// </value>
    public bool IsThrowingShadow {
        get => RenderCore.IsThrowingShadow;
        set => RenderCore.IsThrowingShadow = value;
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [invert normal].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [invert normal]; otherwise, <c>false</c>.
    /// </value>
    public bool InvertNormal {
        get => ((IMeshRenderParams) RenderCore).InvertNormal;
        set => ((IMeshRenderParams) RenderCore).InvertNormal = value;
    }

    /// <summary>
    ///     Gets or sets the color of the wireframe.
    /// </summary>
    /// <value>
    ///     The color of the wireframe.
    /// </value>
    public Color4 WireframeColor {
        get => ((IMeshRenderParams) RenderCore).WireframeColor;
        set => ((IMeshRenderParams) RenderCore).WireframeColor = value;
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [render wireframe].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [render wireframe]; otherwise, <c>false</c>.
    /// </value>
    public bool RenderWireframe {
        get => ((IMeshRenderParams) RenderCore).RenderWireframe;
        set => ((IMeshRenderParams) RenderCore).RenderWireframe = value;
    }

    /// <summary>
    ///     Specifiy if model material is transparent.
    ///     During rendering, transparent objects are rendered after opaque objects. Transparent objects' order in scene graph
    ///     are preserved.
    /// </summary>
    public bool IsTransparent {
        get;
        set {
            if (Set(ref field, value))
                if (RenderType == RenderType.Opaque || RenderType == RenderType.Transparent)
                    RenderType = value
                        ? RenderType.Transparent
                        : RenderType.Opaque;
        }
    }
    private MaterialCore? material;

    /// <summary>
    /// </summary>
    public MaterialCore? Material {
        get => material;
        set {
            if (!Set(ref material, value)) return;
            if (RenderCore is MeshRenderCore core) core.D3D12Material = material;
            InvalidateRender();
        }
    }

    /// <summary>
    /// </summary>
    /// <returns></returns>
    public delegate RasterizerStateDescription CreateRasterStateFunc();

    /// <summary>
    ///     Create raster state description delegate.
    ///     <para>If <see cref="OnCreateRasterState" /> is set, then <see cref="CreateRasterState" /> will not be called.</para>
    /// </summary>
    public CreateRasterStateFunc? OnCreateRasterState;

    protected DefaultStaticMeshBatchingBuffer? BatchingBuffer;

    protected StaticBatchedGeometryBoundsOctree? BatchedGeometryOctree { get; private set; }

    #endregion
}

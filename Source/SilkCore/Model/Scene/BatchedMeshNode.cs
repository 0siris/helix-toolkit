/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core
{
    namespace Model.Scene
    {
        /// <summary>
        ///     Static mesh batching. Supports multiple <see cref="Materials" />. All geometries are merged into single buffer for
        ///     rendering. Indivisual material color infomations are encoded into vertex buffer.
        ///     <para>
        ///         <see cref="Material" /> is used if <see cref="Materials" /> = null. And also used for shared material texture
        ///         binding.
        ///     </para>
        /// </summary>
        public class BatchedMeshNode : SceneNode, IHitable, IThrowingShadow, IBoundable, IApplyPostEffect
        {
            public BatchedMeshNode()
            {
                HasBound = true;
                TransformChanged += BatchedMeshNode_OnTransformChanged;
            }

            private void BatchedMeshNode_OnTransformChanged(object sender, TransformArgs e)
            {
                UpdateBoundsWithTransform();
            }

            protected override RenderCore OnCreateRenderCore()
            {
                return new MeshRenderCore
                {
                    Batched = true
                };
            }

            protected override IRenderTechnique OnCreateRenderTechnique(IEffectsManager effectsManager)
            {
                return effectsManager[DefaultRenderTechniqueNames.MeshBatched];
            }

            /// <summary>
            ///     Called when [raster state changed].
            /// </summary>
            protected virtual void OnRasterStateChanged()
            {
                if (IsAttached && RenderCore is IGeometryRenderCore r)
                    r.RasterDescription = OnCreateRasterState != null ? OnCreateRasterState() : CreateRasterState();
            }

            /// <summary>
            ///     Create raster state description.
            /// </summary>
            /// <returns></returns>
            protected virtual RasterizerStateDescription CreateRasterState()
            {
                return new RasterizerStateDescription
                {
                    FillMode = FillMode,
                    CullMode = CullMode,
                    DepthBias = DepthBias,
                    DepthBiasClamp = -1000,
                    SlopeScaledDepthBias = SlopeScaledDepthBias,
                    IsDepthClipEnabled = IsDepthClipEnabled,
                    IsFrontCounterClockwise = FrontCCW,
                    IsMultisampleEnabled = IsMSAAEnabled,
                    IsScissorEnabled = IsThrowingShadow ? false : IsScissorEnabled
                };
            }

            /// <summary>
            /// </summary>
            protected virtual void AttachMaterial()
            {
                var newVar = material != null && RenderCore is IMaterialRenderParams
                    ? EffectsManager.MaterialVariableManager.Register(material, EffectTechnique)
                    : null;
                RemoveAndDispose(ref materialVariable);
                if (RenderCore is IMaterialRenderParams core) core.MaterialVariables = materialVariable = newVar;
                if (Materials == null && Material is PhongMaterialCore p) batchingBuffer.Materials = new[] {p};
            }

            protected override bool OnAttach(IEffectsManager effectsManager)
            {
                if (base.OnAttach(effectsManager))
                {
                    batchingBuffer = new DefaultStaticMeshBatchingBuffer();
                    batchingBuffer.Geometries = Geometries;
                    batchingBuffer.Materials = materials;
                    if (RenderCore is IGeometryRenderCore r) r.GeometryBuffer = batchingBuffer;
                    AttachMaterial();
                    return true;
                }

                return false;
            }

            /// <summary>
            ///     Called when [attached].
            /// </summary>
            protected override void OnAttached()
            {
                OnRasterStateChanged();
                base.OnAttached();
            }

            /// <summary>
            ///     Used to override Detach
            /// </summary>
            protected override void OnDetach()
            {
                RemoveAndDispose(ref batchingBuffer);
                RemoveAndDispose(ref materialVariable);
                if (RenderCore is IMaterialRenderParams core) core.MaterialVariables = null;
                base.OnDetach();
            }

            protected override OrderKey OnUpdateRenderOrderKey()
            {
                return OrderKey.Create(RenderOrder, materialVariable == null ? (ushort) 0 : materialVariable.ID);
            }

            /// <summary>
            ///     <para>Determine if this can be rendered.</para>
            /// </summary>
            /// <param name="context"></param>
            /// <returns></returns>
            protected override bool CanRender(RenderContext context)
            {
                if (base.CanRender(context) && Geometries != null) return true;

                return false;
            }

            private void UpdateBounds()
            {
                var oldBound = originalBounds;
                var oldBoundSphere = originalBoundsSphere;
                if (geometries != null && geometries.Length > 0)
                {
                    var b = geometries[0].Geometry.Bound;
                    var bs = geometries[0].Geometry.BoundingSphere;
                    foreach (var geo in geometries)
                    {
                        b = BoundingBox.Merge(b, geo.Geometry.Bound.Transform(geo.ModelTransform));
                        bs = BoundingSphereExtensions.Merge(bs,
                            geo.Geometry.BoundingSphere.TransformBoundingSphere(geo.ModelTransform));
                    }

                    originalBounds = b;
                    originalBoundsSphere = bs;
                    BatchedGeometryOctree =
                        new StaticBatchedGeometryBoundsOctree(geometries, new OctreeBuildParameter());
                }
                else
                {
                    originalBounds = MaxBound;
                    originalBoundsSphere = MaxBoundSphere;
                    BatchedGeometryOctree = null;
                }

                RaiseOnBoundChanged(new BoundChangeArgs<BoundingBox>(ref originalBounds, ref oldBound));
                RaiseOnBoundSphereChanged(
                    new BoundChangeArgs<BoundingSphere>(ref originalBoundsSphere, ref oldBoundSphere));
                UpdateBoundsWithTransform();
            }

            private void UpdateBoundsWithTransform()
            {
                var old = boundsWithTransform;
                if (originalBounds == MaxBound)
                    boundsWithTransform = MaxBound;
                else
                    boundsWithTransform = originalBounds.Transform(TotalModelMatrixInternal);
                var oldBS = boundsSphereWithTransform;
                if (originalBoundsSphere == MaxBoundSphere)
                    boundsSphereWithTransform = MaxBoundSphere;
                else
                    boundsSphereWithTransform = originalBoundsSphere.TransformBoundingSphere(TotalModelMatrixInternal);
                RaiseOnTransformBoundChanged(new BoundChangeArgs<BoundingBox>(ref boundsWithTransform, ref old));
                RaiseOnTransformBoundSphereChanged(
                    new BoundChangeArgs<BoundingSphere>(ref boundsSphereWithTransform, ref oldBS));
            }

            /// <summary>
            ///     Views the frustum test.
            /// </summary>
            /// <param name="viewFrustum">The view frustum.</param>
            /// <returns></returns>
            public override bool TestViewFrustum(ref BoundingFrustum viewFrustum)
            {
                if (!EnableViewFrustumCheck) return true;
                return BoundingFrustumExtensions.IsInOrIntersectFrustum(ref viewFrustum, ref boundsWithTransform,
                    ref boundsSphereWithTransform);
            }

            /// <summary>
            ///     Determines whether this instance [can hit test] the specified context.
            /// </summary>
            /// <param name="context">The context.</param>
            /// <returns>
            ///     <c>true</c> if this instance [can hit test] the specified context; otherwise, <c>false</c>.
            /// </returns>
            protected override bool CanHitTest(HitTestContext context)
            {
                return base.CanHitTest(context) && Geometries != null && Geometries.Length > 0 && Materials != null &&
                       Materials.Length > 0;
            }

            /// <summary>
            ///     Updates the not render.
            /// </summary>
            /// <param name="context">The context.</param>
            public override void UpdateNotRender(RenderContext context)
            {
                base.UpdateNotRender(context);
                if (IsHitTestVisible && context.AutoUpdateOctree && Geometries != null)
                    foreach (var geometry in Geometries)
                        geometry.Geometry?.UpdateOctree();

                if (BatchedGeometryOctree != null && !BatchedGeometryOctree.TreeBuilt)
                    BatchedGeometryOctree.BuildTree();
            }

            protected override bool OnHitTest(HitTestContext context, Matrix totalModelMatrix,
                ref List<HitTestResult> hits)
            {
                var rayWS = context.RayWS;
                if (rayWS.Intersects(boundsWithTransform) && rayWS.Intersects(boundsSphereWithTransform))
                {
                    if (BatchedGeometryOctree != null && BatchedGeometryOctree.TreeBuilt)
                        return BatchedGeometryOctree.HitTest(context, WrapperSource, null, totalModelMatrix, ref hits);

                    var isHit = false;
                    for (var i = 0; i < Geometries.Length; ++i)
                    {
                        var currCount = hits.Count;
                        ref var geo = ref Geometries[i];
                        if (geo.Geometry is MeshGeometry3D mesh)
                        {
                            var hasHit = mesh.HitTest(context, geo.ModelTransform * totalModelMatrix, ref hits,
                                WrapperSource);
                            if (hasHit && currCount < hits.Count)
                            {
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

            private BatchedMeshGeometryConfig[] geometries;

            public BatchedMeshGeometryConfig[] Geometries
            {
                get => geometries;
                set
                {
                    if (SetAffectsRender(ref geometries, value))
                    {
                        if (IsAttached) batchingBuffer.Geometries = value;
                        UpdateBounds();
                    }
                }
            }

            private PhongMaterialCore[] materials;

            public PhongMaterialCore[] Materials
            {
                get => materials;
                set
                {
                    if (SetAffectsRender(ref materials, value) && IsAttached)
                    {
                        batchingBuffer.Materials = value;
                        if (value == null && Material is PhongMaterialCore p) batchingBuffer.Materials = new[] {p};
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

            private int depthBias;

            /// <summary>
            ///     Gets or sets the depth bias.
            /// </summary>
            /// <value>
            ///     The depth bias.
            /// </value>
            public int DepthBias
            {
                get => depthBias;
                set
                {
                    if (Set(ref depthBias, value)) OnRasterStateChanged();
                }
            }

            private float slopScaledDepthBias;

            /// <summary>
            ///     Gets or sets the slope scaled depth bias.
            /// </summary>
            /// <value>
            ///     The slope scaled depth bias.
            /// </value>
            public float SlopeScaledDepthBias
            {
                get => slopScaledDepthBias;
                set
                {
                    if (Set(ref slopScaledDepthBias, value)) OnRasterStateChanged();
                }
            }

            private bool isMSAAEnabled = true;

            /// <summary>
            ///     Gets or sets a value indicating whether Multisampling Anti-Aliasing enabled.
            /// </summary>
            /// <value>
            ///     <c>true</c> if this instance is msaa enabled; otherwise, <c>false</c>.
            /// </value>
            public bool IsMSAAEnabled
            {
                get { return isMSAAEnabled = true; }
                set
                {
                    if (Set(ref isMSAAEnabled, value)) OnRasterStateChanged();
                }
            }

            private bool isScissorEnabled = true;

            /// <summary>
            ///     Gets or sets a value indicating whether this instance is scissor enabled.
            /// </summary>
            /// <value>
            ///     <c>true</c> if this instance is scissor enabled; otherwise, <c>false</c>.
            /// </value>
            public bool IsScissorEnabled
            {
                get => isScissorEnabled;
                set
                {
                    if (Set(ref isScissorEnabled, value)) OnRasterStateChanged();
                }
            }

            private FillMode fillMode = FillMode.Solid;

            /// <summary>
            ///     Gets or sets the fill mode.
            /// </summary>
            /// <value>
            ///     The fill mode.
            /// </value>
            public FillMode FillMode
            {
                get => fillMode;
                set
                {
                    if (Set(ref fillMode, value)) OnRasterStateChanged();
                }
            }

            private bool isDepthClipEnabled = true;

            /// <summary>
            ///     Gets or sets a value indicating whether this instance is depth clip enabled.
            /// </summary>
            /// <value>
            ///     <c>true</c> if this instance is depth clip enabled; otherwise, <c>false</c>.
            /// </value>
            public bool IsDepthClipEnabled
            {
                get => isDepthClipEnabled;
                set
                {
                    if (Set(ref isDepthClipEnabled, value)) OnRasterStateChanged();
                }
            }

            private bool frontCCW = true;

            /// <summary>
            ///     Gets or sets a value indicating whether [front CCW].
            /// </summary>
            /// <value>
            ///     <c>true</c> if [front CCW]; otherwise, <c>false</c>.
            /// </value>
            public bool FrontCCW
            {
                get => frontCCW;
                set
                {
                    if (Set(ref frontCCW, value)) OnRasterStateChanged();
                }
            }

            private CullMode cullMode = CullMode.None;

            /// <summary>
            ///     Gets or sets the cull mode.
            /// </summary>
            /// <value>
            ///     The cull mode.
            /// </value>
            public CullMode CullMode
            {
                get => cullMode;
                set
                {
                    if (Set(ref cullMode, value)) OnRasterStateChanged();
                }
            }

            #endregion Rasterizer parameters

            private bool enableViewFrustumCheck = true;

            /// <summary>
            ///     Gets or sets a value indicating whether [enable view frustum check].
            /// </summary>
            /// <value>
            ///     <c>true</c> if [enable view frustum check]; otherwise, <c>false</c>.
            /// </value>
            public bool EnableViewFrustumCheck
            {
                get => enableViewFrustumCheck && HasBound;
                set => Set(ref enableViewFrustumCheck, value);
            }

            private string postEffects;

            /// <summary>
            ///     Gets or sets the post effects.
            /// </summary>
            /// <value>
            ///     The post effects.
            /// </value>
            public string PostEffects
            {
                get => postEffects;
                set
                {
                    if (Set(ref postEffects, value))
                    {
                        ClearPostEffect();
                        if (value is string effects)
                            if (!string.IsNullOrEmpty(effects))
                                foreach (var effect in EffectAttributes.Parse(effects))
                                    AddPostEffect(effect);
                    }
                }
            }

            /// <summary>
            ///     Gets or sets a value indicating whether this instance is throwing shadow.
            /// </summary>
            /// <value>
            ///     <c>true</c> if this instance is throwing shadow; otherwise, <c>false</c>.
            /// </value>
            public bool IsThrowingShadow
            {
                get => RenderCore.IsThrowingShadow;
                set => RenderCore.IsThrowingShadow = value;
            }

            /// <summary>
            ///     Gets or sets a value indicating whether [invert normal].
            /// </summary>
            /// <value>
            ///     <c>true</c> if [invert normal]; otherwise, <c>false</c>.
            /// </value>
            public bool InvertNormal
            {
                get => (RenderCore as IMeshRenderParams).InvertNormal;
                set => (RenderCore as IMeshRenderParams).InvertNormal = value;
            }

            /// <summary>
            ///     Gets or sets the color of the wireframe.
            /// </summary>
            /// <value>
            ///     The color of the wireframe.
            /// </value>
            public Color4 WireframeColor
            {
                get => (RenderCore as IMeshRenderParams).WireframeColor;
                set => (RenderCore as IMeshRenderParams).WireframeColor = value;
            }

            /// <summary>
            ///     Gets or sets a value indicating whether [render wireframe].
            /// </summary>
            /// <value>
            ///     <c>true</c> if [render wireframe]; otherwise, <c>false</c>.
            /// </value>
            public bool RenderWireframe
            {
                get => (RenderCore as IMeshRenderParams).RenderWireframe;
                set => (RenderCore as IMeshRenderParams).RenderWireframe = value;
            }

            private bool isTransparent;

            /// <summary>
            ///     Specifiy if model material is transparent.
            ///     During rendering, transparent objects are rendered after opaque objects. Transparent objects' order in scene graph
            ///     are preserved.
            /// </summary>
            public bool IsTransparent
            {
                get => isTransparent;
                set
                {
                    if (Set(ref isTransparent, value))
                        if (RenderType == RenderType.Opaque || RenderType == RenderType.Transparent)
                            RenderType = value ? RenderType.Transparent : RenderType.Opaque;
                }
            }

            private MaterialVariable materialVariable;
            private MaterialCore material;

            /// <summary>
            /// </summary>
            public MaterialCore Material
            {
                get => material;
                set
                {
                    if (Set(ref material, value))
                        if (EffectsManager != null)
                        {
                            if (IsAttached)
                            {
                                AttachMaterial();
                                InvalidateRender();
                            }
                            else
                            {
                                var effectsMgr = EffectsManager;
                                Detach();
                                Attach(effectsMgr);
                            }
                        }
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
            public CreateRasterStateFunc OnCreateRasterState;

            protected DefaultStaticMeshBatchingBuffer batchingBuffer;

            protected StaticBatchedGeometryBoundsOctree BatchedGeometryOctree { get; private set; }

            #endregion
        }
    }
}
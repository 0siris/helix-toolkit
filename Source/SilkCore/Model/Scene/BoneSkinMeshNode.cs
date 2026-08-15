/*
The MIT License(MIT)
Copyright(c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Core.Buffers;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Animations;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Render.DeviceContextProxy;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Model.Scene;
/// <summary>
/// </summary>
public class BoneSkinMeshNode : MeshNode, IBoneMatricesNode {
    private Vector3[] skinnedVerticesCache = [];

    private BoneSkinRenderCore BoneCore
        => RenderCore as BoneSkinRenderCore
           ?? throw new InvalidOperationException("Bone skin render core is required.");

    /// <summary>
    ///     Gets or sets a value indicating whether this node is used to show skeleton. Only used as an indication.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this node is used to show skeleton; otherwise, <c>false</c>.
    /// </value>
    public bool IsSkeletonNode { get; set; }

    /// <summary>
    ///     Gets or sets the bone matrices.
    /// </summary>
    /// <value>
    ///     The bone matrices.
    /// </value>
    public Matrix[] BoneMatrices {
        get => BoneCore.BoneMatrices;
        set => BoneCore.BoneMatrices = value;
    }

    public float[] MorphTargetWeights {
        get => BoneCore.MorphTargetWeights;
        set => BoneCore.MorphTargetWeights = value;
    }

    /// <summary>
    ///     Gets or sets the bones.
    /// </summary>
    /// <value>
    ///     The bones.
    /// </value>
    public Bone[]? Bones { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether this node has bone group.
    ///     <see cref="BoneGroupNode" /> shares bones with multiple <see cref="BoneSkinMeshNode" />
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance has bone group; otherwise, <c>false</c>.
    /// </value>
    public bool HasBoneGroup { get; internal set; }

    /// <summary>
    ///     Called when [create render core].
    /// </summary>
    /// <returns></returns>
    protected override RenderCore OnCreateRenderCore() => new BoneSkinRenderCore();

    protected override IAttachableBufferModel OnCreateBufferModel(Guid modelGuid, Geometry3D? geometry) {
        var effectsManager = EffectsManager
            ?? throw new InvalidOperationException("Effects manager is required to create a bone skin buffer.");
        return effectsManager.GeometryBufferManager.Register<BoneSkinnedMeshBufferModel>(modelGuid, geometry)
            is not IBoneSkinMeshBufferModel buffer
                ? EmptyGeometryBufferModel.Empty
                : new BoneSkinPreComputeBufferModel(buffer, buffer.VertexStructSize.FirstOrDefault());
    }

    public override bool TestViewFrustum(ref BoundingFrustum viewFrustum) => BoneMatrices.Length != 0 || base.TestViewFrustum(ref viewFrustum);

    protected override bool PreHitTestOnBounds(HitTestContext context) => BoneMatrices.Length != 0 || base.PreHitTestOnBounds(context);

    /// <summary>
    ///     Creates the skeleton node.
    /// </summary>
    /// <param name="material">The material.</param>
    /// <param name="effectName">Name of the effect.</param>
    /// <param name="scale">The scale.</param>
    /// <returns></returns>
    public BoneSkinMeshNode CreateSkeletonNode(MaterialCore material, string effectName, float scale = 0.1f) => CreateSkeletonNode(this, material, effectName, scale);

    /// <summary>
    ///     Creates the skeleton node.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <param name="material">The material.</param>
    /// <param name="effectName">Name of the effect.</param>
    /// <param name="scale">The scale.</param>
    /// <returns></returns>
    public static BoneSkinMeshNode CreateSkeletonNode(
        BoneSkinMeshNode node,
        MaterialCore material,
        string effectName,
        float scale
    ) {
        var bones = node.Bones
            ?? throw new InvalidOperationException("Bones are required to create a skeleton node.");
        var skNode = new BoneSkinMeshNode {
            Material = material,
            IsSkeletonNode = true,
            Geometry = BoneSkinnedMeshGeometry3D.CreateSkeletonMesh(bones, scale),
            PostEffects = effectName,
            Bones = bones
        };
        return skNode;
    }

    /// <summary>
    ///     Try to get skinned vertices. Skinned vertices are copied from GPU.
    /// </summary>
    /// <param name="manager"></param>
    /// <returns>New array with vertex positions</returns>
    public Vector3[]? TryGetSkinnedVertices(IEffectsManager manager) {
        if (Geometry is BoneSkinnedMeshGeometry3D skGeometry)
            if (RenderCore is BoneSkinRenderCore skCore) {
                var nativeResources = manager.NativeDeviceResources;
                var proxy = new DeviceContextProxy(nativeResources.ImmediateContext, nativeResources.Device);
                if (skGeometry.Positions is not { } positions) return null;
                var array = new Vector3[positions.Count];
                if (skCore.CopySkinnedToArray(proxy, array) > 0) return array;
            }

        return null;
    }

    /// <summary>
    ///     Try to get skinned vertices. Skinned vertices are copied from GPU.
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="array">Vertex positions will be copied into this array</param>
    /// <returns></returns>
    public int TryGetSkinnedVertices(IEffectsManager manager, Vector3[] array) {
        if (Geometry is BoneSkinnedMeshGeometry3D)
            if (RenderCore is BoneSkinRenderCore skCore) {
                var nativeResources = manager.NativeDeviceResources;
                var proxy = new DeviceContextProxy(nativeResources.ImmediateContext, nativeResources.Device);
                return skCore.CopySkinnedToArray(proxy, array);
            }

        return 0;
    }

    /// <summary>
    ///     Get the skinned vertices cache used for hit test.
    ///     This cache will only be updated after each hit test.
    ///     To get latest skinned vertices, please use <see cref="TryGetSkinnedVertices(IEffectsManager)" />.
    /// </summary>
    /// <returns></returns>
    public Vector3[] TryGetSkinnedVerticesCache() => skinnedVerticesCache;

    /// <summary>
    ///     Make sure to use SetWeight so that the mutation of elements can be seen
    /// </summary>
    /// <param name="i">index</param>
    /// <param name="w">weight, typically 0-1</param>
    public void SetWeight(int i, float w) {
        BoneCore.SetWeight(i, w);
    }

    /// <summary>
    ///     Tells the render core to update it's morph target weight buffer
    /// </summary>
    public void WeightUpdated() {
        BoneCore.InvalidateMorphTargetWeights();
        InvalidateRender();
    }

    public void SetupIdentitySkeleton() {
        BoneMatrices = [Matrix.Identity];
        Bones = [
            new Bone {
                Name = "Identity", BindPose = Matrix.Identity, InvBindPose = Matrix.Identity,
                BoneLocalTransform = Matrix.Identity
            }
        ];

        if (Geometry is not BoneSkinnedMeshGeometry3D geom || geom.Positions is not { } positions)
            return;

        var vertexBoneIds = new BoneIds[positions.Count];
        geom.VertexBoneIds = vertexBoneIds;
        for (var i = 0; i < vertexBoneIds.Length; i++)
            vertexBoneIds[i] = new BoneIds { Bone1 = 0, Weights = new Vector4(1, 0, 0, 0) };
    }

    public void UpdateBoneMatrices() {
        if (Bones is not { } bones) return;

        BoneMatrices = new Matrix[bones.Length];
        BoneMatrices = [.. BoneMatrices.Select((_, i) => bones[i].Node?.TotalModelMatrixInternal ?? Matrix.Identity)];
    }

    public void InvalidateBoneMatrices() {
        BoneCore.InvalidateBoneMatrices();
    }

    public void InvalidateMorphTargetWeights() {
        BoneCore.InvalidateMorphTargetWeights();
    }

    public bool InitializeMorphTargets(MorphTargetVertex[] mtv, int pitch) => BoneCore.InitializeMorphTargets(mtv, pitch);

    protected override bool OnHitTest(
        HitTestContext context,
        Matrix totalModelMatrix,
        ref List<HitTestResult> hits
    ) {
        if (BoneMatrices.Length > 0 && Geometry is BoneSkinnedMeshGeometry3D skGeometry
            && skGeometry.Positions is { } positions)
            if (RenderCore is BoneSkinRenderCore skCore) {
                if (skinnedVerticesCache.Length < positions.Count)
                    skinnedVerticesCache = new Vector3[positions.Count];
                if (context.RenderMatrices.RenderHost.ImmediateDeviceContext is { } immediateDeviceContext
                    && skCore.CopySkinnedToArray(immediateDeviceContext, skinnedVerticesCache) > 0)
                    return skGeometry.HitTestWithSkinnedVertices(context,
                                                                 skinnedVerticesCache,
                                                                 totalModelMatrix,
                                                                 ref hits,
                                                                 WrapperSource ?? this);
            }

        return base.OnHitTest(context, totalModelMatrix, ref hits);
    }
}

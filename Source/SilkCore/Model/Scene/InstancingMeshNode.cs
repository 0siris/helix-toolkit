/*
The MIT License(MIT)
Copyright(c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Core;

namespace HelixToolkit.SharpDX.Core.Model.Scene;
/// <summary>
/// </summary>
public class InstancingMeshNode : MeshNode {
    /// <summary>
    ///     The instance parameter buffer
    /// </summary>
    protected IElementsBufferModel<InstanceParameter> InstanceParamBuffer =
        new InstanceParamsBufferModel<InstanceParameter>(InstanceParameter.SizeInBytes);

    private bool isInstanceChanged;

    protected override IRenderTechnique OnCreateRenderTechnique(IEffectsManager effectsManager) {
        return effectsManager[DefaultRenderTechniqueNames.InstancingMesh];
    }

    protected override RenderCore OnCreateRenderCore() {
        return new InstancingMeshRenderCore { ParameterBuffer = InstanceParamBuffer };
    }

    protected override bool OnAttach(IEffectsManager effectsManager) {
        if (base.OnAttach(effectsManager)) {
            InstanceParamBuffer.Initialize();

            return true;
        }

        return false;
    }

    /// <summary>
    ///     Called when [detach].
    /// </summary>
    protected override void OnDetach() {
        InstanceParamBuffer.DisposeAndClear();
        base.OnDetach();
    }

    /// <summary>
    ///     Updates the not render.
    /// </summary>
    /// <param name="context">The context.</param>
    public override void UpdateNotRender(RenderContext context) {
        base.UpdateNotRender(context);
        if (isInstanceChanged) {
            BuildOctree();
            isInstanceChanged = false;
        }
    }

    /// <summary>
    ///     Instanceses the changed.
    /// </summary>
    protected override void InstancesChanged() {
        base.InstancesChanged();
        octreeManager?.Clear();
        isInstanceChanged = true;
    }

    /// <summary>
    ///     Builds the octree.
    /// </summary>
    private void BuildOctree() {
        if (IsRenderable && InstanceBuffer.HasElements)
            octreeManager?.RebuildTree(Enumerable.Repeat<SceneNode>(this, 1));
        else
            octreeManager?.Clear();
    }

    public override bool HitTest(HitTestContext context, ref List<HitTestResult> hits) {
        var isHit = false;
        if (CanHitTest(context) && PreHitTestOnBounds(context)) {
            if (octreeManager is {Octree: not null}) {
                var boundHits = new List<HitTestResult>();
                isHit = octreeManager.Octree.HitTest(context,
                                                     WrapperSource,
                                                     Geometry,
                                                     TotalModelMatrixInternal,
                                                     ref boundHits);
                if (isHit) {
                    isHit = false;
                    Matrix instanceMatrix;
                    foreach (var hit in boundHits) {
                        var instanceIdx = (int)hit.Tag;
                        instanceMatrix = InstanceBuffer.Elements[instanceIdx];
                        var h = base.OnHitTest(context, TotalModelMatrixInternal * instanceMatrix, ref hits);
                        isHit |= h;
                        if (h && hits.Count > 0) {
                            var result = hits.Last();
                            object? tag = null;
                            if (InstanceIdentifiers != null &&
                                InstanceIdentifiers.Count == InstanceBuffer.Elements.Count)
                                tag = InstanceIdentifiers[instanceIdx];
                            else
                                tag = instanceIdx;
                            result.Tag = tag;
                            hits[hits.Count - 1] = result;
                        }
                    }
                }
            } else {
                isHit = base.HitTest(context, ref hits);
            }
        }

        return isHit;
    }

    #region Properties

    /// <summary>
    ///     Gets or sets the instance identifiers.
    /// </summary>
    /// <value>
    ///     The instance identifiers.
    /// </value>
    public IList<Guid> InstanceIdentifiers {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Gets or sets the instance parameter array.
    /// </summary>
    /// <value>
    ///     The instance parameter array.
    /// </value>
    public IList<InstanceParameter> InstanceParamArray {
        get => InstanceParamBuffer.Elements;
        set => InstanceParamBuffer.Elements = value;
    }

    private IOctreeManager octreeManager;

    /// <summary>
    ///     Gets or sets the octree manager.
    /// </summary>
    /// <value>
    ///     The octree manager.
    /// </value>
    public IOctreeManager OctreeManager {
        get => octreeManager;
        set {
            if (Set(ref octreeManager, value))
                if (octreeManager != null) {
                    octreeManager.Clear();
                    isInstanceChanged = true;
                }
        }
    }

    #endregion
}

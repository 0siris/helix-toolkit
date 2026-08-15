using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Utilities.Octrees.StaticOctrees;

namespace HelixToolkit.SharpDX.Core.Utilities.Octrees;

/// <summary>
/// </summary>
public sealed class InstancingRenderableOctreeManager : OctreeManagerBase {
    /// <summary>
    ///     Adds the pending item.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public override bool AddPendingItem(SceneNode? item) => false;

    /// <summary>
    ///     Clears this instance.
    /// </summary>
    public override void Clear() => Octree = null;

    /// <summary>
    ///     Processes the pending items.
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public override void ProcessPendingItems() { }

    /// <summary>
    ///     Rebuilds the tree.
    /// </summary>
    /// <param name="items">The items.</param>
    public override void RebuildTree(IEnumerable<SceneNode>? items) {
        Clear();
        if (items?.FirstOrDefault() is not { } sceneNode
            || sceneNode is not IInstancing inst
            || inst.InstanceBuffer.Elements is not { Count: > 0 } instanceMatrix)
            return;

        var octree = new StaticInstancingModelOctree(instanceMatrix, sceneNode.OriginalBounds, Parameter);
        //new InstancingModel3DOctree(instMatrix, (inst as SceneNode).OriginalBounds, this.Parameter, new Stack<KeyValuePair<int, IOctree[]>>(10));
        octree.BuildTree();
        Octree = octree;
    }

    /// <summary>
    ///     Removes the item.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <exception cref="NotImplementedException"></exception>
    public override void RemoveItem(SceneNode? item) { }

    /// <summary>
    ///     Requests the rebuild.
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public override void RequestRebuild() { }
}

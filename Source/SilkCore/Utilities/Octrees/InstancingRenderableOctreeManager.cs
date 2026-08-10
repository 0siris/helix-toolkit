using HelixToolkit.SharpDX.Core.Model.Scene;

namespace HelixToolkit.SharpDX.Core.Utilities;

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
        if (items?.FirstOrDefault() is not IInstancing inst)
            return;
        
        var instMatrix = inst.InstanceBuffer.Elements;
        var octree = new StaticInstancingModelOctree(instMatrix, (inst as SceneNode).OriginalBounds, Parameter);
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

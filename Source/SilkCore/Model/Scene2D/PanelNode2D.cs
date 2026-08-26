/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Scene2D.Abstract;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Model.Scene2D;
public class PanelNode2D : SceneNode2D {
    protected readonly Dictionary<Guid, SceneNode2D> ItemHashSet = [];

    public PanelNode2D() {
        ItemsInternal = [];
        Items = new ReadOnlyObservableFastList<SceneNode2D>(ItemsInternal);
    }

    public virtual bool AddChildNode(SceneNode2D node) {
        if (!ItemHashSet.ContainsKey(node.Guid)) {
            ItemHashSet.Add(node.Guid, node);
            ItemsInternal.Add(node);
            node.Parent = this;
            if (IsAttached) node.Attach(DpiScale);
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Clears this instance.
    /// </summary>
    public virtual void Clear() {
        for (var i = 0; i < ItemsInternal.Count; ++i) {
            ItemsInternal[i].Detach();
            ItemsInternal[i].Parent = null;
        }

        ItemHashSet.Clear();
        ItemsInternal.Clear();
    }

    /// <summary>
    ///     Removes the child node.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <returns></returns>
    public virtual bool RemoveChildNode(SceneNode2D node) {
        if (ItemHashSet.Remove(node.Guid)) {
            node.Detach();
            ItemsInternal.Remove(node);
            node.Parent = null;
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Tries the get node.
    /// </summary>
    /// <param name="guid">The unique identifier.</param>
    /// <param name="node">The node.</param>
    /// <returns></returns>
    public bool TryGetNode(Guid guid, out SceneNode2D? node) => ItemHashSet.TryGetValue(guid, out node);

    protected override bool OnAttach() {
        for (var i = 0; i < ItemsInternal.Count; ++i) ItemsInternal[i].Attach(DpiScale);
        return true;
    }

    protected override void OnDetach() {
        for (var i = 0; i < ItemsInternal.Count; ++i) ItemsInternal[i].Detach();
        base.OnDetach();
    }

protected override bool OnHitTest(ref Vector2 mousePoint, out HitTest2DResult? hitResult) {
        hitResult = null;
        if (!LayoutBoundWithTransform.Contains(mousePoint)) return false;
        foreach (var item in ItemsInternal.Reverse())
            if (item.HitTest(mousePoint, out hitResult))
                return true;

        return false;
    }
}

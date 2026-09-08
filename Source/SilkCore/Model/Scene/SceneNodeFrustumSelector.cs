/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Model.Collection;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;

namespace HelixToolkit.SharpDX.Core.Model.Scene;

/// <summary>
///     Applies the shared camera-frustum contract to renderable scene-node lists.
/// </summary>
internal static class SceneNodeFrustumSelector {
    /// <summary>
    ///     Appends visible nodes and updates each candidate's current frustum state.
    /// </summary>
    /// <param name="candidates">The renderable candidates.</param>
    /// <param name="visible">The destination list.</param>
    /// <param name="testFrustum">Whether camera-frustum testing is enabled.</param>
    /// <param name="frustum">The current camera frustum.</param>
    internal static void AppendVisible(
        FastList<SceneNode> candidates,
        FastList<SceneNode> visible,
        bool testFrustum,
        ref BoundingFrustum frustum
    ) {
        candidates.AsGuardNotNull();
        visible.AsGuardNotNull();
        for (var index = 0; index < candidates.Count; index++) {
            var node = candidates.Items[index];
            node.IsInFrustum = !testFrustum || node.TestViewFrustum(ref frustum);
            if (node.IsInFrustum) visible.Add(node);
        }
    }
}

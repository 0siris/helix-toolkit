/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Model.Animations;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;

namespace HelixToolkit.SharpDX.Core.Assimp;

/// <summary>
///     Scene for importer output
/// </summary>
public class HelixToolkitScene {
    /// <summary>
    ///     Initializes a new instance of the <see cref="HelixToolkitScene" /> class.
    /// </summary>
    /// <param name="root">The root.</param>
    public HelixToolkitScene(SceneNode root) {
        Root = root;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="HelixToolkitScene" /> class.
    /// </summary>
    /// <param name="root">The root.</param>
    /// <param name="animations">The animations.</param>
    public HelixToolkitScene(SceneNode root, IList<Animation>? animations = null) {
        Root = root;
        Animations = [.. animations ?? []];
    }

    /// <summary>
    ///     Gets or sets the root.
    /// </summary>
    /// <value>
    ///     The root.
    /// </value>
    public SceneNode Root { get; set; }

    /// <summary>
    ///     Gets or sets the animations.
    /// </summary>
    /// <value>
    ///     The animations.
    /// </value>
    public IList<Animation> Animations { get; set; } = [];

    /// <summary>
    ///     Gets a value indicating whether this instance has animation.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance has animation; otherwise, <c>false</c>.
    /// </value>
    public bool HasAnimation => Animations.Count > 0;
}

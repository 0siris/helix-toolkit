/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Collection;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Model.Scene.Lights;
using HelixToolkit.SharpDX.Core.Model.Scene2D.Abstract;

namespace HelixToolkit.SharpDX.Core.Render.RenderHost;
public partial class DefaultRenderHost {
    #region Per frame render list

    protected readonly FastList<SceneNode> ViewportRenderables = [];

    /// <summary>
    ///     The pending renderables
    /// </summary>
    protected readonly FastList<(int Key, SceneNode Value)> PerFrameFlattenedSceneInternal = [];

    /// <summary>
    ///     The light renderables
    /// </summary>
    protected readonly FastList<SceneNode> LightNodes = [];

    /// <summary>
    ///     The pending render nodes
    /// </summary>
    protected readonly FastList<SceneNode> OpaqueNodes = [];

    /// <summary>
    ///     The opaque nodes in frustum
    /// </summary>
    protected readonly FastList<SceneNode> OpaqueNodesInFrustum = [];

    /// <summary>
    ///     The transparent nodes
    /// </summary>
    protected readonly FastList<SceneNode> TransparentNodes = [];

    /// <summary>
    ///     The transparent nodes in frustum
    /// </summary>
    protected readonly FastList<SceneNode> TransparentNodesInFrustum = [];

    /// <summary>
    ///     The particle nodes
    /// </summary>
    protected readonly FastList<SceneNode> ParticleNodes = [];

    /// <summary>
    ///     The pending render nodes
    /// </summary>
    protected readonly FastList<SceneNode> PreProcNodes = [];

    /// <summary>
    ///     The post effect nodes
    /// </summary>
    protected readonly FastList<SceneNode> PostEffectNodes = [];

    /// <summary>
    ///     The global effect nodes
    /// </summary>
    protected readonly FastList<SceneNode> GlobalEffectNodes = [];

    /// <summary>
    ///     The nodes have post effect
    /// </summary>
    protected readonly FastList<SceneNode> NodesWithPostEffect = [];

    /// <summary>
    ///     The pending render nodes
    /// </summary>
    protected readonly FastList<SceneNode> ScreenSpacedNodes = [];

    /// <summary>
    ///     The viewport renderable2D
    /// </summary>
    protected readonly FastList<SceneNode2D> ViewportRenderable2D = [];

    /// <summary>
    ///     The need update cores
    /// </summary>
    private readonly FastList<RenderCore> needUpdateCores = [];

    /// <summary>
    ///     Gets the current frame flattened scene graph. KeyValuePair.Key is the depth of the node.
    /// </summary>
    /// <value>
    ///     Gets the current frame flattened scene graph
    /// </value>
    public sealed override FastList<(int Key, SceneNode Value)> PerFrameFlattenedScene =>
        PerFrameFlattenedSceneInternal;

    /// <summary>
    ///     Gets the per frame lights.
    /// </summary>
    /// <value>
    ///     The per frame lights.
    /// </value>
    public sealed override IEnumerable<LightNode> PerFrameLights => LightNodes.OfType<LightNode>();

    /// <summary>
    ///     Gets the per frame nodes for opaque rendering. <see cref="RenderType.Opaque" />
    ///     <para>
    ///         This does not include <see cref="RenderType.Transparent" />, <see cref="RenderType.Particle" />,
    ///         <see cref="RenderType.PreProc" />, <see cref="RenderType.PostEffect" />, <see cref="RenderType.GlobalEffect" />
    ///         , <see cref="RenderType.Light" />,
    ///         <see cref="RenderType.ScreenSpaced" />
    ///     </para>
    /// </summary>
    public sealed override FastList<SceneNode> PerFrameOpaqueNodes => OpaqueNodes;

    /// <summary>
    ///     Gets the per frame opaque nodes in frustum.
    /// </summary>
    /// <value>
    ///     The per frame opaque nodes in frustum.
    /// </value>
    public sealed override FastList<SceneNode> PerFrameOpaqueNodesInFrustum => OpaqueNodesInFrustum;

    /// <summary>
    ///     Gets the per frame transparent nodes in frustum.
    /// </summary>
    /// <value>
    ///     The per frame transparent nodes in frustum.
    /// </value>
    public sealed override FastList<SceneNode> PerFrameTransparentNodesInFrustum => TransparentNodesInFrustum;

    /// <summary>
    ///     Gets the per frame transparent nodes. , <see cref="RenderType.Transparent" />, <see cref="RenderType.Particle" />
    ///     <para>
    ///         This does not include <see cref="RenderType.Opaque" />, <see cref="RenderType.PreProc" />,
    ///         <see cref="RenderType.PostEffect" />, <see cref="RenderType.GlobalEffect" />, <see cref="RenderType.Light" />,
    ///         <see cref="RenderType.ScreenSpaced" />
    ///     </para>
    /// </summary>
    /// <value>
    ///     The per frame transparent nodes.
    /// </value>
    public sealed override FastList<SceneNode> PerFrameTransparentNodes => TransparentNodes;

    /// <summary>
    ///     Gets the per frame transparent nodes.
    /// </summary>
    /// <value>
    ///     The per frame transparent nodes.
    /// </value>
    public sealed override FastList<SceneNode> PerFrameParticleNodes => ParticleNodes;

    /// <summary>
    ///     Gets the per frame post effects cores. It is the subset of <see cref="PerFrameOpaqueNodes" />
    /// </summary>
    /// <value>
    ///     The per frame post effects cores.
    /// </value>
    public sealed override FastList<SceneNode> PerFrameNodesWithPostEffect => NodesWithPostEffect;

    #endregion
}

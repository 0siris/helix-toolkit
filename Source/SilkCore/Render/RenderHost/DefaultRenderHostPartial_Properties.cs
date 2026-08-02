/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model.Scene2D;


namespace HelixToolkit.SharpDX.Core {
    namespace Render {
        public partial class DefaultRenderHost {
            #region Per frame render list

            protected readonly FastList<SceneNode> viewportRenderables = [];

            /// <summary>
            ///     The pending renderables
            /// </summary>
            protected readonly FastList<KeyValuePair<int, SceneNode>> perFrameFlattenedScene = [];

            /// <summary>
            ///     The light renderables
            /// </summary>
            protected readonly FastList<SceneNode> lightNodes = [];

            /// <summary>
            ///     The pending render nodes
            /// </summary>
            protected readonly FastList<SceneNode> opaqueNodes = [];

            /// <summary>
            ///     The opaque nodes in frustum
            /// </summary>
            protected readonly FastList<SceneNode> opaqueNodesInFrustum = [];

            /// <summary>
            ///     The transparent nodes
            /// </summary>
            protected readonly FastList<SceneNode> transparentNodes = [];

            /// <summary>
            ///     The transparent nodes in frustum
            /// </summary>
            protected readonly FastList<SceneNode> transparentNodesInFrustum = [];

            /// <summary>
            ///     The particle nodes
            /// </summary>
            protected readonly FastList<SceneNode> particleNodes = [];

            /// <summary>
            ///     The pending render nodes
            /// </summary>
            protected readonly FastList<SceneNode> preProcNodes = [];

            /// <summary>
            ///     The post effect nodes
            /// </summary>
            protected readonly FastList<SceneNode> postEffectNodes = [];

            /// <summary>
            ///     The global effect nodes
            /// </summary>
            protected readonly FastList<SceneNode> globalEffectNodes = [];

            /// <summary>
            ///     The nodes have post effect
            /// </summary>
            protected readonly FastList<SceneNode> nodesWithPostEffect = [];

            /// <summary>
            ///     The pending render nodes
            /// </summary>
            protected readonly FastList<SceneNode> screenSpacedNodes = [];

            /// <summary>
            ///     The viewport renderable2D
            /// </summary>
            protected readonly FastList<SceneNode2D> viewportRenderable2D = [];

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
            public sealed override FastList<KeyValuePair<int, SceneNode>> PerFrameFlattenedScene =>
                perFrameFlattenedScene;

            /// <summary>
            ///     Gets the per frame lights.
            /// </summary>
            /// <value>
            ///     The per frame lights.
            /// </value>
            public sealed override IEnumerable<LightNode> PerFrameLights {
                get { return lightNodes.Select(x => x as LightNode); }
            }

            /// <summary>
            ///     Gets the per frame nodes for opaque rendering. <see cref="RenderType.Opaque" />
            ///     <para>
            ///         This does not include <see cref="RenderType.Transparent" />, <see cref="RenderType.Particle" />,
            ///         <see cref="RenderType.PreProc" />, <see cref="RenderType.PostEffect" />, <see cref="RenderType.GlobalEffect" />
            ///         , <see cref="RenderType.Light" />,
            ///         <see cref="RenderType.ScreenSpaced" />
            ///     </para>
            /// </summary>
            public sealed override FastList<SceneNode> PerFrameOpaqueNodes => opaqueNodes;

            /// <summary>
            ///     Gets the per frame opaque nodes in frustum.
            /// </summary>
            /// <value>
            ///     The per frame opaque nodes in frustum.
            /// </value>
            public sealed override FastList<SceneNode> PerFrameOpaqueNodesInFrustum => opaqueNodesInFrustum;

            /// <summary>
            ///     Gets the per frame transparent nodes in frustum.
            /// </summary>
            /// <value>
            ///     The per frame transparent nodes in frustum.
            /// </value>
            public sealed override FastList<SceneNode> PerFrameTransparentNodesInFrustum => transparentNodesInFrustum;

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
            public sealed override FastList<SceneNode> PerFrameTransparentNodes => transparentNodes;

            /// <summary>
            ///     Gets the per frame transparent nodes.
            /// </summary>
            /// <value>
            ///     The per frame transparent nodes.
            /// </value>
            public sealed override FastList<SceneNode> PerFrameParticleNodes => particleNodes;

            /// <summary>
            ///     Gets the per frame post effects cores. It is the subset of <see cref="PerFrameOpaqueNodes" />
            /// </summary>
            /// <value>
            ///     The per frame post effects cores.
            /// </value>
            public sealed override FastList<SceneNode> PerFrameNodesWithPostEffect => nodesWithPostEffect;

            #endregion
        }
    }
}

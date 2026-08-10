/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Diagnostics;
using System.Runtime.CompilerServices;
using HelixToolkit.Logger;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Model.Scene2D;
using Microsoft.Extensions.Logging;

namespace HelixToolkit.SharpDX.Core.Render;
/// <summary>
/// </summary>
public partial class DefaultRenderHost : DX11RenderHostBase {
    private static LoggerLib.ILog Logger => LoggerLib.Logger.Current;
    private readonly AsyncActionThread parallelThread = new();
    private AsyncActionWaitable asyncTask;
    private Action frustumTestAction;
    private AsyncActionWaitable getPostEffectCoreTask;
    private AsyncActionWaitable getTriangleCountTask;
    private int numRendered;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DefaultRenderHost" /> class.
    /// </summary>
    public DefaultRenderHost() {
        frustumTestAction = NoFrustumTest;
        FrustumEnabledChanged += (s, e) => { SetupFrustumTestFunctions(); };
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DefaultRenderHost" /> class.
    /// </summary>
    /// <param name="createRenderer">The create renderer.</param>
    public DefaultRenderHost(Func<IDevice3DResources, IRenderer> createRenderer) : base(createRenderer) {
        frustumTestAction = NoFrustumTest;
        FrustumEnabledChanged += (s, e) => { SetupFrustumTestFunctions(); };
    }

    /// <summary>
    ///     Creates the render buffer.
    /// </summary>
    /// <returns></returns>
    protected override DX11RenderBufferProxyBase CreateRenderBuffer() {
        Logger.Info("Creating DX11Texture2DRenderBufferProxy");
        return new DX11Texture2DRenderBufferProxy(EffectsManager);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SeparateRenderables(
        RenderContext context,
        bool invalidateSceneGraph,
        bool invalidatePerFrameRenderables
    ) {
        Clear(invalidateSceneGraph, invalidatePerFrameRenderables);
        if (invalidateSceneGraph) {
            ViewportRenderables.AddRange(Viewport.Renderables);
            Renderer.UpdateSceneGraph(RenderContext, ViewportRenderables, perFrameFlattenedScene);
            if (Logger.IsEnabled(LogLevel.Trace)) Logger.Verbose("Flatten Scene Graph");
        }

        var sceneCount = perFrameFlattenedScene.Count;
        if (invalidatePerFrameRenderables) {
            if (Logger.IsEnabled(LogLevel.Trace)) Logger.Verbose("Get PerFrameRenderables");
            var isInScreenSpacedGroup = false;
            var screenSpacedGroupDepth = int.MaxValue;
            for (var i = 0; i < sceneCount;) {
                var renderable = perFrameFlattenedScene[i];
                renderable.Value.Update(context);
                var type = renderable.Value.RenderType;
                var depth = renderable.Key;
                if (!renderable.Value.IsRenderable) {
                    //Skip scene graph depth larger than current node
                    ++i;
                    for (; i < sceneCount; ++i) {
                        if (perFrameFlattenedScene[i].Key <= depth) break;
                        i += perFrameFlattenedScene[i].Value.ItemsInternal.Count;
                    }

                    continue;
                }

                if (renderable.Value.RenderCore
                              .NeedUpdate) // Run update function at the beginning of actual rendering.
                    needUpdateCores.Add(renderable.Value.RenderCore);
                ++i;
                // Add node into screen spaced array if the node belongs to a screen spaced group.
                if (isInScreenSpacedGroup && depth > screenSpacedGroupDepth) {
                    ScreenSpacedNodes.Add(renderable.Value);
                    continue;
                }

                isInScreenSpacedGroup = false;
                screenSpacedGroupDepth = int.MaxValue;
                switch (type) {
                    case RenderType.Opaque:
                        OpaqueNodes.Add(renderable.Value);
                        break;
                    case RenderType.Light:
                        LightNodes.Add(renderable.Value);
                        break;
                    case RenderType.Transparent:
                        TransparentNodes.Add(renderable.Value);
                        break;
                    case RenderType.Particle:
                        ParticleNodes.Add(renderable.Value);
                        break;
                    case RenderType.PreProc:
                        PreProcNodes.Add(renderable.Value);
                        break;
                    case RenderType.PostEffect:
                        PostEffectNodes.Add(renderable.Value);
                        break;
                    case RenderType.GlobalEffect:
                        GlobalEffectNodes.Add(renderable.Value);
                        break;
                    case RenderType.ScreenSpaced:
                        ScreenSpacedNodes.Add(renderable.Value);
                        isInScreenSpacedGroup = true;
                        screenSpacedGroupDepth = renderable.Key;
                        break;
                }
            }

            if (RenderConfiguration.EnableRenderOrder) {
                for (var i = 0; i < PreProcNodes.Count; ++i) PreProcNodes[i].UpdateRenderOrderKey();
                PreProcNodes.Sort();
                for (var i = 0; i < OpaqueNodes.Count; ++i) OpaqueNodes[i].UpdateRenderOrderKey();
                OpaqueNodes.Sort();
                for (var i = 0; i < PostEffectNodes.Count; ++i) PostEffectNodes[i].UpdateRenderOrderKey();
                PostEffectNodes.Sort();
                for (var i = 0; i < ParticleNodes.Count; ++i) ParticleNodes[i].UpdateRenderOrderKey();
                ParticleNodes.Sort();
            }

            SetupFrustumTestFunctions();
        } else {
            for (var i = 0; i < sceneCount;) {
                var renderable = perFrameFlattenedScene[i];
                renderable.Value.Update(context);
                if (!renderable.Value.IsRenderable) {
                    //Skip scene graph depth larger than current node
                    var depth = renderable.Key;
                    ++i;
                    for (; i < sceneCount; ++i) {
                        if (perFrameFlattenedScene[i].Key <= depth) break;
                        i += perFrameFlattenedScene[i].Value.ItemsInternal.Count;
                    }

                    continue;
                }

                if (renderable.Value.RenderCore
                              .NeedUpdate) // Run update function at the beginning of actual rendering.
                    needUpdateCores.Add(renderable.Value.RenderCore);
                ++i;
            }
        }
    }

    /// <summary>
    ///     <see cref="DX11RenderHostBase.PreRender" />
    /// </summary>
    protected override void PreRender(bool invalidateSceneGraph, bool invalidatePerFrameRenderables) {
        base.PreRender(invalidateSceneGraph, invalidatePerFrameRenderables);
        parallelThread.Enabled = EnableParallelProcessing;

        SeparateRenderables(RenderContext, invalidateSceneGraph, invalidatePerFrameRenderables);
        if (invalidateSceneGraph) TriggerSceneGraphUpdated();
        asyncTask = parallelThread.EnqueueAction(() => {
            Renderer?.UpdateNotRenderParallel(RenderContext, perFrameFlattenedScene);
        });
        var ft = Stopwatch.GetTimestamp();
        frustumTestAction();
        ft = Stopwatch.GetTimestamp() - ft;
        renderStatistics.FrustumTestTime = (float)ft / Stopwatch.Frequency;
        CollectPostEffectNodes();
        if ((ShowRenderDetail & RenderDetail.TriangleInfo) == RenderDetail.TriangleInfo)
            getTriangleCountTask = parallelThread.EnqueueAction(() => {
                var count = 0;
                foreach (var core in OpaqueNodesInFrustum.Select(x => x.RenderCore))
                    if (core is IGeometryRenderCore c)
                        if (c.GeometryBuffer is IGeometryBufferModel geo && geo.Geometry != null &&
                            geo.Geometry.Indices != null)
                            count += geo.Geometry.Indices.Count / 3;

                foreach (var core in TransparentNodesInFrustum.Select(x => x.RenderCore))
                    if (core is IGeometryRenderCore c)
                        if (c.GeometryBuffer is IGeometryBufferModel geo && geo.Geometry != null &&
                            geo.Geometry.Indices != null)
                            count += geo.Geometry.Indices.Count / 3;

                renderStatistics.NumTriangles = count;
            });
    }

    private void CollectPostEffectNodes() {
        //Get RenderCores with post effect specified.
        if (PostEffectNodes.Count > 0) {
            if (OpaqueNodesInFrustum.Count + TransparentNodesInFrustum.Count > 50) {
                getPostEffectCoreTask = parallelThread.EnqueueAction(() => {
                    for (var i = 0; i < OpaqueNodesInFrustum.Count; ++i)
                        if (OpaqueNodesInFrustum[i].HasAnyPostEffect)
                            NodesWithPostEffect.Add(OpaqueNodesInFrustum[i]);

                    for (var i = 0; i < TransparentNodesInFrustum.Count; ++i)
                        if (TransparentNodesInFrustum[i].HasAnyPostEffect)
                            NodesWithPostEffect.Add(TransparentNodesInFrustum[i]);
                });
            } else {
                for (var i = 0; i < OpaqueNodesInFrustum.Count; ++i)
                    if (OpaqueNodesInFrustum[i].HasAnyPostEffect)
                        NodesWithPostEffect.Add(OpaqueNodesInFrustum[i]);

                for (var i = 0; i < TransparentNodesInFrustum.Count; ++i)
                    if (TransparentNodesInFrustum[i].HasAnyPostEffect)
                        NodesWithPostEffect.Add(TransparentNodesInFrustum[i]);
            }
        }
    }

    /// <summary>
    ///     <see cref="DX11RenderHostBase.OnRender(TimeSpan)" />
    /// </summary>
    /// <param name="time">The time.</param>
    protected override void OnRender(TimeSpan time) {
        var renderParameter = new RenderParameter {
            RenderTargetView = [RenderTargetBufferView],
            DepthStencilView = DepthStencilBufferView,
            CurrentTargetTexture = RenderBuffer.ColorBuffer.Resource,
            IsMsaaTexture = RenderBuffer.ColorBufferSampleDesc.Count > 1,
            ScissorRegion = new Rectangle(0, 0, RenderBuffer.TargetWidth, RenderBuffer.TargetHeight),
            ViewportRegion = new ViewportF(0, 0, RenderBuffer.TargetWidth, RenderBuffer.TargetHeight),
            RenderLight = RenderConfiguration.RenderLights,
            UpdatePerFrameData = RenderConfiguration.UpdatePerFrameData
        };
        Renderer.SetRenderTargets(ref renderParameter);
        Renderer.UpdateGlobalVariables(RenderContext, LightNodes, ref renderParameter);
        for (var i = 0; i < needUpdateCores.Count; ++i)
            needUpdateCores[i].Update(RenderContext, Renderer.ImmediateContext);
        numRendered += needUpdateCores.Count;
        if (RenderBuffer.HasMsaa) {
            numRendered += DoDepthPrepass();
            Renderer.SetRenderTargets(ref renderParameter);
        }

        Renderer.RenderPreProc(RenderContext, PreProcNodes, ref renderParameter);
        numRendered += Renderer.RenderOpaque(RenderContext, OpaqueNodesInFrustum, ref renderParameter, false);
        numRendered += Renderer.RenderOpaque(RenderContext, ParticleNodes, ref renderParameter, true);
        numRendered +=
            Renderer.RenderTransparent(RenderContext, TransparentNodesInFrustum, ref renderParameter);

        getPostEffectCoreTask?.Wait();
        RemoveAndDispose(ref getPostEffectCoreTask);
        if (RenderConfiguration.FxaaLevel != FxaaLevel.None
            || PostEffectNodes.Count > 0 || GlobalEffectNodes.Count > 0) {
            Renderer.RenderToPingPongBuffer(RenderContext, ref renderParameter);
            renderParameter.IsMsaaTexture = false;
            renderParameter.CurrentTargetTexture = RenderBuffer.FullResPpBuffer.CurrentTexture;
            renderParameter.RenderTargetView[0] = RenderBuffer.FullResPpBuffer.CurrentRtv;
        }

        if (PostEffectNodes.Count > 0) {
            Renderer.RenderPostProc(RenderContext, PostEffectNodes, ref renderParameter);
            renderParameter.CurrentTargetTexture = RenderBuffer.FullResPpBuffer.CurrentTexture;
            renderParameter.RenderTargetView[0] = RenderBuffer.FullResPpBuffer.CurrentRtv;
        }

        if (GlobalEffectNodes.Count > 0) {
            Renderer.RenderPostProc(RenderContext, GlobalEffectNodes, ref renderParameter);
            renderParameter.CurrentTargetTexture = RenderBuffer.FullResPpBuffer.CurrentTexture;
            renderParameter.RenderTargetView[0] = RenderBuffer.FullResPpBuffer.CurrentRtv;
        }

        if (ScreenSpacedNodes.Count > 0) {
            var start = 0;
            while (start < ScreenSpacedNodes.Count)
                if (ScreenSpacedNodes[start].AffectsGlobalVariable) {
                    NodesWithPostEffect.Clear();
                    var i = start + 1;
                    for (; i < ScreenSpacedNodes.Count; ++i) {
                        if (ScreenSpacedNodes[i].AffectsGlobalVariable) break;
                        if (ScreenSpacedNodes[i].HasAnyPostEffect)
                            NodesWithPostEffect.Add(ScreenSpacedNodes[i]);
                    }

                    Renderer.RenderScreenSpaced(RenderContext,
                                                ScreenSpacedNodes,
                                                start,
                                                i - start,
                                                ref renderParameter);
                    Renderer.RenderPostProc(RenderContext, PostEffectNodes, ref renderParameter);
                    RenderContext.RestoreGlobalTransform();
                    start = i;
                } else {
                    ++start;
                }
        }

        Renderer.RenderToBackBuffer(RenderContext, ref renderParameter);
        numRendered += PreProcNodes.Count + PostEffectNodes.Count + ScreenSpacedNodes.Count;
        if (ShowRenderDetail != RenderDetail.None) {
            getTriangleCountTask?.Wait();
            renderStatistics.NumModel3D = perFrameFlattenedScene.Count;
            renderStatistics.NumCore3D = numRendered;
        }
    }

    private int DoDepthPrepass() {
        Renderer.ImmediateContext.ClearDepthStencilView(RenderBuffer.DepthStencilBufferNoMsaa,
                                                        DepthStencilClearFlags.Depth |
                                                        DepthStencilClearFlags.Stencil);
        Renderer.ImmediateContext.SetRenderTarget(RenderBuffer.DepthStencilBufferNoMsaa, null);
        RenderContext.CustomPassName = DefaultPassNames.DepthPrepass;
        for (var i = 0; i < PerFrameOpaqueNodesInFrustum.Count; ++i)
            PerFrameOpaqueNodesInFrustum[i].RenderDepth(RenderContext, Renderer.ImmediateContext, null);
        return PerFrameOpaqueNodesInFrustum.Count;
    }

    /// <summary>
    ///     <see cref="DX11RenderHostBase.PostRender" />
    /// </summary>
    protected override void PostRender() {
        asyncTask?.Wait();
        RemoveAndDispose(ref asyncTask);
        getTriangleCountTask?.Wait();
        RemoveAndDispose(ref getTriangleCountTask);
    }

    /// <summary>
    ///     Called when [render2 d].
    /// </summary>
    /// <param name="time">The time.</param>
    protected override void OnRender2D(TimeSpan time) {
        ViewportRenderable2D.Clear();
        var d2DRoot = Viewport.D2DRenderables.FirstOrDefault();
        var renderD2D = false;
        if (d2DRoot != null && d2DRoot.ItemsInternal.Count > 0 && RenderConfiguration.RenderD2D) {
            renderD2D = true;
            d2DRoot.Measure(new Size2F(ActualWidth, ActualHeight));
            d2DRoot.Arrange(new RectangleF(0, 0, ActualWidth, ActualHeight));
        }

        if (!renderD2D) return;
        ViewportRenderable2D.AddRange(Viewport.D2DRenderables);
        Renderer.UpdateSceneGraph2D(RenderContext2D, ViewportRenderable2D);

        foreach (var node2D in ViewportRenderable2D)
            node2D.Render(RenderContext2D);

        //Draw bitmap cache to render target
        RenderContext2D.PushRenderTarget(D2DTarget.D2DTarget, false);
        if (renderD2D || ShowRenderDetail != RenderDetail.None)
            foreach (var node2D in ViewportRenderable2D)
                node2D.RenderBitmapCache(RenderContext2D);

        RenderContext2D.PopRenderTarget();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Clear(bool clearFrameRenderables, bool clearPerFrameRenderables) {
        numRendered = 0;
        var fastClear = !clearFrameRenderables;
        ViewportRenderables.Clear(fastClear);
        needUpdateCores.Clear(fastClear);
        NodesWithPostEffect.Clear(fastClear);
        OpaqueNodesInFrustum.Clear(fastClear);
        TransparentNodesInFrustum.Clear(fastClear);
        if (clearFrameRenderables) perFrameFlattenedScene.Clear();
        if (clearPerFrameRenderables) {
            OpaqueNodes.Clear(fastClear);
            TransparentNodes.Clear(fastClear);
            ParticleNodes.Clear(fastClear);
            LightNodes.Clear(fastClear);
            PostEffectNodes.Clear(fastClear);
            GlobalEffectNodes.Clear(fastClear);
            PreProcNodes.Clear(fastClear);
            ScreenSpacedNodes.Clear(fastClear);
        }
    }

    protected override void OnStartD3D() {
        base.OnStartD3D();
#if !WINUI
        parallelThread.Start();
#endif
    }

    /// <summary>
    ///     Called when [ending d3 d].
    /// </summary>
    protected override void OnEndingD3D() {
        Logger.Info("On Ending D3D");
        asyncTask?.Wait();
        getTriangleCountTask?.Wait();
        getPostEffectCoreTask?.Wait();
        RemoveAndDispose(ref asyncTask);
        RemoveAndDispose(ref getTriangleCountTask);
        RemoveAndDispose(ref getPostEffectCoreTask);
#if !WINUI
        parallelThread.Stop();
#endif
        Clear(true, true);
        base.OnEndingD3D();
    }

    protected override void OnDispose(bool disposeManagedResources) {
        parallelThread.Dispose();
        base.OnDispose(disposeManagedResources);
    }

    #region FrustumTest

    protected void SetupFrustumTestFunctions() {
        if (!EnableRenderFrustum)
            frustumTestAction = NoFrustumTest;
        else
            frustumTestAction = FrustumTestDefault;
    }

    private void NoFrustumTest() {
        OpaqueNodesInFrustum.AddAll(OpaqueNodes);
        TransparentNodesInFrustum.AddAll(TransparentNodes);
    }

    private void FrustumTestDefault() {
        var frustum = RenderContext.BoundingFrustum;
        for (var i = 0; i < OpaqueNodes.Count; ++i) {
            OpaqueNodes.Items[i].IsInFrustum = OpaqueNodes.Items[i].TestViewFrustum(ref frustum);
            if (OpaqueNodes.Items[i].IsInFrustum) OpaqueNodesInFrustum.Add(OpaqueNodes.Items[i]);
        }

        for (var i = 0; i < TransparentNodes.Count; ++i) {
            TransparentNodes.Items[i].IsInFrustum = TransparentNodes.Items[i].TestViewFrustum(ref frustum);
            if (TransparentNodes.Items[i].IsInFrustum) 
                TransparentNodesInFrustum.Add(TransparentNodes.Items[i]);
        }
    }

    #endregion
}

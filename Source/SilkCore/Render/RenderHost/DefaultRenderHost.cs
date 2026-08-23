/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Diagnostics;
using System.Runtime.CompilerServices;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Logger;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render.RenderBuffers;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Utilities.AsyncTasks;
using Microsoft.Extensions.Logging;

namespace HelixToolkit.SharpDX.Core.Render.RenderHost;

/// <summary>
/// </summary>
public partial class DefaultRenderHost : DX11RenderHostBase {
    private static LoggerLib.ILog Logger => LoggerLib.Logger.Current;
    private readonly AsyncActionThread parallelThread = new();
    private AsyncActionWaitable? asyncTask;
    private Action frustumTestAction;
    private AsyncActionWaitable? getPostEffectCoreTask;
    private AsyncActionWaitable? getTriangleCountTask;
    private int numRendered;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DefaultRenderHost" /> class.
    /// </summary>
    public DefaultRenderHost() {
        frustumTestAction = NoFrustumTest;
        FrustumEnabledChanged += (_, _) => { SetupFrustumTestFunctions(); };
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DefaultRenderHost" /> class.
    /// </summary>
    /// <param name="createRenderer">The create renderer.</param>
    public DefaultRenderHost(Func<IDevice3DResources, IRenderer> createRenderer) : base(createRenderer) {
        frustumTestAction = NoFrustumTest;
        FrustumEnabledChanged += (_, _) => { SetupFrustumTestFunctions(); };
    }

    /// <summary>
    ///     Creates the render buffer.
    /// </summary>
    /// <returns></returns>
    protected override DX11RenderBufferProxyBase CreateRenderBuffer() {
        Logger.Info("Creating DX11Texture2DRenderBufferProxy");
        return new DX11Texture2DRenderBufferProxy(
            EffectsManager.AssertNotNull("Effects manager is not initialized."));
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SeparateRenderables(
        RenderContext context,
        bool invalidateSceneGraph,
        bool invalidatePerFrameRenderables
    ) {
        var viewport = Viewport.AssertNotNull("Viewport is not initialized.");
        var renderer = Renderer.AssertNotNull("Renderer is not initialized.");
        Clear(invalidateSceneGraph, invalidatePerFrameRenderables);
        if (invalidateSceneGraph) {
            ViewportRenderables.AddRange(viewport.Renderables);
            renderer.UpdateSceneGraph(context, ViewportRenderables, PerFrameFlattenedSceneInternal);
            if (Logger.IsEnabled(LogLevel.Trace)) Logger.Verbose("Flatten Scene Graph");
        }

        var sceneCount = PerFrameFlattenedSceneInternal.Count;
        if (invalidatePerFrameRenderables) {
            if (Logger.IsEnabled(LogLevel.Trace)) Logger.Verbose("Get PerFrameRenderables");
            var isInScreenSpacedGroup = false;
            var screenSpacedGroupDepth = int.MaxValue;
            for (var i = 0; i < sceneCount;) {
                var renderable = PerFrameFlattenedSceneInternal[i];
                renderable.Value.Update(context);
                var type = renderable.Value.RenderType;
                var depth = renderable.Key;
                if (!renderable.Value.IsRenderable) {
                    //Skip scene graph depth larger than current node
                    ++i;
                    for (; i < sceneCount; ++i) {
                        if (PerFrameFlattenedSceneInternal[i].Key <= depth) break;
                        i += PerFrameFlattenedSceneInternal[i].Value.ItemsInternal.Count;
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
                for (var i = 0; i < PreProcNodes.Count; ++i)
                    PreProcNodes[i]
                        .UpdateRenderOrderKey();
                PreProcNodes.Sort();
                for (var i = 0; i < OpaqueNodes.Count; ++i)
                    OpaqueNodes[i]
                        .UpdateRenderOrderKey();
                OpaqueNodes.Sort();
                for (var i = 0; i < PostEffectNodes.Count; ++i)
                    PostEffectNodes[i]
                        .UpdateRenderOrderKey();
                PostEffectNodes.Sort();
                for (var i = 0; i < ParticleNodes.Count; ++i)
                    ParticleNodes[i]
                        .UpdateRenderOrderKey();
                ParticleNodes.Sort();
            }

            SetupFrustumTestFunctions();
        } else {
            for (var i = 0; i < sceneCount;) {
                var renderable = PerFrameFlattenedSceneInternal[i];
                renderable.Value.Update(context);
                if (!renderable.Value.IsRenderable) {
                    //Skip scene graph depth larger than current node
                    var depth = renderable.Key;
                    ++i;
                    for (; i < sceneCount; ++i) {
                        if (PerFrameFlattenedSceneInternal[i].Key <= depth) break;
                        i += PerFrameFlattenedSceneInternal[i].Value.ItemsInternal.Count;
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
        var context = RenderContext.AssertNotNull("Render context is not initialized.");
        var renderer = Renderer.AssertNotNull("Renderer is not initialized.");
        parallelThread.Enabled = EnableParallelProcessing;

        SeparateRenderables(context, invalidateSceneGraph, invalidatePerFrameRenderables);
        if (invalidateSceneGraph) TriggerSceneGraphUpdated();
        asyncTask = parallelThread.EnqueueAction(() => {
            renderer.UpdateNotRenderParallel(context, PerFrameFlattenedSceneInternal);
        });
        var ft = Stopwatch.GetTimestamp();
        frustumTestAction();
        ft = Stopwatch.GetTimestamp() - ft;
        RenderStatisticsInternal.FrustumTestTime = (float) ft / Stopwatch.Frequency;
        CollectPostEffectNodes();
        if ((ShowRenderDetail & RenderDetail.TriangleInfo) == RenderDetail.TriangleInfo)
            getTriangleCountTask = parallelThread.EnqueueAction(() => {
                var count = 0;
                foreach (var core in OpaqueNodesInFrustum.Select(x => x.RenderCore))
                    if (core is IGeometryRenderCore {
                            GeometryBuffer: IGeometryBufferModel {Geometry.Indices: not null} geo
                        })
                        count += geo.Geometry.Indices.Count / 3;

                foreach (var core in TransparentNodesInFrustum.Select(x => x.RenderCore))
                    if (core is IGeometryRenderCore {
                            GeometryBuffer: IGeometryBufferModel {Geometry.Indices: not null} geo
                        })
                        count += geo.Geometry.Indices.Count / 3;

                RenderStatisticsInternal.NumTriangles = count;
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
        var renderer = Renderer.AssertNotNull("Renderer is not initialized.");
        var context = RenderContext.AssertNotNull("Render context is not initialized.");
        var renderBuffer = RenderBuffer.AssertNotNull("Render buffer is not initialized.");
        var renderParameter = new RenderParameter {
            RenderTargetView = [RenderTargetBufferView.AssertNotNull("Render target view is not initialized.")],
            DepthStencilView = DepthStencilBufferView,
            CurrentTargetTexture = renderBuffer.ColorBuffer.Resource.AssertNotNull("Color buffer is not initialized."),
            IsMsaaTexture = renderBuffer.ColorBufferSampleDesc.Count > 1,
            ScissorRegion = new Rectangle(0, 0, renderBuffer.TargetWidth, renderBuffer.TargetHeight),
            ViewportRegion = new ViewportF(0, 0, renderBuffer.TargetWidth, renderBuffer.TargetHeight),
            RenderLight = RenderConfiguration.RenderLights,
            UpdatePerFrameData = RenderConfiguration.UpdatePerFrameData
        };
        renderer.SetRenderTargets(ref renderParameter);
        renderer.UpdateGlobalVariables(context, LightNodes, ref renderParameter);
        for (var i = 0; i < needUpdateCores.Count; ++i)
            needUpdateCores[i]
                .Update(context, renderer.ImmediateContext);
        numRendered += needUpdateCores.Count;
        if (renderBuffer.HasMsaa) {
            numRendered += DoDepthPrepass();
            renderer.SetRenderTargets(ref renderParameter);
        }

        renderer.RenderPreProc(context, PreProcNodes, ref renderParameter);
        numRendered += renderer.RenderOpaque(context, OpaqueNodesInFrustum, ref renderParameter, false);
        numRendered += renderer.RenderOpaque(context, ParticleNodes, ref renderParameter, true);
        numRendered +=
            renderer.RenderTransparent(context, TransparentNodesInFrustum, ref renderParameter);

        getPostEffectCoreTask?.Wait();
        RemoveAndDispose(ref getPostEffectCoreTask);
        if (RenderConfiguration.FxaaLevel != FxaaLevel.None
            || PostEffectNodes.Count > 0 || GlobalEffectNodes.Count > 0) {
            renderer.RenderToPingPongBuffer(context, ref renderParameter);
            renderParameter.IsMsaaTexture = false;
            renderParameter.CurrentTargetTexture = renderBuffer.FullResPpBuffer.CurrentTexture.AssertNotNull(
                "Post-processing texture is not initialized.");
            renderParameter.RenderTargetView[0] =
                ((RenderTargetView?) renderBuffer.FullResPpBuffer.CurrentRtv).AssertNotNull(
                    "Post-processing render target is not initialized.");
        }

        if (PostEffectNodes.Count > 0) {
            renderer.RenderPostProc(context, PostEffectNodes, ref renderParameter);
            renderParameter.CurrentTargetTexture = renderBuffer.FullResPpBuffer.CurrentTexture.AssertNotNull(
                "Post-processing texture is not initialized.");
            renderParameter.RenderTargetView[0] =
                ((RenderTargetView?) renderBuffer.FullResPpBuffer.CurrentRtv).AssertNotNull(
                    "Post-processing render target is not initialized.");
        }

        if (GlobalEffectNodes.Count > 0) {
            renderer.RenderPostProc(context, GlobalEffectNodes, ref renderParameter);
            renderParameter.CurrentTargetTexture = renderBuffer.FullResPpBuffer.CurrentTexture.AssertNotNull(
                "Post-processing texture is not initialized.");
            renderParameter.RenderTargetView[0] =
                ((RenderTargetView?) renderBuffer.FullResPpBuffer.CurrentRtv).AssertNotNull(
                    "Post-processing render target is not initialized.");
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

                    renderer.RenderScreenSpaced(context,
                        ScreenSpacedNodes,
                        start,
                        i - start,
                        ref renderParameter);
                    renderer.RenderPostProc(context, PostEffectNodes, ref renderParameter);
                    context.RestoreGlobalTransform();
                    start = i;
                } else {
                    ++start;
                }
        }

        renderer.RenderToBackBuffer(context, ref renderParameter);
        numRendered += PreProcNodes.Count + PostEffectNodes.Count + ScreenSpacedNodes.Count;
        if (ShowRenderDetail != RenderDetail.None) {
            getTriangleCountTask?.Wait();
            RenderStatisticsInternal.NumModel3D = PerFrameFlattenedSceneInternal.Count;
            RenderStatisticsInternal.NumCore3D = numRendered;
        }
    }

    private int DoDepthPrepass() {
        var renderer = Renderer.AssertNotNull("Renderer is not initialized.");
        var context = RenderContext.AssertNotNull("Render context is not initialized.");
        var depthStencilBufferProxy = RenderBuffer.AssertNotNull("Render buffer is not initialized.")
            .DepthStencilBufferNoMsaa;
        var depthStencilBuffer = ((DepthStencilView?) depthStencilBufferProxy).AssertNotNull(
            "Depth stencil buffer is not initialized.");
        renderer.ImmediateContext.ClearDepthStencilView(depthStencilBuffer,
            DepthStencilClearFlags.Depth |
            DepthStencilClearFlags.Stencil);
        renderer.ImmediateContext.SetRenderTarget(depthStencilBuffer, null);
        context.CustomPassName = DefaultPassNames.DepthPrepass;
        for (var i = 0; i < PerFrameOpaqueNodesInFrustum.Count; ++i)
            PerFrameOpaqueNodesInFrustum[i]
                .RenderDepth(context, renderer.ImmediateContext, null);
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
        var viewport = Viewport.AssertNotNull("Viewport is not initialized.");
        var renderer = Renderer.AssertNotNull("Renderer is not initialized.");
        var context = RenderContext2D.AssertNotNull("2D render context is not initialized.");
        ViewportRenderable2D.Clear();
        var d2DRoot = viewport.D2DRenderables.FirstOrDefault();
        var renderD2D = false;
        if (d2DRoot is {ItemsInternal.Count: > 0} && RenderConfiguration.RenderD2D) {
            renderD2D = true;
            d2DRoot.Measure(new Size2F(ActualWidth, ActualHeight));
            d2DRoot.Arrange(new RectangleF(0, 0, ActualWidth, ActualHeight));
        }

        if (!renderD2D) return;
        ViewportRenderable2D.AddRange(viewport.D2DRenderables);
        renderer.UpdateSceneGraph2D(context, ViewportRenderable2D);

        foreach (var node2D in ViewportRenderable2D)
            node2D?.Render(context);

        //Draw bitmap cache to render target
        context.PushRenderTarget(D2DTarget.AssertNotNull("2D target is not initialized.")
            .D2DTarget.AssertNotNull(
                "2D target bitmap is not initialized."), false);
        if (renderD2D || ShowRenderDetail != RenderDetail.None)
            foreach (var node2D in ViewportRenderable2D)
                node2D?.RenderBitmapCache(context);

        context.PopRenderTarget();
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
        if (clearFrameRenderables) PerFrameFlattenedSceneInternal.Clear();
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
        var frustum = default(BoundingFrustum);
        SceneNodeFrustumSelector.AppendVisible(OpaqueNodes, OpaqueNodesInFrustum, false, ref frustum);
        SceneNodeFrustumSelector.AppendVisible(TransparentNodes, TransparentNodesInFrustum, false, ref frustum);
    }

    private void FrustumTestDefault() {
        var frustum = RenderContext.AssertNotNull("Render context is not initialized.")
            .BoundingFrustum;
        SceneNodeFrustumSelector.AppendVisible(OpaqueNodes, OpaqueNodesInFrustum, true, ref frustum);
        SceneNodeFrustumSelector.AppendVisible(TransparentNodes, TransparentNodesInFrustum, true, ref frustum);
    }

    #endregion
}

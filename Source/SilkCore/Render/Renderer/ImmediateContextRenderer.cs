/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

//#define OLD

using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Core.PostEffects;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Collection;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Model.Scene2D.Abstract;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Render.Renderer;
/// <summary>
/// </summary>
public class ImmediateContextRenderer : DisposeObject, IRenderer {
    private static readonly Func<SceneNode, RenderContext, bool> UpdateFunc = (_, _) => true;

    private readonly Stack<(int Key, IList<SceneNode2D> Value)> stack2DCache1 = new(20);
    private readonly Stack<(int Key, IList<SceneNode> Value)> stackCache1 = new(20);
    private DeviceContextProxy.DeviceContextProxy immediateContext;
    private OitDepthPeeling oitDepthPeelingCore;
    private OrderIndependentTransparentRenderCore oitWeightedCore;
    private PostEffectFxaa postFxaaCore;
    private SsaoCore preSsaoCore;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ImmediateContextRenderer" /> class.
    /// </summary>
    /// <param name="deviceResource">The deviceResource.</param>
    public ImmediateContextRenderer(IDevice3DResources deviceResource) {
        immediateContext = new DeviceContextProxy.DeviceContextProxy(deviceResource.NativeDeviceResources.ImmediateContext,
                                                  deviceResource.NativeDeviceResources.Device);
        oitWeightedCore = new OrderIndependentTransparentRenderCore();
        oitDepthPeelingCore = new OitDepthPeeling();
        postFxaaCore = new PostEffectFxaa();
        preSsaoCore = new SsaoCore();
    }

    /// <summary>
    ///     Gets or sets the immediate context.
    /// </summary>
    /// <value>
    ///     The immediate context.
    /// </value>
    public DeviceContextProxy.DeviceContextProxy ImmediateContext => immediateContext;

    /// <summary>
    ///     Updates the scene graph.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="renderables">The renderables.</param>
    /// <param name="results">Returns list of flattened scene graph with depth index as KeyValuePair.Key</param>
    /// <returns></returns>
    public virtual void UpdateSceneGraph(
        RenderContext context,
        FastList<SceneNode> renderables,
        FastList<(int Key, SceneNode Value)> results
    ) =>
        renderables.PreorderDft(context, UpdateFunc, results, stackCache1);


    /// <summary>
    ///     Updates the scene graph.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="renderables">The renderables.</param>
    /// <returns></returns>
    public void UpdateSceneGraph2D(RenderContext2D context, FastList<SceneNode2D> renderables) {
        renderables.PreorderDftRun(x => {
                                       x.Update(context);
                                       return x.IsRenderable;
                                   },
                                   stack2DCache1);
    }

    /// <summary>
    ///     Updates the global variables. Such as light buffer and global transformation buffer
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="lights">The lights.</param>
    /// <param name="parameter">The parameter.</param>
    public virtual void UpdateGlobalVariables(
        RenderContext context,
        FastList<SceneNode> lights,
        ref RenderParameter parameter
    ) {
        ImmediateContext.Reset();
        if (parameter.RenderLight) {
            context.LightScene.LightModels.ResetLightCount();
            var count = lights.Count;
            for (var i = 0; i < count && i < Constants.MaxLights; ++i)
                lights[i].Render(context, ImmediateContext);
        }

        if (parameter.UpdatePerFrameData) context.UpdatePerFrameData(ImmediateContext);
    }

    /// <summary>
    ///     Renders the scene.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="renderables">The renderables.</param>
    /// <param name="parameter">The parameter.</param>
    /// <param name="testFrustum"></param>
    /// <returns>Number of node has been rendered</returns>
    public virtual int RenderOpaque(
        RenderContext context,
        FastList<SceneNode> renderables,
        ref RenderParameter parameter,
        bool testFrustum
    ) {
        var renderedCount = 0;
        var count = renderables.Count;
        var frustum = context.BoundingFrustum;
        if (!testFrustum)
            for (var i = 0; i < count; ++i) {
                renderables[i].Render(context, ImmediateContext);
                ++renderedCount;
            }
        else
            for (var i = 0; i < count; ++i) {
                if (!renderables[i].TestViewFrustum(ref frustum)) continue;
                renderables[i].Render(context, ImmediateContext);
                ++renderedCount;
            }

        return renderedCount;
    }

    /// <summary>
    ///     Renders the transparent.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="renderables">The renderables.</param>
    /// <param name="parameter">The parameter.</param>
    /// <returns></returns>
    public virtual int RenderTransparent(
        RenderContext context,
        FastList<SceneNode> renderables,
        ref RenderParameter parameter
    ) {
        if (renderables.Count == 0) return 0;
        if (context.RenderHost.RenderConfiguration.OitRenderType != OitRenderType.None
            && context.RenderHost.FeatureLevel >= FeatureLevel.Level110)
            switch (context.RenderHost.RenderConfiguration.OitRenderType) {
                case OitRenderType.SinglePassWeighted:
                    oitWeightedCore.ExternRenderParameter = parameter;
                    oitWeightedCore.Render(context, ImmediateContext);
                    return oitWeightedCore.RenderCount;
                case OitRenderType.DepthPeeling:
                    if (oitDepthPeelingCore.IsAttached) {
                        oitDepthPeelingCore.ExternRenderParameter = parameter;
                        oitDepthPeelingCore.PeelingIteration = context.OitDepthPeelingIteration;
                        oitDepthPeelingCore.Render(context, ImmediateContext);
                        return oitDepthPeelingCore.RenderCount;
                    }
                    break;
            }

        var renderedCount = 0;
        var count = renderables.Count;
        for (var i = 0; i < count; ++i) {
            renderables[i].Render(context, ImmediateContext);
            ++renderedCount;
        }

        return renderedCount;
    }

    /// <summary>
    ///     Updates the no render parallel.
    ///     <see cref="IRenderer.UpdateNotRenderParallel(RenderContext, FastList{KeyValuePair{int, SceneNode}})" />
    /// </summary>
    /// <param name="renderables">The renderables.</param>
    /// <param name="context"></param>
    /// <returns></returns>
    public virtual void UpdateNotRenderParallel(
        RenderContext context,
        FastList<KeyValuePair<int, SceneNode>> renderables
    ) {
        var count = renderables.Count;
        for (var i = 0; i < count; ++i) renderables[i].Value.UpdateNotRender(context);
    }

    /// <summary>
    ///     Sets the render targets.
    /// </summary>
    /// <param name="parameter">The parameter.</param>
    public void SetRenderTargets(ref RenderParameter parameter) {
        ImmediateContext.SetRenderTargets(parameter.DepthStencilView, parameter.RenderTargetView);
        ImmediateContext.SetViewport(ref parameter.ViewportRegion);
        ImmediateContext.SetScissorRectangle(parameter.ScissorRegion.Left,
                                             parameter.ScissorRegion.Top,
                                             parameter.ScissorRegion.Right,
                                             parameter.ScissorRegion.Bottom);
    }

    public void UpdateNotRenderParallel(RenderContext context, FastList<(int Key, SceneNode Value)> renderables) {
    }

    /// <summary>
    ///     Render2s the d.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="renderables">The renderables.</param>
    /// <param name="parameter">The parameter.</param>
    public virtual void RenderScene2D(
        RenderContext2D context,
        FastList<SceneNode2D> renderables,
        ref RenderParameter2D parameter
    ) {
        var count = renderables.Count;
        for (var i = 0; i < count; ++i) renderables[i].Render(context);
    }

    /// <summary>
    ///     Renders the pre proc.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="renderables">The renderables.</param>
    /// <param name="parameter">The parameter.</param>
    public virtual void RenderPreProc(
        RenderContext context,
        FastList<SceneNode> renderables,
        ref RenderParameter parameter
    ) {
        var count = renderables.Count;
        for (var i = 0; i < count; ++i) renderables[i].Render(context, ImmediateContext);
        if (context.SsaoEnabled) {
            preSsaoCore.Radius = context.RenderHost.RenderConfiguration.SsaoRadius;
            preSsaoCore.Quality = context.RenderHost.RenderConfiguration.SsaoQuality;
            preSsaoCore.Render(context, ImmediateContext);
        }
    }

    /// <summary>
    ///     Renders the post proc.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="renderables">The renderables.</param>
    /// <param name="parameter">The parameter.</param>
    public virtual void RenderPostProc(
        RenderContext context,
        FastList<SceneNode> renderables,
        ref RenderParameter parameter
    ) {
        var count = renderables.Count;
        for (var i = 0; i < count; ++i) renderables[i].Render(context, ImmediateContext);
    }

    /// <summary>
    ///     Renders to ping pong buffer.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="parameter">The parameter.</param>
    public virtual void RenderToPingPongBuffer(RenderContext context, ref RenderParameter parameter) {
        if (context.RenderHost.RenderBuffer is not { } buffer)
            return;

        buffer.FullResPpBuffer.Initialize();
        if (buffer.FullResPpBuffer.CurrentTexture is not { } destination)
            return;

        if (parameter.IsMsaaTexture)
            ImmediateContext.ResolveSubresource(parameter.CurrentTargetTexture,
                                                0,
                                                destination,
                                                0,
                                                buffer.Format);
        else
            ImmediateContext.CopyResource(parameter.CurrentTargetTexture,
                                          destination);
    }

    /// <summary>
    ///     Renders the screen spaced.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="renderables">The renderables.</param>
    /// <param name="start">Start index in renderables</param>
    /// <param name="count">Number of renderables to render.</param>
    /// <param name="parameter">The parameter.</param>
    public virtual void RenderScreenSpaced(
        RenderContext context,
        FastList<SceneNode> renderables,
        int start,
        int count,
        ref RenderParameter parameter
    ) {
        if (count > 0) {
            if (context.RenderHost.RenderBuffer is not { } buffer)
                return;

            var depthStencilBuffer = parameter.IsMsaaTexture
                                         ? buffer.DepthStencilBuffer
                                         : buffer.DepthStencilBufferNoMsaa;
            ImmediateContext.SetRenderTargets(depthStencilBuffer, parameter.RenderTargetView);

            for (var i = start; i < start + count; ++i) renderables[i].Render(context, ImmediateContext);
        }
    }

    /// <summary>
    ///     Renders to back buffer.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="parameter">The parameter.</param>
    public virtual void RenderToBackBuffer(RenderContext context, ref RenderParameter parameter) {
        if (context.RenderHost.FeatureLevel >= FeatureLevel.Level110
            && context.RenderHost.RenderConfiguration.FxaaLevel != FxaaLevel.None) {
            postFxaaCore.FxaaLevel = context.RenderHost.RenderConfiguration.FxaaLevel;
            postFxaaCore.Render(context, ImmediateContext);
        }

        ImmediateContext.Flush();
        if (context.RenderHost.RenderBuffer is not { } buffer)
            return;

        if (buffer.BackBuffer.Resource is not { } destination)
            return;

        if (parameter.IsMsaaTexture)
            ImmediateContext.ResolveSubresource(parameter.CurrentTargetTexture,
                                                0,
                                                destination,
                                                0,
                                                buffer.Format);
        else
            ImmediateContext.CopyResource(parameter.CurrentTargetTexture, destination);
    }

    public void Attach(IRenderHost host) {
        if (host is {FeatureLevel: >= FeatureLevel.Level110, EffectsManager: { } effectsManager}) {
            oitWeightedCore.Attach(effectsManager.GetTechnique(DefaultRenderTechniqueNames.MeshOitQuad));
            oitDepthPeelingCore.Attach(
                effectsManager.GetTechnique(DefaultRenderTechniqueNames.MeshOitDepthPeeling));
            postFxaaCore.Attach(effectsManager.GetTechnique(DefaultRenderTechniqueNames.PostEffectFxaa));
            preSsaoCore.Attach(effectsManager.GetTechnique(DefaultRenderTechniqueNames.Ssao));
        }
    }

    public void Detach() {
        stackCache1.Clear();
        stack2DCache1.Clear();
        oitWeightedCore.Detach();
        oitDepthPeelingCore.Detach();
        postFxaaCore.Detach();
        preSsaoCore.Detach();
    }

    /// <summary>
    ///     Renders the screenspaced node's post proc.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="screenSpacedWithPostEffects"></param>
    /// <param name="nodesWithPostEffects"></param>
    /// <param name="postProcNodes"></param>
    /// <param name="parameter">The parameter.</param>
    public virtual void RenderScreenSpacedPostProc(
        RenderContext context,
        FastList<SceneNode> screenSpacedWithPostEffects,
        FastList<SceneNode> nodesWithPostEffects,
        FastList<SceneNode> postProcNodes,
        ref RenderParameter parameter
    ) {
        var i = 0;
        while (i < screenSpacedWithPostEffects.Count) {
            if (screenSpacedWithPostEffects[i].AffectsGlobalVariable) {
                context.RestoreGlobalTransform();
                screenSpacedWithPostEffects[i].Render(context, ImmediateContext);
                nodesWithPostEffects.Clear();
                while (++i < screenSpacedWithPostEffects.Count
                       && !screenSpacedWithPostEffects[i].AffectsGlobalVariable)
                    nodesWithPostEffects.Add(screenSpacedWithPostEffects[i]);
                RenderPostProc(context, postProcNodes, ref parameter);
                continue;
            }

            ++i;
        }
    }

    protected override void OnDispose(bool disposeManagedResources) {
        Detach();
        immediateContext.Dispose();
        oitWeightedCore.Dispose();
        oitDepthPeelingCore.Dispose();
        postFxaaCore.Dispose();
        preSsaoCore.Dispose();
        base.OnDispose(disposeManagedResources);
    }
}

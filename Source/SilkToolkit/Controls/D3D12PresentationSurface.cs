/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Camera;
using HelixToolkit.SharpDX.Core.Model.Collection;
using HelixToolkit.SharpDX.Core.Model.Lights;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Model.Scene.Lights;
using HelixToolkit.SharpDX.Core.Model.Scene.PostEffects;
using HelixToolkit.SharpDX.Core.Model.Scene2D.Abstract;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace HelixToolkit.Wpf.SharpDX.Controls;

/// <summary>
///     Owns the pure-WPF HWND and synchronous Direct3D 12 presentation lifecycle.
/// </summary>
public sealed class D3D12PresentationSurface : Grid, IDisposable {
    /// <summary>
    ///     The depth/stencil format shared by the presentation resource and graphics pipelines.
    /// </summary>
    private const Format DepthStencilFormat = Format.FormatD32FloatS8X24Uint;

    private readonly HwndSwapChainHost windowHost = new();
    private readonly D3D12ResizeQueue resizeQueue = new();
    private readonly SilkDriverType driverType;
    private IEffectsManager? effectsManager;
    private DispatcherOperation? resizeOperation;
    private SilkD3D12Device? device;
    private SilkD3D12CommandQueue? commandQueue;
    private SilkD3D12CommandContext? commandContext;
    private SilkD3D12Fence? fence;
    private SilkD3D12SwapChain? swapChain;
    private SilkD3D12DescriptorHeap? resourceHeap;
    private SilkD3D12DescriptorHeap? samplerHeap;
    private SilkD3D12SceneRenderer? sceneRenderer;
    private D3D12ScenePassCatalog? passCatalog;

    /// <summary>
    ///     The non-shader-visible depth/stencil descriptor heap.
    /// </summary>
    private SilkD3D12DescriptorHeap? depthStencilHeap;

    /// <summary>
    ///     The stable depth/stencil descriptor allocation.
    /// </summary>
    private SilkD3D12Descriptor? depthStencilView;

    /// <summary>
    ///     The size-dependent presentation depth/stencil texture.
    /// </summary>
    private SilkD3D12Resource? depthStencilBuffer;

    /// <summary>
    ///     Reused opaque viewport candidates in scene traversal order.
    /// </summary>
    private readonly FastList<SceneNode> opaqueNodes = [];

    /// <summary>
    ///     Reused transparent viewport candidates in scene traversal order.
    /// </summary>
    private readonly FastList<SceneNode> transparentNodes = [];

    /// <summary>
    ///     Reused volume nodes in scene traversal order.
    /// </summary>
    private readonly FastList<SceneNode> volumeNodes = [];

    /// <summary>
    ///     Reused particle nodes in scene traversal order.
    /// </summary>
    private readonly FastList<SceneNode> particleNodes = [];

    /// <summary>
    ///     Reused visible light nodes in scene traversal order.
    /// </summary>
    private readonly FastList<SceneNode> lightNodes = [];

    /// <summary>
    ///     Reused global post-effect nodes in scene traversal order.
    /// </summary>
    private readonly FastList<SceneNode> globalEffectNodes = [];

    /// <summary>
    ///     Reused object-level post-effect nodes in scene traversal order.
    /// </summary>
    private readonly FastList<SceneNode> postEffectNodes = [];

    /// <summary>
    ///     Reused screen-spaced root nodes in scene traversal order.
    /// </summary>
    private readonly FastList<ScreenSpacedNode> screenSpacedNodes = [];

    /// <summary>
    ///     Reused opaque children of the current screen-spaced root.
    /// </summary>
    private readonly FastList<SceneNode> screenSpacedOpaqueNodes = [];

    /// <summary>
    ///     Reused transparent children of the current screen-spaced root.
    /// </summary>
    private readonly FastList<SceneNode> screenSpacedTransparentNodes = [];

    /// <summary>
    ///     Shared light payload rebuilt before each viewport frame.
    /// </summary>
    private readonly LightsBufferModel lights = new();

    /// <summary>
    ///     Creates a presentation surface using hardware rendering by default.
    /// </summary>
    /// <param name="driverType">The hardware or WARP device type.</param>
    public D3D12PresentationSurface(SilkDriverType driverType = SilkDriverType.Hardware,
        IEffectsManager? effectsManager = null) {
        this.driverType = driverType;
        this.effectsManager = effectsManager;
        Children.Add(windowHost);
        windowHost.HandleCreated += OnHandleCreated;
        windowHost.HandleDestroyed += OnHandleDestroyed;
        windowHost.DpiScaleChanged += OnDpiScaleChanged;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    /// <summary>
    ///     Gets the hosted native window.
    /// </summary>
    public HwndSwapChainHost WindowHost => windowHost;

    /// <summary>
    ///     Gets whether native presentation resources are initialized.
    /// </summary>
    public bool IsInitialized => swapChain is not null;

    /// <summary>
    ///     Gets the current physical back-buffer width.
    /// </summary>
    public uint PixelWidth => swapChain?.Width ?? 0;

    /// <summary>
    ///     Updates the technique registry after a late WPF data binding has resolved.
    /// </summary>
    /// <param name="value">The current viewport effects manager.</param>
    internal void SetEffectsManager(IEffectsManager? value) {
        Dispatcher.VerifyAccess();
        if (ReferenceEquals(effectsManager, value)) return;

        effectsManager = value;
        if (device is null || swapChain is null) return;

        var replacement = new D3D12ScenePassCatalog(device,
            swapChain.Format,
            DepthStencilFormat,
            (effectsManager as EffectsManager)?.TechniqueDescriptions,
            effectsManager);
        passCatalog?.Dispose();
        passCatalog = replacement;
    }

    /// <summary>
    ///     Gets the current physical back-buffer height.
    /// </summary>
    public uint PixelHeight => swapChain?.Height ?? 0;

    /// <summary>
    ///     Gets the number of successfully presented frames.
    /// </summary>
    public ulong PresentedFrames { get; private set; }

    /// <summary>
    ///     Gets whether this surface has been disposed.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    ///     Clears and presents one frame, waiting for completion before returning.
    /// </summary>
    /// <param name="clearColor">Four RGBA clear components.</param>
    public void RenderOnce(ReadOnlySpan<float> clearColor) {
        Dispatcher.VerifyAccess();
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        var (currentContext, currentSwapChain, backBuffer) = BeginFrame(clearColor);
        EndFrame(currentContext, currentSwapChain, backBuffer);
    }

    /// <summary>
    ///     Records visible existing scene nodes into the current swap-chain back buffer and presents the frame.
    /// </summary>
    /// <param name="clearColor">Four RGBA clear components.</param>
    /// <param name="candidates">The opaque or transparent scene candidates in render order.</param>
    /// <param name="transforms">The current camera and viewport transforms.</param>
    /// <param name="testFrustum">Whether camera-frustum culling is enabled.</param>
    /// <param name="frustum">The current camera frustum.</param>
    /// <param name="lights">The optional shared light model.</param>
    /// <returns>The number of recorded draws.</returns>
    internal int RenderSceneOnce(
        ReadOnlySpan<float> clearColor,
        FastList<SceneNode> candidates,
        in GlobalTransformStruct transforms,
        bool testFrustum,
        ref BoundingFrustum frustum,
        LightsBufferModel? lights = null
    ) {
        Dispatcher.VerifyAccess();
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        var renderer = sceneRenderer
            ?? throw new InvalidOperationException("The Direct3D 12 scene renderer is unavailable.");
        var catalog = passCatalog
            ?? throw new InvalidOperationException("The Direct3D 12 pass catalog is unavailable.");
        var (currentContext, currentSwapChain, backBuffer) = BeginFrame(clearColor);
        var recorded = renderer.RenderVisible(currentContext,
            candidates,
            catalog.Resolve,
            in transforms,
            testFrustum,
            ref frustum,
            lights,
            boneSkinningPassSelector: catalog.ResolveBoneSkinning);
        EndFrame(currentContext, currentSwapChain, backBuffer);
        return recorded;
    }

    /// <summary>
    ///     Flattens existing viewport roots, applies the current camera, and presents one ordered scene frame.
    /// </summary>
    /// <param name="clearColor">Four RGBA clear components.</param>
    /// <param name="roots">The current viewport scene roots.</param>
    /// <param name="camera">The current viewport camera.</param>
    /// <param name="testFrustum">Whether camera-frustum culling is enabled.</param>
    /// <param name="enableShadows">Whether a visible shadow-map node may record a depth pass.</param>
    /// <param name="oitRenderType">The transparent composition mode.</param>
    /// <param name="oitDepthPeelingIterations">The positive dual-depth-peeling iteration count.</param>
    /// <param name="oitWeightPower">The weighted-OIT depth power.</param>
    /// <param name="oitWeightDepthSlope">The weighted-OIT depth slope.</param>
    /// <param name="oitWeightMode">The weighted-OIT equation selector.</param>
    /// <param name="fxaaLevel">The final full-screen anti-aliasing quality.</param>
    /// <param name="enableSsao">Whether screen-space ambient occlusion is evaluated.</param>
    /// <param name="ssaoRadius">The SSAO view-space sample radius.</param>
    /// <param name="ssaoIntensity">The SSAO intensity multiplier.</param>
    /// <param name="ssaoQuality">The SSAO target resolution.</param>
    /// <param name="roots2D">The optional Scene2D overlays drawn after post processing.</param>
    /// <returns>The number of recorded draws.</returns>
    internal int RenderViewportOnce(
        ReadOnlySpan<float> clearColor,
        IEnumerable<SceneNode> roots,
        CameraCore camera,
        bool testFrustum,
        bool enableShadows = true,
        OitRenderType oitRenderType = OitRenderType.None,
        int oitDepthPeelingIterations = 4,
        float oitWeightPower = 3,
        float oitWeightDepthSlope = 1,
        OitWeightMode oitWeightMode = OitWeightMode.Linear1,
        FxaaLevel fxaaLevel = FxaaLevel.None,
        bool enableSsao = false,
        float ssaoRadius = 0.5f,
        float ssaoIntensity = 1,
        SsaoQuality ssaoQuality = SsaoQuality.Low,
        IEnumerable<SceneNode2D>? roots2D = null,
        bool captureFrame = false
    ) {
        Dispatcher.VerifyAccess();
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        roots.GuardNotNull();
        camera.GuardNotNull();
        var renderer = sceneRenderer
            ?? throw new InvalidOperationException("The Direct3D 12 scene renderer is unavailable.");
        var catalog = passCatalog
            ?? throw new InvalidOperationException("The Direct3D 12 pass catalog is unavailable.");
        opaqueNodes.Clear();
        transparentNodes.Clear();
        volumeNodes.Clear();
        particleNodes.Clear();
        lightNodes.Clear();
        globalEffectNodes.Clear();
        postEffectNodes.Clear();
        screenSpacedNodes.Clear();
        TextureModel? environmentMap = null;
        ShadowMapNode? shadowNode = null;
        foreach (var node in roots.PreorderDft(node => node.Visible))
            if (node is ScreenSpacedNode screenSpaced) {
                screenSpacedNodes.Add(screenSpaced);
            } else if (FindScreenSpacedAncestor(node) is not null) {
                continue;
            } else if (node is EnvironmentMapNode environment) {
                environmentMap = environment.Texture;
            } else if (node is ShadowMapNode shadow) {
                shadowNode ??= shadow;
            } else if (node is VolumeTextureNode) {
                volumeNodes.Add(node);
            } else if (node is ParticleStormNode) {
                particleNodes.Add(node);
            } else
            switch (node.RenderType) {
                case RenderType.Light:
                    lightNodes.Add(node);
                    break;
                case RenderType.Opaque:
                    opaqueNodes.Add(node);
                    break;
                case RenderType.Transparent:
                    transparentNodes.Add(node);
                    break;
                case RenderType.GlobalEffect:
                    globalEffectNodes.Add(node);
                    break;
                case RenderType.PostEffect:
                    postEffectNodes.Add(node);
                    break;
            }

        SilkD3D12SceneRenderer.UpdateLights(lightNodes, lights);

        var transforms = CreateViewportTransforms(camera,
            PixelWidth,
            PixelHeight,
            (float) windowHost.DpiScale,
            (float) Stopwatch.GetTimestamp() / Stopwatch.Frequency,
            out var frustum);
        transforms.OITWeightPower = oitWeightPower;
        transforms.OITWeightDepthSlope = oitWeightDepthSlope;
        transforms.OITWeightMode = (int) oitWeightMode;
        transforms.SSAOEnabled = enableSsao ? 1u : 0u;
        transforms.SSAOBias = 1e-3f;
        transforms.SSAOIntensity = ssaoIntensity;
        var (currentContext, currentSwapChain, backBuffer) = BeginFrame(clearColor);
        renderer.BeginFrame();
        ShadowMapParamStruct? shadowParameters = null;
        if (enableShadows && shadowNode is not null &&
            shadowNode.TryCreateD3D12Parameters(lightNodes,
                opaqueNodes,
                out var currentShadowParameters,
                out var shadowFrustum)) {
            renderer.RenderShadows(currentContext,
                opaqueNodes,
                catalog.ResolveShadow,
                catalog.ResolveBoneSkinning,
                in transforms,
                in currentShadowParameters,
                ref shadowFrustum);
            var shadowDepthStencilView = depthStencilView
                ?? throw new InvalidOperationException("The depth/stencil view is unavailable.");
            currentContext.SetRenderTargets(currentSwapChain.CurrentRenderTargetView, shadowDepthStencilView);
            currentContext.SetViewport(currentSwapChain.Width, currentSwapChain.Height);
            shadowParameters = currentShadowParameters;
        }
        var currentDepthStencilView = depthStencilView
            ?? throw new InvalidOperationException("The depth/stencil view is unavailable.");
        SilkD3D12Resource? ssaoMap = null;
        if (enableSsao && opaqueNodes.Count > 0) {
            ssaoMap = renderer.RenderSsao(currentContext,
                opaqueNodes,
                catalog.ResolveSsaoGeometry,
                catalog.ResolvePostProcess(DefaultRenderTechniqueNames.Ssao, DefaultPassNames.Default),
                catalog.ResolvePostProcess(DefaultRenderTechniqueNames.Ssao, DefaultPassNames.EffectBlurHorizontal),
                catalog.ResolveBoneSkinning,
                in transforms,
                testFrustum,
                ref frustum,
                currentSwapChain.Width,
                currentSwapChain.Height,
                ssaoQuality,
                ssaoRadius);
            currentContext.SetRenderTargets(currentSwapChain.CurrentRenderTargetView, currentDepthStencilView);
            currentContext.SetViewport(currentSwapChain.Width, currentSwapChain.Height);
        }
        var recorded = renderer.RenderVisible(currentContext,
            opaqueNodes,
            catalog.Resolve,
            in transforms,
            testFrustum,
            ref frustum,
            lights,
            environmentMap,
            catalog.ResolveBoneSkinning,
            shadowParameters,
            ssaoMap,
            ssaoMap is null ? -1 : 31);
        recorded += renderer.RenderVolumes(currentContext,
            volumeNodes,
            opaqueNodes,
            catalog.ResolveVolumeBack,
            catalog.ResolveVolume,
            catalog.ResolveVolumePositions,
            catalog.ResolveBoneSkinning,
            in transforms,
            testFrustum,
            ref frustum,
            currentSwapChain.CurrentRenderTargetView,
            currentDepthStencilView,
            currentSwapChain.Width,
            currentSwapChain.Height);
        recorded += renderer.RenderParticles(currentContext,
            particleNodes,
            catalog.ResolveParticleUpdate,
            catalog.ResolveParticleInsert,
            catalog.ResolveParticle,
            in transforms,
            testFrustum,
            ref frustum);
        recorded += oitRenderType switch {
            OitRenderType.SinglePassWeighted => renderer.RenderWeightedTransparency(currentContext,
                transparentNodes,
                catalog.ResolveWeightedTransparency,
                catalog.ResolveWeightedComposition(),
                in transforms,
                testFrustum,
                ref frustum,
                currentSwapChain.CurrentRenderTargetView,
                currentDepthStencilView,
                currentSwapChain.Width,
                currentSwapChain.Height,
                lights,
                environmentMap,
                catalog.ResolveBoneSkinning,
                shadowParameters),
            OitRenderType.DepthPeeling => renderer.RenderDepthPeeling(currentContext,
                transparentNodes,
                catalog.ResolveDepthPeelingInitialization,
                catalog.ResolveDepthPeeling,
                catalog.ResolveDepthPeelingComposition(),
                oitDepthPeelingIterations,
                in transforms,
                testFrustum,
                ref frustum,
                backBuffer,
                currentSwapChain.CurrentRenderTargetView,
                currentDepthStencilView,
                currentSwapChain.Width,
                currentSwapChain.Height,
                lights,
                environmentMap,
                catalog.ResolveBoneSkinning,
                shadowParameters),
            _ => renderer.RenderVisible(currentContext,
                transparentNodes,
                catalog.Resolve,
                in transforms,
                testFrustum,
                ref frustum,
                lights,
                environmentMap,
                catalog.ResolveBoneSkinning,
                shadowParameters)
        };
        foreach (var effectNode in globalEffectNodes)
            if (effectNode is NodePostEffectBloom bloom) {
                var bloomParameters = new BorderEffectStruct {
                    Color = bloom.ThresholdColor,
                    Param = new Matrix {
                        M11 = bloom.BloomExtractIntensity,
                        M12 = bloom.BloomPassIntensity,
                        M13 = bloom.BloomCombineSaturation,
                        M14 = bloom.BloomCombineIntensity
                    }
                };
                renderer.RenderBloom(currentContext,
                    backBuffer,
                    currentSwapChain.CurrentRenderTargetView,
                    catalog.ResolvePostProcess(DefaultRenderTechniqueNames.PostEffectBloom,
                        DefaultPassNames.ScreenQuad),
                    catalog.ResolvePostProcess(DefaultRenderTechniqueNames.PostEffectBloom,
                        DefaultPassNames.EffectBlurVertical),
                    catalog.ResolvePostProcess(DefaultRenderTechniqueNames.PostEffectBloom,
                        DefaultPassNames.EffectBlurHorizontal),
                    catalog.ResolvePostProcess(DefaultRenderTechniqueNames.PostEffectBloom,
                        DefaultPassNames.MeshOutline),
                    in bloomParameters,
                    bloom.NumberOfBlurPass,
                    in transforms,
                    currentSwapChain.Width,
                    currentSwapChain.Height);
            }
        foreach (var effectNode in postEffectNodes)
            switch (effectNode) {
                case NodePostEffectMeshOutlineBlur outline: {
                    var technique = effectNode is NodePostEffectBorderHighlight
                        ? DefaultRenderTechniqueNames.PostEffectMeshBorderHighlight
                        : DefaultRenderTechniqueNames.PostEffectMeshOutlineBlur;
                    var effectName = outline.EffectName;
                    var outlineParameters = new BorderEffectStruct {
                        Color = outline.Color,
                        Param = new Matrix { M11 = outline.ScaleX, M12 = outline.ScaleY },
                        ViewportScale = 1
                    };
                    recorded += renderer.RenderOutline(currentContext,
                        opaqueNodes,
                        node => node.TryGetPostEffect(effectName, out _)
                            ? catalog.ResolvePostEffectGeometry(node,
                                DefaultPassNames.EffectOutlineP1,
                                DepthStencilFormat)
                            : null,
                        catalog.ResolvePostProcess(technique, DefaultPassNames.EffectBlurVertical),
                        catalog.ResolvePostProcess(technique, DefaultPassNames.EffectBlurHorizontal),
                        catalog.ResolvePostProcess(technique, DefaultPassNames.MeshOutline),
                        in outlineParameters,
                        outline.NumberOfBlurPass,
                        in transforms,
                        testFrustum,
                        ref frustum,
                        backBuffer,
                        currentSwapChain.CurrentRenderTargetView,
                        currentDepthStencilView,
                        currentSwapChain.Width,
                        currentSwapChain.Height);
                    break;
                }
                case NodePostEffectXRay xray: {
                    var effectName = xray.EffectName;
                    var xrayParameters = new BorderEffectStruct {
                        Color = xray.Color,
                        Param = new Matrix { M11 = xray.OutlineFadingFactor }
                    };
                    List<Func<SceneNode, HelixToolkit.SharpDX.Core.Shaders.ShaderPass?>> selectors = [];
                    if (xray.EnableDoublePass)
                        selectors.Add(node => node.TryGetPostEffect(effectName, out _)
                            ? catalog.ResolvePostEffectGeometry(node,
                                DefaultPassNames.EffectMeshXRayP1,
                                DepthStencilFormat)
                            : null);
                    selectors.Add(node => node.TryGetPostEffect(effectName, out _)
                        ? catalog.ResolvePostEffectGeometry(node,
                            DefaultPassNames.EffectMeshXRayP2,
                            DepthStencilFormat)
                        : null);
                    recorded += renderer.RenderXRay(currentContext,
                        opaqueNodes,
                        selectors,
                        in xrayParameters,
                        in transforms,
                        testFrustum,
                        ref frustum,
                        currentSwapChain.CurrentRenderTargetView,
                        currentDepthStencilView,
                        currentSwapChain.Width,
                        currentSwapChain.Height);
                    break;
                }
                case NodePostEffectXRayGrid grid: {
                    var effectName = grid.EffectName;
                    var gridParameters = new BorderEffectStruct {
                        Color = grid.Color,
                        Param = new Matrix {
                            M11 = grid.GridDensity,
                            M12 = grid.DimmingFactor,
                            M13 = grid.BlendingFactor
                        }
                    };
                    List<Func<SceneNode, HelixToolkit.SharpDX.Core.Shaders.ShaderPass?>> selectors = [
                        node => node.TryGetPostEffect(effectName, out _)
                            ? catalog.ResolvePostEffectGeometry(node,
                                DefaultPassNames.EffectMeshXRayGridP1,
                                DepthStencilFormat)
                            : null
                    ];
                    if (grid.UseDepthOcclusion)
                        selectors.Add(node => node.TryGetPostEffect(effectName, out _)
                            ? catalog.ResolvePostEffectGeometry(node,
                                DefaultPassNames.EffectMeshXRayGridP2,
                                DepthStencilFormat)
                            : null);
                    selectors.Add(node => node.TryGetPostEffect(effectName, out _)
                        ? catalog.ResolvePostEffectGeometry(node, grid.XRayDrawingPassName, DepthStencilFormat)
                        : null);
                    recorded += renderer.RenderXRay(currentContext,
                        opaqueNodes,
                        selectors,
                        in gridParameters,
                        in transforms,
                        testFrustum,
                        ref frustum,
                        currentSwapChain.CurrentRenderTargetView,
                        currentDepthStencilView,
                        currentSwapChain.Width,
                        currentSwapChain.Height);
                    break;
                }
            }
        recorded += RenderScreenSpaced(currentContext,
            renderer,
            catalog,
            camera,
            in transforms,
            currentSwapChain,
            currentDepthStencilView,
            lights,
            environmentMap);
        if (fxaaLevel != FxaaLevel.None)
            renderer.RenderFxaa(currentContext,
                backBuffer,
                currentSwapChain.CurrentRenderTargetView,
                catalog.ResolvePostProcess(DefaultRenderTechniqueNames.PostEffectFxaa, DefaultPassNames.LumaPass),
                catalog.ResolvePostProcess(DefaultRenderTechniqueNames.PostEffectFxaa, DefaultPassNames.FxaaPass),
                fxaaLevel,
                in transforms,
                currentSwapChain.Width,
                currentSwapChain.Height);
        if (roots2D is not null)
            recorded += renderer.Render2D(currentContext,
                roots2D,
                catalog.ResolveSprite2D(),
                currentSwapChain.CurrentRenderTargetView,
                currentSwapChain.Width,
                currentSwapChain.Height,
                (float) windowHost.DpiScale);
        SilkD3D12Resource? readback = null;
        PlacedSubresourceFootprint footprint = default;
        ulong totalBytes = 0;
        if (captureFrame) {
            var currentDevice = device
                ?? throw new InvalidOperationException("The Direct3D 12 device is unavailable.");
            footprint = currentDevice.GetCopyableFootprint(backBuffer, out totalBytes);
            readback = currentDevice.CreateBuffer(totalBytes, HeapType.Readback);
            currentContext.Transition(backBuffer, ResourceStates.CopySource);
            currentContext.CopyTextureToBuffer(readback, backBuffer, in footprint);
        }
        EndFrame(currentContext, currentSwapChain, backBuffer);
        if (readback is not null) {
            using (readback) {
                LastCapturedFrame = CreateCapturedFrame(readback,
                    in footprint,
                    totalBytes,
                    currentSwapChain.Width,
                    currentSwapChain.Height);
            }
        }
        return recorded;
    }

    /// <summary>
    ///     Renders each screen-spaced root with its own fixed camera and corner viewport.
    /// </summary>
    private int RenderScreenSpaced(SilkD3D12CommandContext context,
        SilkD3D12SceneRenderer renderer,
        D3D12ScenePassCatalog catalog,
        CameraCore camera,
        in GlobalTransformStruct mainTransform,
        SilkD3D12SwapChain currentSwapChain,
        SilkD3D12Descriptor currentDepthStencilView,
        LightsBufferModel lights,
        TextureModel? environmentMap) {
        var currentDepthStencilBuffer = depthStencilBuffer
            ?? throw new InvalidOperationException("The depth/stencil buffer is unavailable.");
        var recorded = 0;
        foreach (var root in screenSpacedNodes) {
            if (root.RenderCore is not ScreenSpacedMeshRenderCore screenCore ||
                !screenCore.TryCreateD3D12Transform(camera,
                    in mainTransform,
                    currentSwapChain.Width,
                    currentSwapChain.Height,
                    (float) windowHost.DpiScale,
                    out var screenTransform,
                    out var viewport))
                continue;

            screenSpacedOpaqueNodes.Clear();
            screenSpacedTransparentNodes.Clear();
            foreach (var node in root.Items.PreorderDft(node => node.Visible && node is not ScreenSpacedNode)) {
                if (node.RenderType == RenderType.Opaque) screenSpacedOpaqueNodes.Add(node);
                else if (node.RenderType == RenderType.Transparent) screenSpacedTransparentNodes.Add(node);
            }

            var scissorLeft = Math.Max(0, (int) viewport.X);
            var scissorTop = Math.Max(0, (int) viewport.Y);
            var scissorRight = Math.Min((int) currentSwapChain.Width,
                (int) Math.Ceiling(viewport.X + viewport.Width));
            var scissorBottom = Math.Min((int) currentSwapChain.Height,
                (int) Math.Ceiling(viewport.Y + viewport.Height));
            if (scissorRight <= scissorLeft || scissorBottom <= scissorTop) continue;
            context.ClearDepthStencil(currentDepthStencilBuffer, currentDepthStencilView);
            context.SetViewport(viewport.X, viewport.Y, viewport.Width, viewport.Height);
            context.SetScissorRectangle(scissorLeft, scissorTop, scissorRight, scissorBottom);
            var screenFrustum = new BoundingFrustum(screenTransform.ViewProjection);
            recorded += renderer.RenderVisible(context,
                screenSpacedOpaqueNodes,
                catalog.Resolve,
                in screenTransform,
                false,
                ref screenFrustum,
                lights,
                environmentMap,
                catalog.ResolveBoneSkinning);
            recorded += renderer.RenderVisible(context,
                screenSpacedTransparentNodes,
                catalog.Resolve,
                in screenTransform,
                false,
                ref screenFrustum,
                lights,
                environmentMap,
                catalog.ResolveBoneSkinning);
        }
        context.SetViewport(currentSwapChain.Width, currentSwapChain.Height);
        return recorded;
    }

    /// <summary>
    ///     Finds the screen-spaced group that owns a flattened scene node.
    /// </summary>
    private static ScreenSpacedNode? FindScreenSpacedAncestor(SceneNode node) {
        for (var parent = node.Parent; parent is not null; parent = parent.Parent)
            if (parent is ScreenSpacedNode screenSpaced)
                return screenSpaced;
        return null;
    }

    /// <summary>
    ///     Gets the last synchronously captured viewport frame.
    /// </summary>
    internal D3D12CapturedFrame? LastCapturedFrame { get; private set; }

    /// <summary>
    ///     Removes native row padding and converts the swap-chain RGBA bytes to WPF BGRA bytes.
    /// </summary>
    /// <param name="readback">The completed readback buffer.</param>
    /// <param name="footprint">The native copy footprint.</param>
    /// <param name="totalBytes">The complete readback allocation size.</param>
    /// <param name="width">The frame width.</param>
    /// <param name="height">The frame height.</param>
    /// <returns>The tightly packed WPF frame.</returns>
    private static D3D12CapturedFrame CreateCapturedFrame(SilkD3D12Resource readback,
        in PlacedSubresourceFootprint footprint,
        ulong totalBytes,
        uint width,
        uint height) => CreateCapturedFrame(readback.Read(checked((int) totalBytes)),
        footprint.Offset,
        footprint.Footprint.RowPitch,
        width,
        height);

    /// <summary>
    ///     Converts a padded RGBA readback allocation to tightly packed WPF BGRA pixels.
    /// </summary>
    /// <param name="padded">The complete native readback allocation.</param>
    /// <param name="offset">The first texture byte in the allocation.</param>
    /// <param name="rowPitch">The padded native row size.</param>
    /// <param name="width">The frame width.</param>
    /// <param name="height">The frame height.</param>
    /// <returns>The tightly packed WPF frame.</returns>
    internal static D3D12CapturedFrame CreateCapturedFrame(byte[] padded,
        ulong offset,
        uint rowPitch,
        uint width,
        uint height) {
        padded.GuardNotNull();
        var rowBytes = checked((int) width * 4);
        var pixels = new byte[checked(rowBytes * (int) height)];
        for (var row = 0; row < height; row++) {
            var sourceOffset = checked((int) offset + (int) row * (int) rowPitch);
            var destinationOffset = checked((int) row * rowBytes);
            padded.AsSpan(sourceOffset, rowBytes).CopyTo(pixels.AsSpan(destinationOffset, rowBytes));
        }
        for (var index = 0; index < pixels.Length; index += 4)
            (pixels[index], pixels[index + 2]) = (pixels[index + 2], pixels[index]);
        return new D3D12CapturedFrame(width, height, pixels);
    }

    /// <summary>
    ///     Creates the renderer constant-buffer values and culling frustum for one physical viewport.
    /// </summary>
    /// <param name="camera">The active camera.</param>
    /// <param name="width">The physical viewport width.</param>
    /// <param name="height">The physical viewport height.</param>
    /// <param name="dpiScale">The WPF-to-physical-pixel scale.</param>
    /// <param name="timeStamp">The frame timestamp in seconds.</param>
    /// <param name="frustum">The resulting camera frustum.</param>
    /// <returns>The complete global transform payload.</returns>
    internal static GlobalTransformStruct CreateViewportTransforms(
        CameraCore camera,
        uint width,
        uint height,
        float dpiScale,
        float timeStamp,
        out BoundingFrustum frustum
    ) {
        camera.GuardNotNull();
        if (width == 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height == 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (!float.IsFinite(dpiScale) || dpiScale <= 0) throw new ArgumentOutOfRangeException(nameof(dpiScale));
        var aspectRatio = width / (float) height;
        var view = camera.CreateViewMatrix();
        var projection = camera.CreateProjectionMatrix(aspectRatio);
        var viewProjection = view * projection;
        var cameraParameters = camera.CreateCameraParams(aspectRatio);
        frustum = new BoundingFrustum(viewProjection);
        return new GlobalTransformStruct {
            View = view,
            Projection = projection,
            ViewProjection = viewProjection,
            Frustum = new Vector4(cameraParameters.Fov,
                cameraParameters.AspectRatio,
                cameraParameters.ZNear,
                cameraParameters.ZFar),
            Viewport = new Vector4(width, height, 1f / width, 1f / height),
            Resolution = new Vector4(width, height, 1f / width, 1f / height),
            EyePos = cameraParameters.Position,
            IsPerspective = !frustum.IsOrthographic,
            TimeStamp = timeStamp,
            DpiScale = dpiScale
        };
    }

    /// <inheritdoc />
    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo) {
        base.OnRenderSizeChanged(sizeInfo);
        QueueResize();
    }

    /// <summary>
    ///     Releases all native and WPF-host resources.
    /// </summary>
    public void Dispose() {
        if (IsDisposed) return;

        Loaded -= OnLoaded;
        Unloaded -= OnUnloaded;
        windowHost.HandleCreated -= OnHandleCreated;
        windowHost.HandleDestroyed -= OnHandleDestroyed;
        windowHost.DpiScaleChanged -= OnDpiScaleChanged;
        if (resizeOperation is { Status: DispatcherOperationStatus.Pending }) resizeOperation.Abort();
        StopNative();
        windowHost.Dispose();
        IsDisposed = true;
    }

    /// <summary>
    ///     Starts native presentation when WPF has attached the surface.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="eventArgs">The event arguments.</param>
    private void OnLoaded(object sender, RoutedEventArgs eventArgs) => StartNative();

    /// <summary>
    ///     Stops native presentation when WPF detaches the surface.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="eventArgs">The event arguments.</param>
    private void OnUnloaded(object sender, RoutedEventArgs eventArgs) => StopNative();

    /// <summary>
    ///     Starts native presentation as soon as the child HWND exists.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="eventArgs">The event arguments.</param>
    private void OnHandleCreated(object? sender, EventArgs eventArgs) => StartNative();

    /// <summary>
    ///     Stops native presentation before the child HWND is destroyed.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="eventArgs">The event arguments.</param>
    private void OnHandleDestroyed(object? sender, EventArgs eventArgs) => StopNative();

    /// <summary>
    ///     Queues a physical resize when WPF changes DPI.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="dpiScale">The new DPI scale.</param>
    private void OnDpiScaleChanged(object? sender, double dpiScale) => QueueResize();

    /// <summary>
    ///     Creates the device, queue, command context, fence, and swap chain once.
    /// </summary>
    private void StartNative() {
        Dispatcher.VerifyAccess();
        if (IsDisposed || swapChain is not null || !windowHost.IsHandleCreated) return;

        var size = D3D12PresentationSize.Calculate(ActualWidth, ActualHeight, windowHost.DpiScale);
        try {
            device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, driverType);
            commandQueue = device.CreateCommandQueue();
            commandContext = device.CreateCommandContext();
            fence = device.CreateFence();
            swapChain = new SilkD3D12SwapChain(device,
                commandQueue,
                windowHost.NativeHandle,
                size.Width,
                size.Height);
            depthStencilHeap = device.CreateDescriptorHeap(DescriptorHeapType.Dsv, 1);
            depthStencilView = depthStencilHeap.Allocate();
            RecreateDepthStencil(size.Width, size.Height);
            resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 16_384, true);
            samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 2_048, true);
            sceneRenderer = new SilkD3D12SceneRenderer(device, resourceHeap, samplerHeap);
            passCatalog = new D3D12ScenePassCatalog(device,
                swapChain.Format,
                DepthStencilFormat,
                (effectsManager as EffectsManager)?.TechniqueDescriptions,
                effectsManager);
        } catch {
            StopNative();
            throw;
        }
    }

    /// <summary>
    ///     Waits for queued GPU work and releases the presentation stack in dependency order.
    /// </summary>
    private void StopNative() {
        if (commandQueue is not null && fence is not null) {
            var fenceValue = commandQueue.Signal(fence);
            fence.Wait(fenceValue, TimeSpan.FromSeconds(5));
        }

        passCatalog?.Dispose();
        sceneRenderer?.Dispose();
        samplerHeap?.Dispose();
        resourceHeap?.Dispose();
        depthStencilBuffer?.Dispose();
        depthStencilView?.Dispose();
        depthStencilHeap?.Dispose();
        swapChain?.Dispose();
        commandContext?.Dispose();
        fence?.Dispose();
        commandQueue?.Dispose();
        device?.Dispose();
        passCatalog = null;
        sceneRenderer = null;
        samplerHeap = null;
        resourceHeap = null;
        depthStencilBuffer = null;
        depthStencilView = null;
        depthStencilHeap = null;
        swapChain = null;
        commandContext = null;
        fence = null;
        commandQueue = null;
        device = null;
    }

    /// <summary>
    ///     Resets the command context, binds the current back buffer, and clears it.
    /// </summary>
    /// <param name="clearColor">Four RGBA clear components.</param>
    /// <returns>The open context, current swap chain, and render-target back buffer.</returns>
    private (SilkD3D12CommandContext Context, SilkD3D12SwapChain SwapChain, SilkD3D12Resource BackBuffer)
        BeginFrame(ReadOnlySpan<float> clearColor) {
        var currentSwapChain = swapChain
            ?? throw new InvalidOperationException("The presentation surface is not attached to a window.");
        var currentContext = commandContext
            ?? throw new InvalidOperationException("The command context is unavailable.");
        var backBuffer = currentSwapChain.CurrentBackBuffer;
        var currentDepthStencilBuffer = depthStencilBuffer
            ?? throw new InvalidOperationException("The depth/stencil buffer is unavailable.");
        var currentDepthStencilView = depthStencilView
            ?? throw new InvalidOperationException("The depth/stencil view is unavailable.");
        currentContext.Reset();
        currentContext.Transition(backBuffer, ResourceStates.RenderTarget);
        currentContext.ClearRenderTarget(backBuffer, currentSwapChain.CurrentRenderTargetView, clearColor);
        currentContext.ClearDepthStencil(currentDepthStencilBuffer, currentDepthStencilView);
        currentContext.SetRenderTargets(currentSwapChain.CurrentRenderTargetView, currentDepthStencilView);
        currentContext.SetViewport(currentSwapChain.Width, currentSwapChain.Height);
        return (currentContext, currentSwapChain, backBuffer);
    }

    /// <summary>
    ///     Transitions, submits, presents, and waits for one completed frame.
    /// </summary>
    /// <param name="currentContext">The open command context.</param>
    /// <param name="currentSwapChain">The current swap chain.</param>
    /// <param name="backBuffer">The current back buffer.</param>
    private void EndFrame(
        SilkD3D12CommandContext currentContext,
        SilkD3D12SwapChain currentSwapChain,
        SilkD3D12Resource backBuffer
    ) {
        var currentQueue = commandQueue
            ?? throw new InvalidOperationException("The command queue is unavailable.");
        var currentFence = fence
            ?? throw new InvalidOperationException("The presentation fence is unavailable.");
        currentContext.Transition(backBuffer, ResourceStates.Present);
        currentContext.Close();
        currentQueue.Execute(currentContext);
        currentSwapChain.Present(0);
        var fenceValue = currentQueue.Signal(currentFence);
        currentFence.Wait(fenceValue, TimeSpan.FromSeconds(5));
        PresentedFrames++;
    }

    /// <summary>
    ///     Coalesces WPF size and DPI changes into one background-priority native resize.
    /// </summary>
    private void QueueResize() {
        if (IsDisposed) return;

        var size = D3D12PresentationSize.Calculate(ActualWidth, ActualHeight, windowHost.DpiScale);
        if (!resizeQueue.Request(size.Width, size.Height)) return;

        resizeOperation = Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action) ApplyPendingResize);
    }

    /// <summary>
    ///     Applies the newest pending physical size after waiting for outstanding GPU work.
    /// </summary>
    private void ApplyPendingResize() {
        if (!resizeQueue.TryTake(out var size) || swapChain is null || commandQueue is null || fence is null)
            return;
        if (swapChain.Width == size.Width && swapChain.Height == size.Height) return;

        var fenceValue = commandQueue.Signal(fence);
        fence.Wait(fenceValue, TimeSpan.FromSeconds(5));
        swapChain.Resize(size.Width, size.Height);
        RecreateDepthStencil(size.Width, size.Height);
    }

    /// <summary>
    ///     Replaces the depth/stencil texture after initial attachment or a completed swap-chain resize.
    /// </summary>
    /// <param name="width">The physical width in pixels.</param>
    /// <param name="height">The physical height in pixels.</param>
    private void RecreateDepthStencil(uint width, uint height) {
        var currentDevice = device ?? throw new InvalidOperationException("The Direct3D 12 device is unavailable.");
        var currentView = depthStencilView
            ?? throw new InvalidOperationException("The depth/stencil descriptor is unavailable.");
        var replacement = currentDevice.CreateDepthStencilTexture2D(width, height, DepthStencilFormat);
        try {
            currentDevice.CreateDepthStencilView(replacement, currentView);
        } catch {
            replacement.Dispose();
            throw;
        }

        depthStencilBuffer?.Dispose();
        depthStencilBuffer = replacement;
    }
}

/// <summary>
///     Contains one tightly packed BGRA viewport frame for WPF bitmap encoding.
/// </summary>
/// <param name="Width">The frame width.</param>
/// <param name="Height">The frame height.</param>
/// <param name="Pixels">The tightly packed BGRA pixels.</param>
internal readonly record struct D3D12CapturedFrame(uint Width, uint Height, byte[] Pixels);

/// <summary>
///     Retains only the newest pending presentation size and requests at most one dispatcher operation.
/// </summary>
internal sealed class D3D12ResizeQueue {
    private (uint Width, uint Height)? pending;

    /// <summary>
    ///     Stores the newest size.
    /// </summary>
    /// <param name="width">The width in pixels.</param>
    /// <param name="height">The height in pixels.</param>
    /// <returns><see langword="true" /> when a dispatcher operation must be scheduled.</returns>
    internal bool Request(uint width, uint height) {
        if (width == 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height == 0) throw new ArgumentOutOfRangeException(nameof(height));

        var schedule = pending is null;
        pending = (width, height);
        return schedule;
    }

    /// <summary>
    ///     Removes the newest pending size.
    /// </summary>
    /// <param name="size">The pending size, if present.</param>
    /// <returns><see langword="true" /> when a size was pending.</returns>
    internal bool TryTake(out (uint Width, uint Height) size) {
        if (pending is not { } value) {
            size = default;
            return false;
        }

        pending = null;
        size = value;
        return true;
    }
}

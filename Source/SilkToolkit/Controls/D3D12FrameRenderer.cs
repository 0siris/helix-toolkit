/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Cameras;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model.Scene2D;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Shaders;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace HelixToolkit.Wpf.SharpDX.Controls;

/// <summary>Prepares scenes and records ordered native passes into a borrowed color target.</summary>
/// <remarks>
/// The caller owns command recording and submission and must complete preceding GPU work before recording,
/// resizing, changing the effects manager, or disposing this renderer. Targets and devices are borrowed.
/// </remarks>
internal sealed class D3D12FrameRenderer : IDisposable {
    /// <summary>The depth/stencil format shared by all recorded graphics pipelines.</summary>
    internal const Format DepthStencilFormat = Format.FormatD32FloatS8X24Uint;

    /// <summary>The borrowed device used for native resource creation.</summary>
    private readonly SilkD3D12Device device;

    /// <summary>The color format fixed for this renderer's graphics pipelines.</summary>
    private readonly Format colorFormat;

    /// <summary>The immutable color and coverage contract used by this presentation lifetime.</summary>
    private readonly D3D12ColorOutputMode outputMode;

    /// <summary>The borrowed manager used by original scene attachment and Scene2D hooks.</summary>
    private IEffectsManager? effectsManager;

    /// <summary>The owned shader-visible resource descriptor heap.</summary>
    private SilkD3D12DescriptorHeap? resourceHeap;

    /// <summary>The owned shader-visible sampler descriptor heap.</summary>
    private SilkD3D12DescriptorHeap? samplerHeap;

    /// <summary>The productive scene renderer and native resource caches.</summary>
    private SilkD3D12SceneRenderer? sceneRenderer;

    /// <summary>The current owned pass catalog.</summary>
    private D3D12ScenePassCatalog? passCatalog;

    /// <summary>The reusable explicit stack for screen-spaced subtree traversal.</summary>
    private readonly Stack<SceneNode> screenSpacedTraversal = new();

    /// <summary>The Resolve selector cached for the current catalog instance.</summary>
    private Func<SceneNode, ShaderPass?> resolvePass = null!;

    /// <summary>The ResolveBoneSkinning selector cached for the current catalog instance.</summary>
    private Func<SceneNode, ShaderPass?> resolveBoneSkinningPass = null!;

    /// <summary>The ResolveShadow selector cached for the current catalog instance.</summary>
    private Func<SceneNode, ShaderPass?> resolveShadowPass = null!;

    /// <summary>The ResolveSsaoGeometry selector cached for the current catalog instance.</summary>
    private Func<SceneNode, ShaderPass?> resolveSsaoGeometryPass = null!;

    /// <summary>The ResolveWireframe selector cached for the current catalog instance.</summary>
    private Func<SceneNode, ShaderPass?> resolveWireframePass = null!;

    /// <summary>The ResolveVolumeBack selector cached for the current catalog instance.</summary>
    private Func<SceneNode, ShaderPass?> resolveVolumeBackPass = null!;

    /// <summary>The ResolveVolume selector cached for the current catalog instance.</summary>
    private Func<SceneNode, ShaderPass?> resolveVolumePass = null!;

    /// <summary>The ResolveVolumePositions selector cached for the current catalog instance.</summary>
    private Func<SceneNode, ShaderPass?> resolveVolumePositionsPass = null!;

    /// <summary>The ResolveParticleUpdate selector cached for the current catalog instance.</summary>
    private Func<SceneNode, ShaderPass?> resolveParticleUpdatePass = null!;

    /// <summary>The ResolveParticleInsert selector cached for the current catalog instance.</summary>
    private Func<SceneNode, ShaderPass?> resolveParticleInsertPass = null!;

    /// <summary>The ResolveParticle selector cached for the current catalog instance.</summary>
    private Func<SceneNode, ShaderPass?> resolveParticlePass = null!;

    /// <summary>The ResolveWeightedTransparency selector cached for the current catalog instance.</summary>
    private Func<SceneNode, ShaderPass?> resolveWeightedTransparencyPass = null!;

    /// <summary>The ResolveDepthPeelingInitialization selector cached for the current catalog instance.</summary>
    private Func<SceneNode, ShaderPass?> resolveDepthPeelingInitializationPass = null!;

    /// <summary>The ResolveDepthPeeling selector cached for the current catalog instance.</summary>
    private Func<SceneNode, ShaderPass?> resolveDepthPeelingPass = null!;

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

    /// <summary>Reused post-effect candidates for the main scene or current screen-spaced root in traversal order.</summary>
    private readonly FastList<SceneNode> postEffectCandidates = [];

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

    /// <summary>Gets the native scene owner used by every recording entry point.</summary>
    internal SilkD3D12SceneRenderer SceneRenderer => sceneRenderer
                                                     ?? throw new ObjectDisposedException(nameof(D3D12FrameRenderer));

    /// <summary>Gets the actual current pipeline catalog.</summary>
    internal D3D12ScenePassCatalog PassCatalog => passCatalog
                                                  ?? throw new ObjectDisposedException(nameof(D3D12FrameRenderer));

    /// <summary>Gets the actual shader-visible resource heap.</summary>
    internal SilkD3D12DescriptorHeap ResourceHeap => resourceHeap
                                                     ?? throw new ObjectDisposedException(nameof(D3D12FrameRenderer));

    /// <summary>Gets the actual shader-visible sampler heap.</summary>
    internal SilkD3D12DescriptorHeap SamplerHeap => samplerHeap
                                                    ?? throw new ObjectDisposedException(nameof(D3D12FrameRenderer));

    /// <summary>Gets the actual depth/stencil descriptor heap.</summary>
    internal SilkD3D12DescriptorHeap? DepthStencilHeap => depthStencilHeap;

    /// <summary>Gets the actual depth/stencil descriptor.</summary>
    internal SilkD3D12Descriptor? DepthStencilView => depthStencilView;

    /// <summary>Gets the size-dependent depth/stencil resource.</summary>
    internal SilkD3D12Resource? DepthStencilBuffer => depthStencilBuffer;

    /// <summary>Gets the update context retained across unchanged camera, size, and DPI frames.</summary>
    internal RenderContext? RenderContext { get; private set; }

    /// <summary>Gets the Scene2D context retained across unchanged manager, size, and DPI frames.</summary>
    internal RenderContext2D? RenderContext2D { get; private set; }

    /// <summary>Gets the hierarchy produced by actual scene preparation.</summary>
    internal FastList<KeyValuePair<int, SceneNode>> PerFrameFlattenedScene => SceneRenderer.PerFrameFlattenedScene;

    /// <summary>Gets whether the owned scene resources have been released.</summary>
    internal bool IsDisposed { get; private set; }

    /// <summary>Creates the common native resource owners without retaining a presentation host.</summary>
    /// <param name="device">The borrowed device, retained by the caller through GPU completion and disposal.</param>
    /// <param name="colorFormat">The color format used by all borrowed targets and graphics pipelines.</param>
    /// <param name="effectsManager">The optional borrowed scene effects manager.</param>
    /// <param name="outputMode">The immutable compositing contract; legacy callers retain existing behavior.</param>
    internal D3D12FrameRenderer(
        SilkD3D12Device device,
        Format colorFormat,
        IEffectsManager? effectsManager = null,
        D3D12ColorOutputMode outputMode = D3D12ColorOutputMode.Legacy
    ) {
        device.AsGuardNotNull();
        if (colorFormat == Format.FormatUnknown) throw new ArgumentOutOfRangeException(nameof(colorFormat));
        this.device = device;
        this.colorFormat = colorFormat;
        this.outputMode = outputMode;
        try {
            depthStencilHeap = device.CreateDescriptorHeap(DescriptorHeapType.Dsv, 1);
            depthStencilView = depthStencilHeap.Allocate();
            resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 16_384, true);
            samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 2_048, true);
            sceneRenderer = new SilkD3D12SceneRenderer(device, resourceHeap, samplerHeap, colorFormat, outputMode);
            SetEffectsManager(effectsManager);
        } catch {
            Dispose();
            throw;
        }
    }

    /// <summary>Replaces the pass catalog and cached selectors after the caller completes preceding GPU work.</summary>
    /// <param name="value">The optional borrowed scene effects manager.</param>
    internal void SetEffectsManager(IEffectsManager? value) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (passCatalog is not null && ReferenceEquals(effectsManager, value)) return;
        var replacement = new D3D12ScenePassCatalog(
            device, colorFormat, DepthStencilFormat,
            (value as EffectsManager)?.TechniqueDescriptions, value, outputMode
        );
        var previous = passCatalog;
        passCatalog = replacement;
        effectsManager = value;
        resolvePass = replacement.Resolve;
        resolveBoneSkinningPass = replacement.ResolveBoneSkinning;
        resolveShadowPass = replacement.ResolveShadow;
        resolveSsaoGeometryPass = replacement.ResolveSsaoGeometry;
        resolveWireframePass = replacement.ResolveWireframe;
        resolveVolumeBackPass = replacement.ResolveVolumeBack;
        resolveVolumePass = replacement.ResolveVolume;
        resolveVolumePositionsPass = replacement.ResolveVolumePositions;
        resolveParticleUpdatePass = replacement.ResolveParticleUpdate;
        resolveParticleInsertPass = replacement.ResolveParticleInsert;
        resolveParticlePass = replacement.ResolveParticle;
        resolveWeightedTransparencyPass = replacement.ResolveWeightedTransparency;
        resolveDepthPeelingInitializationPass = replacement.ResolveDepthPeelingInitialization;
        resolveDepthPeelingPass = replacement.ResolveDepthPeeling;
        previous?.Dispose();
        RenderContext2D?.Dispose();
        RenderContext2D = null;
    }

    /// <summary>Clears and binds the borrowed target and shared depth on an already open command context.</summary>
    /// <param name="context">The caller-owned open command context.</param>
    /// <param name="target">The borrowed physical color target.</param>
    /// <param name="clearColor">Four RGBA clear components.</param>
    internal void RecordClearFrame(
        SilkD3D12CommandContext context,
        in D3D12FrameTarget target,
        ReadOnlySpan<float> clearColor
    ) {
        ValidateTarget(in target);
        context.AsGuardNotNull();
        if (clearColor.Length != 4) throw new ArgumentException("Four RGBA components are required.", nameof(clearColor));
        Resize(target.Width, target.Height);
        context.Transition(target.ColorResource, ResourceStates.RenderTarget);
        if (outputMode == D3D12ColorOutputMode.Premultiplied) {
            var alpha = clearColor[3];
            ReadOnlySpan<float> premultiplied = [
                clearColor[0] * alpha, clearColor[1] * alpha,
                clearColor[2] * alpha, alpha
            ];
            context.ClearRenderTarget(target.ColorResource, target.RenderTargetView, premultiplied);
        } else {
            context.ClearRenderTarget(target.ColorResource, target.RenderTargetView, clearColor);
        }

        context.ClearDepthStencil(depthStencilBuffer!, depthStencilView!);
        context.SetRenderTargets(target.RenderTargetView, depthStencilView!);
        context.SetViewport(target.Width, target.Height);
    }

    /// <summary>Clears the shared depth buffer and records a supplied list of scene candidates.</summary>
    /// <param name="context">The caller-owned open command context.</param>
    /// <param name="target">The borrowed physical color target.</param>
    /// <param name="clearColor">Four RGBA clear components.</param>
    /// <param name="candidates">The candidate list in draw order.</param>
    /// <param name="transforms">The physical camera and viewport transforms, including explicit DPI.</param>
    /// <param name="testFrustum">Whether camera-frustum culling is enabled.</param>
    /// <param name="frustum">The current camera frustum.</param>
    /// <param name="lights">The optional light payload.</param>
    /// <returns>The number of recorded native draws.</returns>
    internal int RecordSceneFrame(
        SilkD3D12CommandContext context,
        in D3D12FrameTarget target,
        ReadOnlySpan<float> clearColor,
        FastList<SceneNode> candidates,
        in GlobalTransformStruct transforms,
        bool testFrustum,
        ref BoundingFrustum frustum,
        LightsBufferModel? lights = null
    ) {
        RecordClearFrame(context, in target, clearColor);
        SceneRenderer.BeginFrame();
        return SceneRenderer.RenderVisible(
            context, candidates, resolvePass, in transforms,
            testFrustum, ref frustum, lights, boneSkinningPassSelector: resolveBoneSkinningPass
        );
    }

    /// <summary>
    ///     Prepares viewport roots and records the complete ordered scene into a borrowed target.
    /// </summary>
    /// <param name="currentContext">The caller-owned open command context.</param>
    /// <param name="target">The borrowed physical color target.</param>
    /// <param name="dpiScale">The explicit logical-to-physical-pixel scale.</param>
    /// <param name="renderHost">The actual scene consumer passed to original attachment hooks.</param>
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
    /// <param name="autoUpdateOctree">Whether existing hit-testable dirty geometry updates its octree.</param>
    /// <param name="enableRenderOrder">Whether the original render-order keys sort the relevant draw lists.</param>
    /// <returns>The number of recorded draws.</returns>
    /// <exception cref="ObjectDisposedException">The common renderer has been disposed.</exception>
    /// <exception cref="ArgumentException">The borrowed target is incompatible with this renderer.</exception>
    internal int RecordViewportFrame(
        SilkD3D12CommandContext currentContext,
        in D3D12FrameTarget target,
        float dpiScale,
        ReadOnlySpan<float> clearColor,
        IEnumerable<SceneNode> roots,
        CameraCore camera,
        bool testFrustum,
        bool enableShadows = true,
        OITRenderType oitRenderType = OITRenderType.None,
        int oitDepthPeelingIterations = 4,
        float oitWeightPower = 3,
        float oitWeightDepthSlope = 1,
        OITWeightMode oitWeightMode = OITWeightMode.Linear1,
        FXAALevel fxaaLevel = FXAALevel.None,
        bool enableSsao = false,
        float ssaoRadius = 0.5f,
        float ssaoIntensity = 1,
        SSAOQuality ssaoQuality = SSAOQuality.Low,
        IEnumerable<SceneNode2D>? roots2D = null,
        bool autoUpdateOctree = true,
        bool enableRenderOrder = false,
        IRenderHost? renderHost = null
    ) {
        ValidateTarget(in target);
        currentContext.AsGuardNotNull();
        roots.AsGuardNotNull();
        camera.AsGuardNotNull();
        if (!float.IsFinite(dpiScale) || dpiScale <= 0) throw new ArgumentOutOfRangeException(nameof(dpiScale));
        var renderer = SceneRenderer;
        var catalog = PassCatalog;
        opaqueNodes.Clear();
        transparentNodes.Clear();
        postEffectCandidates.Clear();
        volumeNodes.Clear();
        particleNodes.Clear();
        lightNodes.Clear();
        globalEffectNodes.Clear();
        postEffectNodes.Clear();
        screenSpacedNodes.Clear();
        TextureModel? environmentMap = null;
        ShadowMapNode? shadowNode = null;
        var width = target.Width;
        var height = target.Height;
        if (RenderContext is not { } previous || !ReferenceEquals(previous.Camera, camera) ||
            previous.ActualWidth != width || previous.ActualHeight != height || previous.DpiScale != dpiScale) {
            RenderContext?.Dispose();
            RenderContext = new RenderContext(camera, width, height, dpiScale);
        }

        var updateContext = RenderContext;
        updateContext.RestoreGlobalTransform();
        updateContext.EnableBoundingFrustum = testFrustum;
        updateContext.AutoUpdateOctree = autoUpdateOctree;
        updateContext.TimeStamp = TimeSpan.FromSeconds((double) Stopwatch.GetTimestamp() / Stopwatch.Frequency);
        var canContinue = (renderHost as Viewport3DX)?.ScenePreparationPermission;
        var preparedNodes = renderer.PrepareScene(roots, updateContext, effectsManager, renderHost, canContinue);
        if (canContinue?.Invoke() == false) return 0;
        foreach (var node in preparedNodes) {
            if (FindScreenSpacedAncestor(node) is not null) continue;
            if (node is ScreenSpacedNode screenSpaced) {
                screenSpacedNodes.Add(screenSpaced);
            } else if (node is EnvironmentMapNode environment) {
                environmentMap = environment.Texture;
            } else if (node is ShadowMapNode shadow) {
                shadowNode ??= shadow;
            } else if (node is VolumeTextureNode) {
                volumeNodes.Add(node);
            } else if (node is ParticleStormNode) {
                particleNodes.Add(node);
            } else {
                switch (node.RenderType) {
                    case RenderType.Light:
                        lightNodes.Add(node);
                        break;
                    case RenderType.Opaque:
                        opaqueNodes.Add(node);
                        postEffectCandidates.Add(node);
                        break;
                    case RenderType.Transparent:
                        transparentNodes.Add(node);
                        postEffectCandidates.Add(node);
                        break;
                    case RenderType.GlobalEffect:
                        globalEffectNodes.Add(node);
                        break;
                    case RenderType.PostEffect:
                        postEffectNodes.Add(node);
                        break;
                }
            }
        }

        if (enableRenderOrder) {
            foreach (var node in opaqueNodes) node.UpdateRenderOrderKey();
            opaqueNodes.Sort();
            foreach (var node in postEffectNodes) node.UpdateRenderOrderKey();
            postEffectNodes.Sort();
            foreach (var node in particleNodes) node.UpdateRenderOrderKey();
            particleNodes.Sort();
        }

        SilkD3D12SceneRenderer.UpdateLights(lightNodes, lights);

        var transforms = CreateViewportTransforms(
            camera,
            target.Width,
            target.Height,
            dpiScale,
            (float) Stopwatch.GetTimestamp() / Stopwatch.Frequency,
            out var frustum
        );
        transforms.OITWeightPower = oitWeightPower;
        transforms.OITWeightDepthSlope = oitWeightDepthSlope;
        transforms.OITWeightMode = (int) oitWeightMode;
        transforms.SSAOEnabled = enableSsao
            ? 1u
            : 0u;
        transforms.SSAOBias = 1e-3f;
        transforms.SSAOIntensity = ssaoIntensity;
        RecordClearFrame(currentContext, in target, clearColor);
        var backBuffer = target.ColorResource;
        renderer.BeginFrame();
        ShadowMapParamStruct? shadowParameters = null;
        if (enableShadows && shadowNode is not null &&
            shadowNode.TryCreateD3D12Parameters(
                lightNodes,
                opaqueNodes,
                out var currentShadowParameters,
                out var shadowFrustum
            )) {
            renderer.RenderShadows(
                currentContext,
                opaqueNodes,
                resolveShadowPass,
                resolveBoneSkinningPass,
                in transforms,
                in currentShadowParameters,
                ref shadowFrustum
            );
            var shadowDepthStencilView = depthStencilView
                                         ?? throw new InvalidOperationException("The depth/stencil view is unavailable.");
            currentContext.SetRenderTargets(target.RenderTargetView, shadowDepthStencilView);
            currentContext.SetViewport(target.Width, target.Height);
            shadowParameters = currentShadowParameters;
        }

        var currentDepthStencilView = depthStencilView
                                      ?? throw new InvalidOperationException("The depth/stencil view is unavailable.");
        SilkD3D12Resource? ssaoMap = null;
        if (enableSsao && opaqueNodes.Count > 0) {
            ssaoMap = renderer.RenderSsao(
                currentContext,
                opaqueNodes,
                resolveSsaoGeometryPass,
                catalog.ResolvePostProcess(DefaultRenderTechniqueNames.Ssao, DefaultPassNames.Default),
                catalog.ResolvePostProcess(DefaultRenderTechniqueNames.Ssao, DefaultPassNames.EffectBlurHorizontal),
                resolveBoneSkinningPass,
                in transforms,
                testFrustum,
                ref frustum,
                target.Width,
                target.Height,
                ssaoQuality,
                ssaoRadius
            );
            currentContext.SetRenderTargets(target.RenderTargetView, currentDepthStencilView);
            currentContext.SetViewport(target.Width, target.Height);
        }

        int recorded;
        {
            using var opaqueProfile = D3D12Profiler.Begin(currentContext, "Silk.Opaque");
            recorded = renderer.RenderVisible(
                currentContext,
                opaqueNodes,
                resolvePass,
                in transforms,
                testFrustum,
                ref frustum,
                lights,
                environmentMap,
                resolveBoneSkinningPass,
                shadowParameters,
                ssaoMap,
                ssaoMap is null
                    ? -1
                    : 31
            );
        }
        recorded += renderer.RenderVisible(
            currentContext,
            opaqueNodes,
            resolveWireframePass,
            in transforms,
            testFrustum,
            ref frustum,
            boneSkinningPassSelector: resolveBoneSkinningPass
        );
        recorded += renderer.RenderVolumes(
            currentContext,
            volumeNodes,
            opaqueNodes,
            resolveVolumeBackPass,
            resolveVolumePass,
            resolveVolumePositionsPass,
            resolveBoneSkinningPass,
            in transforms,
            testFrustum,
            ref frustum,
            target.RenderTargetView,
            currentDepthStencilView,
            target.Width,
            target.Height
        );
        recorded += renderer.RenderParticles(
            currentContext,
            particleNodes,
            resolveParticleUpdatePass,
            resolveParticleInsertPass,
            resolveParticlePass,
            in transforms,
            testFrustum,
            ref frustum
        );
        recorded += oitRenderType switch {
            OITRenderType.SinglePassWeighted => renderer.RenderWeightedTransparency(
                currentContext,
                transparentNodes,
                resolveWeightedTransparencyPass,
                catalog.ResolveWeightedComposition(),
                in transforms,
                testFrustum,
                ref frustum,
                target.RenderTargetView,
                currentDepthStencilView,
                target.Width,
                target.Height,
                lights,
                environmentMap,
                resolveBoneSkinningPass,
                shadowParameters
            ),
            OITRenderType.DepthPeeling => renderer.RenderDepthPeeling(
                currentContext,
                transparentNodes,
                resolveDepthPeelingInitializationPass,
                resolveDepthPeelingPass,
                catalog.ResolveDepthPeelingComposition(),
                oitDepthPeelingIterations,
                in transforms,
                testFrustum,
                ref frustum,
                backBuffer,
                target.RenderTargetView,
                currentDepthStencilView,
                target.Width,
                target.Height,
                lights,
                environmentMap,
                resolveBoneSkinningPass,
                shadowParameters
            ),
            _ => renderer.RenderVisible(
                currentContext,
                transparentNodes,
                resolvePass,
                in transforms,
                testFrustum,
                ref frustum,
                lights,
                environmentMap,
                resolveBoneSkinningPass,
                shadowParameters
            )
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
                renderer.RenderBloom(
                    currentContext,
                    backBuffer,
                    target.RenderTargetView,
                    catalog.ResolvePostProcess(
                        DefaultRenderTechniqueNames.PostEffectBloom,
                        DefaultPassNames.ScreenQuad
                    ),
                    catalog.ResolvePostProcess(
                        DefaultRenderTechniqueNames.PostEffectBloom,
                        DefaultPassNames.EffectBlurVertical
                    ),
                    catalog.ResolvePostProcess(
                        DefaultRenderTechniqueNames.PostEffectBloom,
                        DefaultPassNames.EffectBlurHorizontal
                    ),
                    catalog.ResolvePostProcess(
                        DefaultRenderTechniqueNames.PostEffectBloom,
                        DefaultPassNames.MeshOutline
                    ),
                    in bloomParameters,
                    bloom.NumberOfBlurPass,
                    in transforms,
                    target.Width,
                    target.Height
                );
            }

        recorded += renderer.RenderPostEffects(
            currentContext, postEffectCandidates, postEffectNodes,
            catalog, in transforms, testFrustum, ref frustum, backBuffer,
            target.RenderTargetView,
            depthStencilBuffer ?? throw new InvalidOperationException("The depth/stencil resource is unavailable"),
            currentDepthStencilView, target.Width, target.Height
        );
        recorded += RenderScreenSpaced(
            currentContext,
            renderer,
            camera,
            in transforms,
            in target,
            dpiScale,
            currentDepthStencilView,
            lights,
            environmentMap
        );
        if (fxaaLevel != FXAALevel.None)
            renderer.RenderFxaa(
                currentContext,
                backBuffer,
                target.RenderTargetView,
                catalog.ResolvePostProcess(DefaultRenderTechniqueNames.PostEffectFxaa, DefaultPassNames.LumaPass),
                catalog.ResolvePostProcess(DefaultRenderTechniqueNames.PostEffectFxaa, DefaultPassNames.FxaaPass),
                fxaaLevel,
                in transforms,
                target.Width,
                target.Height
            );
        if (roots2D is not null) {
            if (canContinue?.Invoke() == false) return 0;
            var context2D = GetRenderContext2D(in target, dpiScale);
            try {
                recorded += renderer.Render2D(
                    currentContext,
                    roots2D,
                    catalog.ResolveSprite2D(),
                    target.RenderTargetView,
                    target.Width,
                    target.Height,
                    dpiScale,
                    context2D,
                    canContinue
                );
            } catch {
                // Original update hooks can leave mutable context stacks unbalanced when they throw.
                // A failed context must never be reused by the next frame.
                RenderContext2D = null;
                if (context2D is not null) {
                    try {
                        if (context2D.HasTarget) context2D.DeviceContext.EndDraw();
                    } finally {
                        if (context2D.HasTarget) context2D.DeviceContext.Target = null;
                        context2D.Dispose();
                    }
                }

                throw;
            }
        }

        if (canContinue?.Invoke() == false) return 0;
        D3D12Profiler.ReportDescriptors(renderer.SceneBindings, renderer.EffectBindings, ResourceHeap, SamplerHeap);
        return recorded;
    }

    /// <summary>Records screen-spaced roots with their fixed cameras and corner viewports.</summary>
    /// <param name="context">The open command context.</param>
    /// <param name="renderer">The shared native scene renderer.</param>
    /// <param name="camera">The main viewport camera.</param>
    /// <param name="mainTransform">The main viewport transforms.</param>
    /// <param name="target">The borrowed physical color target.</param>
    /// <param name="dpiScale">The explicit physical-pixel scale.</param>
    /// <param name="currentDepthStencilView">The shared depth/stencil descriptor.</param>
    /// <param name="lights">The shared light payload.</param>
    /// <param name="environmentMap">The optional scene environment map.</param>
    /// <returns>The number of recorded screen-spaced draws.</returns>
    private int RenderScreenSpaced(
        SilkD3D12CommandContext context,
        SilkD3D12SceneRenderer renderer,
        CameraCore camera,
        in GlobalTransformStruct mainTransform,
        in D3D12FrameTarget target,
        float dpiScale,
        SilkD3D12Descriptor currentDepthStencilView,
        LightsBufferModel lights,
        TextureModel? environmentMap
    ) {
        var currentDepthStencilBuffer = depthStencilBuffer
                                        ?? throw new InvalidOperationException("The depth/stencil buffer is unavailable.");
        var recorded = 0;
        foreach (var root in screenSpacedNodes) {
            if (root.RenderCore is not ScreenSpacedMeshRenderCore screenCore ||
                !screenCore.TryCreateD3D12Transform(
                    camera,
                    in mainTransform,
                    target.Width,
                    target.Height,
                    dpiScale,
                    out var screenTransform,
                    out var viewport
                ))
                continue;

            screenSpacedOpaqueNodes.Clear();
            screenSpacedTransparentNodes.Clear();
            postEffectCandidates.Clear();
            CollectScreenSpacedNodes(root);

            var scissorLeft = Math.Max(0, (int) viewport.X);
            var scissorTop = Math.Max(0, (int) viewport.Y);
            var scissorRight = Math.Min(
                (int) target.Width,
                (int) Math.Ceiling(viewport.X + viewport.Width)
            );
            var scissorBottom = Math.Min(
                (int) target.Height,
                (int) Math.Ceiling(viewport.Y + viewport.Height)
            );
            if (scissorRight <= scissorLeft || scissorBottom <= scissorTop) continue;
            context.ClearDepthStencil(currentDepthStencilBuffer, currentDepthStencilView);
            context.SetViewport(viewport.X, viewport.Y, viewport.Width, viewport.Height);
            context.SetScissorRectangle(scissorLeft, scissorTop, scissorRight, scissorBottom);
            var screenFrustum = new BoundingFrustum(screenTransform.ViewProjection);
            recorded += renderer.RenderVisible(
                context,
                screenSpacedOpaqueNodes,
                resolvePass,
                in screenTransform,
                false,
                ref screenFrustum,
                lights,
                environmentMap,
                resolveBoneSkinningPass
            );
            recorded += renderer.RenderVisible(
                context,
                screenSpacedTransparentNodes,
                resolvePass,
                in screenTransform,
                false,
                ref screenFrustum,
                lights,
                environmentMap,
                resolveBoneSkinningPass
            );
            if (postEffectCandidates.Count > 0 && postEffectNodes.Count > 0)
                recorded += renderer.RenderPostEffects(
                    context, postEffectCandidates, postEffectNodes,
                    PassCatalog, in screenTransform, false, ref screenFrustum, target.ColorResource,
                    target.RenderTargetView, currentDepthStencilBuffer, currentDepthStencilView,
                    target.Width, target.Height, geometryViewport: viewport
                );
        }

        context.SetViewport(target.Width, target.Height);
        return recorded;
    }

    /// <summary>Finds the screen-spaced group that owns a flattened scene node.</summary>
    /// <param name="node">The flattened scene node.</param>
    /// <returns>The owning screen-spaced ancestor, or null for an ordinary node.</returns>
    private static ScreenSpacedNode? FindScreenSpacedAncestor(SceneNode node) {
        for (var parent = node.Parent; parent is not null; parent = parent.Parent)
            if (parent is ScreenSpacedNode screenSpaced)
                return screenSpaced;
        return null;
    }

    /// <summary>Classifies visible descendants without allocating traversal iterators or delegates.</summary>
    /// <param name="root">The screen-spaced subtree root.</param>
    private void CollectScreenSpacedNodes(ScreenSpacedNode root) {
        screenSpacedTraversal.Clear();
        for (var index = root.Items.Count - 1; index >= 0; index--)
            screenSpacedTraversal.Push(root.Items[index]);
        while (screenSpacedTraversal.TryPop(out var node)) {
            if (!node.Visible || node is ScreenSpacedNode) continue;
            if (node.RenderType == RenderType.Opaque) screenSpacedOpaqueNodes.Add(node);
            else if (node.RenderType == RenderType.Transparent) screenSpacedTransparentNodes.Add(node);
            if (node.HasAnyPostEffect) postEffectCandidates.Add(node);
            for (var index = node.Items.Count - 1; index >= 0; index--)
                screenSpacedTraversal.Push(node.Items[index]);
        }
    }

    /// <summary>Gets or replaces the layout context only when its immutable dimensions or DPI change.</summary>
    /// <param name="target">The borrowed physical target.</param>
    /// <param name="dpiScale">The explicit physical-pixel scale.</param>
    /// <returns>The retained context, or null when no effects manager supplies Scene2D resources.</returns>
    private RenderContext2D? GetRenderContext2D(in D3D12FrameTarget target, float dpiScale) {
        if (effectsManager is null) return null;
        if (RenderContext2D is not { } previous || previous.ActualWidth != target.Width ||
            previous.ActualHeight != target.Height || previous.DpiScale != dpiScale) {
            RenderContext2D?.Dispose();
            RenderContext2D = new RenderContext2D(
                effectsManager.DeviceContext2D, effectsManager,
                target.Width, target.Height, dpiScale
            );
        }

        return RenderContext2D;
    }

    /// <summary>Rejects incompatible targets before replacing resources or preparing the scene.</summary>
    /// <param name="target">The borrowed target and its declared physical metadata.</param>
    private void ValidateTarget(in D3D12FrameTarget target) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        target.ColorResource.AsGuardNotNull();
        target.RenderTargetView.AsGuardNotNull();
        ObjectDisposedException.ThrowIf(target.ColorResource.IsDisposed, target.ColorResource);
        ObjectDisposedException.ThrowIf(target.RenderTargetView.IsDisposed, target.RenderTargetView);
        if (target.Width == 0 || target.Height == 0)
            throw new ArgumentOutOfRangeException(nameof(target), "Target dimensions must be nonzero.");
        var description = target.ColorResource.Description;
        if (target.Format != colorFormat || description.Format != colorFormat)
            throw new ArgumentException("The target format must match the renderer color format.", nameof(target));
        if (description.Dimension != ResourceDimension.Texture2D || description.Width != target.Width ||
            description.Height != target.Height || description.SampleDesc.Count != 1 ||
            (description.Flags & ResourceFlags.AllowRenderTarget) == 0 ||
            target.RenderTargetView.Type != DescriptorHeapType.Rtv)
            throw new ArgumentException(
                "The target must describe its actual single-sample render texture and RTV.",
                nameof(target)
            );
    }

    /// <summary>Replaces depth resources only after the caller has completed preceding GPU work.</summary>
    /// <param name="width">The nonzero physical width.</param>
    /// <param name="height">The nonzero physical height.</param>
    internal void Resize(uint width, uint height) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (width == 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height == 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (depthStencilBuffer is { } current && current.Description.Width == width &&
            current.Description.Height == height) return;
        var replacement = device.CreateDepthStencilTexture2D(width, height, DepthStencilFormat);
        try {
            device.CreateDepthStencilView(replacement, depthStencilView!);
        } catch {
            replacement.Dispose();
            throw;
        }

        depthStencilBuffer?.Dispose();
        depthStencilBuffer = replacement;
    }

    /// <summary>Releases owned native resources after the caller has completed every recorded GPU use.</summary>
    public void Dispose() {
        if (IsDisposed) return;
        passCatalog?.Dispose();
        sceneRenderer?.Dispose();
        RenderContext?.Dispose();
        RenderContext = null;
        RenderContext2D?.Dispose();
        RenderContext2D = null;
        samplerHeap?.Dispose();
        resourceHeap?.Dispose();
        depthStencilBuffer?.Dispose();
        depthStencilView?.Dispose();
        depthStencilHeap?.Dispose();
        passCatalog = null;
        sceneRenderer = null;
        samplerHeap = null;
        resourceHeap = null;
        depthStencilBuffer = null;
        depthStencilView = null;
        depthStencilHeap = null;
        IsDisposed = true;
    }

    /// <summary>Creates global transforms and the frustum for a physical viewport and explicit DPI.</summary>
    /// <param name="camera">The active camera.</param>
    /// <param name="width">The physical width.</param>
    /// <param name="height">The physical height.</param>
    /// <param name="dpiScale">The physical-pixel scale.</param>
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
        camera.AsGuardNotNull();
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
            Frustum = new Vector4(
                cameraParameters.Fov,
                cameraParameters.AspectRatio,
                cameraParameters.ZNear,
                cameraParameters.ZFar
            ),
            Viewport = new Vector4(width, height, 1f / width, 1f / height),
            Resolution = new Vector4(width, height, 1f / width, 1f / height),
            EyePos = cameraParameters.Position,
            IsPerspective = !frustum.IsOrthographic,
            TimeStamp = timeStamp,
            DpiScale = dpiScale
        };
    }
}
/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Extensions;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Collection;
using HelixToolkit.SharpDX.Core.Model.Lights;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Model.Scene.Lights;
using HelixToolkit.SharpDX.Core.Model.Scene2D.Abstract;
using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core.Native;

/// <summary>
///     Records visible existing geometry scene nodes through the productive Direct3D 12 render-core paths.
/// </summary>
internal sealed class SilkD3D12SceneRenderer : IDisposable {
    /// <summary>
    ///     The shared shader-visible resource heap.
    /// </summary>
    private readonly SilkD3D12DescriptorHeap resourceHeap;

    /// <summary>
    ///     The shared shader-visible sampler heap.
    /// </summary>
    private readonly SilkD3D12DescriptorHeap samplerHeap;

    /// <summary>
    ///     Host-lifetime native geometry and texture resources.
    /// </summary>
    private readonly SilkD3D12ResourceManager resources;

    /// <summary>
    ///     Native overlay renderer sharing this host's texture cache and descriptor heaps.
    /// </summary>
    private readonly D3D12Scene2DRenderer scene2DRenderer;

    /// <summary>
    ///     Per-core mesh constant buffers and descriptor tables.
    /// </summary>
    private readonly Dictionary<MeshRenderCore, SilkD3D12MeshBindings> meshBindings =
        new(ReferenceEqualityComparer.Instance);

    /// <summary>
    ///     Per-core point, line, and billboard constant buffers and descriptor tables.
    /// </summary>
    private readonly Dictionary<PointLineRenderCore, SilkD3D12PointLineBindings> pointLineBindings =
        new(ReferenceEqualityComparer.Instance);

    /// <summary>
    ///     Per-core volume constant buffers and descriptor tables.
    /// </summary>
    private readonly Dictionary<VolumeRenderCore, SilkD3D12VolumeBindings> volumeBindings =
        new(ReferenceEqualityComparer.Instance);

    /// <summary>
    ///     Per-core particle simulation and indirect-draw resources.
    /// </summary>
    private readonly Dictionary<ParticleRenderCore, SilkD3D12ParticleResources> particleResources =
        new(ReferenceEqualityComparer.Instance);

    /// <summary>
    ///     Per-core desktop frame uploads and screen-pass bindings.
    /// </summary>
    private readonly Dictionary<ScreenCloneRenderCore, SilkD3D12ScreenCaptureResources> screenCaptureResources =
        new(ReferenceEqualityComparer.Instance);

    /// <summary>
    ///     Nodes attached by this renderer and detached with it.
    /// </summary>
    private readonly HashSet<SceneNode> attachedNodes = new(ReferenceEqualityComparer.Instance);

    /// <summary>
    ///     Reused visible-node list for allocation-free per-frame selection.
    /// </summary>
    private readonly FastList<SceneNode> visibleNodes = [];

    /// <summary>
    ///     The host-lifetime size-adjustable shadow depth resource.
    /// </summary>
    private SilkD3D12ShadowMap? shadowMap;

    /// <summary>
    ///     The shared immutable volume cube and size-dependent offscreen targets.
    /// </summary>
    private SilkD3D12VolumeResources? volumeResources;

    /// <summary>
    ///     Shared size-dependent transparency targets.
    /// </summary>
    private SilkD3D12TransparencyResources? transparencyResources;

    /// <summary>
    ///     Shared full-resolution ping-pong targets for post-processing.
    /// </summary>
    private SilkD3D12PostProcessResources? postProcessResources;

    /// <summary>
    ///     Shared size-dependent SSAO resources.
    /// </summary>
    private SilkD3D12SsaoResources? ssaoResources;

    /// <summary>
    ///     Initializes one scene renderer over host-owned descriptor heaps.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="resourceHeap">The shader-visible CBV/SRV/UAV heap.</param>
    /// <param name="samplerHeap">The shader-visible sampler heap.</param>
    internal SilkD3D12SceneRenderer(
        SilkD3D12Device device,
        SilkD3D12DescriptorHeap resourceHeap,
        SilkD3D12DescriptorHeap samplerHeap
    ) {
        device.GuardNotNull();
        resourceHeap.GuardNotNull();
        samplerHeap.GuardNotNull();
        this.resourceHeap = resourceHeap;
        this.samplerHeap = samplerHeap;
        resources = new SilkD3D12ResourceManager(device, resourceHeap);
        scene2DRenderer = new D3D12Scene2DRenderer(device,
            resources,
            resourceHeap,
            samplerHeap);
        Device = device;
    }

    /// <summary>
    ///     Gets the Direct3D 12 device used to allocate per-core bindings.
    /// </summary>
    private SilkD3D12Device Device { get; }

    /// <summary>
    ///     Gets the number of nodes selected by the most recent camera-frustum pass.
    /// </summary>
    internal int VisibleCount => visibleNodes.Count;

    /// <summary>
    ///     Gets the completed shadow depth resource for focused state and readback verification.
    /// </summary>
    internal SilkD3D12Resource? ShadowResource => shadowMap?.Resource;

    /// <summary>
    ///     Gets the completed volume back-position resource for focused state and readback verification.
    /// </summary>
    internal SilkD3D12Resource? VolumeBackPositions => volumeResources?.BackPositions;

    /// <summary>
    ///     Gets the current native particle resources for focused counter and lifecycle verification.
    /// </summary>
    /// <param name="core">The existing particle core.</param>
    /// <returns>The current resources, or <see langword="null" /> before the first particle frame.</returns>
    internal SilkD3D12ParticleResources? FindParticleResources(ParticleRenderCore core) =>
        particleResources.GetValueOrDefault(core);

    /// <summary>
    ///     Gets whether this renderer has released its bindings and shared resources.
    /// </summary>
    internal bool IsDisposed { get; private set; }

    /// <summary>
    ///     Recycles descriptor slots only after the presentation host has completed the preceding frame fence.
    /// </summary>
    internal void BeginFrame() {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        postProcessResources?.BeginFrame();
        scene2DRenderer.BeginFrame();
    }

    /// <summary>
    ///     Records existing Scene2D overlays after the completed 3D and post-processing frame.
    /// </summary>
    /// <param name="context">The open command context.</param>
    /// <param name="roots">The existing Scene2D roots.</param>
    /// <param name="pass">The existing Sprite2D pass.</param>
    /// <param name="target">The presentation render-target descriptor.</param>
    /// <param name="width">The physical target width.</param>
    /// <param name="height">The physical target height.</param>
    /// <param name="dpiScale">The physical-pixel scale.</param>
    /// <returns>The number of recorded 2D draws.</returns>
    internal int Render2D(
        SilkD3D12CommandContext context,
        IEnumerable<SceneNode2D> roots,
        ShaderPass pass,
        SilkD3D12Descriptor target,
        uint width,
        uint height,
        float dpiScale
    ) {
        scene2DRenderer.Prepare(roots, width, height, dpiScale);
        return scene2DRenderer.RenderPrepared(context, pass, target, width, height);
    }

    /// <summary>
    ///     Gets the native 2D renderer for focused cache, ordering, and lifecycle verification.
    /// </summary>
    internal D3D12Scene2DRenderer Scene2DRenderer => scene2DRenderer;

    /// <summary>
    ///     Runs the existing luma and FXAA passes against the presentation color.
    /// </summary>
    /// <param name="context">The open command context.</param>
    /// <param name="mainColor">The presentation color resource.</param>
    /// <param name="mainRenderTarget">The presentation RTV.</param>
    /// <param name="lumaPass">The existing luma pass.</param>
    /// <param name="fxaaPass">The existing FXAA pass.</param>
    /// <param name="level">The requested quality level.</param>
    /// <param name="transforms">The current global constants.</param>
    /// <param name="width">The physical width.</param>
    /// <param name="height">The physical height.</param>
    internal void RenderFxaa(
        SilkD3D12CommandContext context,
        SilkD3D12Resource mainColor,
        SilkD3D12Descriptor mainRenderTarget,
        ShaderPass lumaPass,
        ShaderPass fxaaPass,
        FxaaLevel level,
        in GlobalTransformStruct transforms,
        uint width,
        uint height
    ) {
        if (level == FxaaLevel.None) return;
        postProcessResources ??= new SilkD3D12PostProcessResources(Device, resourceHeap, samplerHeap);
        var parameters = CreateFxaaParameters(level, width, height);
        postProcessResources.Capture(context, mainColor, width, height);
        postProcessResources.Draw(context, lumaPass, 0, 1, in transforms, in parameters);
        context.Transition(mainColor, Silk.NET.Direct3D12.ResourceStates.RenderTarget);
        postProcessResources.DrawToPresentation(context,
            fxaaPass,
            1,
            mainRenderTarget,
            in transforms,
            in parameters);
    }

    /// <summary>
    ///     Extracts, blurs, and additively combines bloom through the existing full-screen passes.
    /// </summary>
    /// <param name="context">The open command context.</param>
    /// <param name="mainColor">The presentation color resource.</param>
    /// <param name="mainRenderTarget">The presentation RTV.</param>
    /// <param name="extractPass">The bloom extraction pass.</param>
    /// <param name="verticalBlurPass">The vertical blur pass.</param>
    /// <param name="horizontalBlurPass">The horizontal blur pass.</param>
    /// <param name="combinePass">The additive bloom composition pass.</param>
    /// <param name="parameters">The existing b4 bloom constants.</param>
    /// <param name="blurPasses">The non-negative number of blur pairs.</param>
    /// <param name="transforms">The current global constants.</param>
    /// <param name="width">The physical width.</param>
    /// <param name="height">The physical height.</param>
    internal void RenderBloom(
        SilkD3D12CommandContext context,
        SilkD3D12Resource mainColor,
        SilkD3D12Descriptor mainRenderTarget,
        ShaderPass extractPass,
        ShaderPass verticalBlurPass,
        ShaderPass horizontalBlurPass,
        ShaderPass combinePass,
        in BorderEffectStruct parameters,
        int blurPasses,
        in GlobalTransformStruct transforms,
        uint width,
        uint height
    ) {
        ArgumentOutOfRangeException.ThrowIfNegative(blurPasses);
        postProcessResources ??= new SilkD3D12PostProcessResources(Device, resourceHeap, samplerHeap);
        postProcessResources.Capture(context, mainColor, width, height);
        postProcessResources.Draw(context, extractPass, 0, 1, in transforms, in parameters);
        var source = 1;
        for (var index = 0; index < blurPasses; index++) {
            postProcessResources.Draw(context, verticalBlurPass, source, 1 - source, in transforms, in parameters);
            source = 1 - source;
            postProcessResources.Draw(context, horizontalBlurPass, source, 1 - source, in transforms, in parameters);
            source = 1 - source;
        }
        context.Transition(mainColor, Silk.NET.Direct3D12.ResourceStates.RenderTarget);
        postProcessResources.DrawToPresentation(context,
            combinePass,
            source,
            mainRenderTarget,
            in transforms,
            in parameters);
    }

    /// <summary>
    ///     Creates the exact b4 quality constants used by the existing FXAA shader.
    /// </summary>
    /// <param name="level">The requested quality level.</param>
    /// <param name="width">The non-zero target width.</param>
    /// <param name="height">The non-zero target height.</param>
    /// <returns>The complete FXAA constants.</returns>
    internal static BorderEffectStruct CreateFxaaParameters(FxaaLevel level, uint width, uint height) {
        ArgumentOutOfRangeException.ThrowIfZero(width);
        ArgumentOutOfRangeException.ThrowIfZero(height);
        var result = new BorderEffectStruct { Color = new Color4(1f / width, 1f / height, 0, 0) };
        (result.Param.M11, result.Param.M12, result.Param.M13) = level switch {
            FxaaLevel.Low => (0.25f, 0.250f, 0.0833f),
            FxaaLevel.Medium => (0.50f, 0.166f, 0.0625f),
            FxaaLevel.High => (0.75f, 0.125f, 0.0625f),
            FxaaLevel.Ultra => (1.00f, 0.063f, 0.0312f),
            _ => throw new ArgumentOutOfRangeException(nameof(level))
        };
        return result;
    }

    /// <summary>
    ///     Records opaque normal/depth geometry and evaluates the existing SSAO passes.
    /// </summary>
    /// <param name="context">The open command context.</param>
    /// <param name="nodes">The opaque scene candidates.</param>
    /// <param name="geometryPassSelector">Selects the existing SSAO geometry pass.</param>
    /// <param name="ssaoPass">The full-screen SSAO evaluation pass.</param>
    /// <param name="blurPass">The full-screen SSAO blur pass.</param>
    /// <param name="boneSkinningPassSelector">Selects optional bone precompute passes.</param>
    /// <param name="transforms">The current global constants.</param>
    /// <param name="testFrustum">Whether camera-frustum culling is enabled.</param>
    /// <param name="frustum">The current camera frustum.</param>
    /// <param name="width">The physical target width.</param>
    /// <param name="height">The physical target height.</param>
    /// <param name="quality">The SSAO resolution quality.</param>
    /// <param name="radius">The view-space sample radius.</param>
    /// <returns>The completed shader-readable occlusion map.</returns>
    internal SilkD3D12Resource RenderSsao(
        SilkD3D12CommandContext context,
        FastList<SceneNode> nodes,
        Func<SceneNode, ShaderPass?> geometryPassSelector,
        ShaderPass ssaoPass,
        ShaderPass blurPass,
        Func<SceneNode, ShaderPass?> boneSkinningPassSelector,
        in GlobalTransformStruct transforms,
        bool testFrustum,
        ref BoundingFrustum frustum,
        uint width,
        uint height,
        SsaoQuality quality,
        float radius
    ) {
        var scale = quality == SsaoQuality.High ? 1 : 2;
        var targetWidth = Math.Max(1u, width / (uint)scale);
        var targetHeight = Math.Max(1u, height / (uint)scale);
        ssaoResources ??= new SilkD3D12SsaoResources(Device, resourceHeap, samplerHeap);
        ssaoResources.BeginGeometry(context, targetWidth, targetHeight);
        _ = RenderVisible(context,
            nodes,
            geometryPassSelector,
            in transforms,
            testFrustum,
            ref frustum,
            boneSkinningPassSelector: boneSkinningPassSelector);
        return ssaoResources.Complete(context, ssaoPass, blurPass, in transforms, radius, scale);
    }

    /// <summary>
    ///     Renders matching geometry into a mask, blurs it, and composites an outline onto the presentation target.
    /// </summary>
    /// <param name="context">The open command context.</param>
    /// <param name="nodes">Scene geometry carrying the selected effect.</param>
    /// <param name="maskPassSelector">Selects the existing outline mask pass.</param>
    /// <param name="verticalBlurPass">The vertical blur pass.</param>
    /// <param name="horizontalBlurPass">The horizontal blur pass.</param>
    /// <param name="compositionPass">The final outline composition pass.</param>
    /// <param name="parameters">The b6 outline constants.</param>
    /// <param name="blurPasses">The non-negative number of blur pairs.</param>
    /// <param name="transforms">The current global constants.</param>
    /// <param name="testFrustum">Whether camera-frustum culling is enabled.</param>
    /// <param name="frustum">The current camera frustum.</param>
    /// <param name="mainColor">The presentation color resource.</param>
    /// <param name="mainRenderTarget">The presentation RTV.</param>
    /// <param name="mainDepthStencil">The presentation DSV.</param>
    /// <param name="width">The physical width.</param>
    /// <param name="height">The physical height.</param>
    /// <returns>The number of recorded outline-mask draws.</returns>
    internal int RenderOutline(
        SilkD3D12CommandContext context,
        FastList<SceneNode> nodes,
        Func<SceneNode, ShaderPass?> maskPassSelector,
        ShaderPass verticalBlurPass,
        ShaderPass horizontalBlurPass,
        ShaderPass compositionPass,
        in BorderEffectStruct parameters,
        int blurPasses,
        in GlobalTransformStruct transforms,
        bool testFrustum,
        ref BoundingFrustum frustum,
        SilkD3D12Resource mainColor,
        SilkD3D12Descriptor mainRenderTarget,
        SilkD3D12Descriptor mainDepthStencil,
        uint width,
        uint height
    ) {
        ArgumentOutOfRangeException.ThrowIfNegative(blurPasses);
        postProcessResources ??= new SilkD3D12PostProcessResources(Device, resourceHeap, samplerHeap);
        _ = postProcessResources.BeginTarget(context, 0, width, height, mainDepthStencil);
        var recorded = RenderVisible(context,
            nodes,
            maskPassSelector,
            in transforms,
            testFrustum,
            ref frustum,
            effectParameters: parameters);
        var source = 0;
        for (var index = 0; index < Math.Max(1, blurPasses); index++) {
            postProcessResources.Draw(context, verticalBlurPass, source, 1 - source, in transforms, in parameters);
            source = 1 - source;
            postProcessResources.Draw(context, horizontalBlurPass, source, 1 - source, in transforms, in parameters);
            source = 1 - source;
        }
        context.Transition(mainColor, Silk.NET.Direct3D12.ResourceStates.RenderTarget);
        postProcessResources.DrawToPresentation(context,
            compositionPass,
            source,
            mainRenderTarget,
            in transforms,
            in parameters);
        context.SetRenderTargets(mainRenderTarget, mainDepthStencil);
        return recorded;
    }

    /// <summary>
    ///     Records an ordered set of existing XRay custom geometry passes directly into the presentation target.
    /// </summary>
    /// <param name="context">The open command context.</param>
    /// <param name="nodes">Scene geometry carrying the selected effect.</param>
    /// <param name="passSelectors">The ordered custom-pass selectors.</param>
    /// <param name="parameters">The b6 XRay constants.</param>
    /// <param name="transforms">The current global constants.</param>
    /// <param name="testFrustum">Whether camera-frustum culling is enabled.</param>
    /// <param name="frustum">The current camera frustum.</param>
    /// <param name="mainRenderTarget">The presentation RTV.</param>
    /// <param name="mainDepthStencil">The presentation DSV.</param>
    /// <param name="width">The physical width.</param>
    /// <param name="height">The physical height.</param>
    /// <returns>The number of custom-pass geometry draws.</returns>
    internal int RenderXRay(
        SilkD3D12CommandContext context,
        FastList<SceneNode> nodes,
        IReadOnlyList<Func<SceneNode, ShaderPass?>> passSelectors,
        in BorderEffectStruct parameters,
        in GlobalTransformStruct transforms,
        bool testFrustum,
        ref BoundingFrustum frustum,
        SilkD3D12Descriptor mainRenderTarget,
        SilkD3D12Descriptor mainDepthStencil,
        uint width,
        uint height
    ) {
        var recorded = 0;
        foreach (var selector in passSelectors) {
            context.SetRenderTargets(mainRenderTarget, mainDepthStencil);
            context.SetViewport(width, height);
            recorded += RenderVisible(context,
                nodes,
                selector,
                in transforms,
                testFrustum,
                ref frustum,
                effectParameters: parameters);
        }
        return recorded;
    }

    /// <summary>
    ///     Selects, attaches, and records one opaque or transparent scene-node list.
    /// </summary>
    /// <param name="context">The open Direct3D 12 command context.</param>
    /// <param name="candidates">The renderable scene candidates in pass order.</param>
    /// <param name="passSelector">Selects the native pass for each scene node.</param>
    /// <param name="transforms">The current camera and viewport transforms.</param>
    /// <param name="testFrustum">Whether camera-frustum culling is enabled.</param>
    /// <param name="frustum">The current camera frustum.</param>
    /// <param name="lights">The optional shared light model.</param>
    /// <param name="environmentMap">The optional existing environment cube map.</param>
    /// <param name="boneSkinningPassSelector">The optional bone-skinning precompute-pass selector.</param>
    /// <param name="shadowParameters">The optional completed shadow-map payload.</param>
    /// <param name="passTexture">The optional texture used by a custom pass.</param>
    /// <param name="passTextureRegister">The custom pass texture register.</param>
    /// <param name="effectParameters">The optional b6 custom-pass constants.</param>
    /// <returns>The number of recorded scene-node draws.</returns>
    internal int RenderVisible(
        SilkD3D12CommandContext context,
        FastList<SceneNode> candidates,
        Func<SceneNode, ShaderPass?> passSelector,
        in GlobalTransformStruct transforms,
        bool testFrustum,
        ref BoundingFrustum frustum,
        LightsBufferModel? lights = null,
        TextureModel? environmentMap = null,
        Func<SceneNode, ShaderPass?>? boneSkinningPassSelector = null,
        ShadowMapParamStruct? shadowParameters = null,
        SilkD3D12Resource? passTexture = null,
        int passTextureRegister = -1,
        BorderEffectStruct? effectParameters = null
    ) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        context.GuardNotNull();
        candidates.GuardNotNull();
        passSelector.GuardNotNull();
        visibleNodes.Clear();
        SceneNodeFrustumSelector.AppendVisible(candidates, visibleNodes, testFrustum, ref frustum);
        context.SetDescriptorHeaps(resourceHeap, samplerHeap);
        var effectiveEnvironmentMap = lights is null
            ? null
            : PrepareEnvironment(context, lights, environmentMap);

        var recorded = 0;
        for (var index = 0; index < visibleNodes.Count; index++) {
            var node = visibleNodes.Items[index];
            if (!node.Attach()) continue;
            attachedNodes.Add(node);
            node.ComputeTransformMatrix();
            node.RenderCore.ModelMatrix = node.TotalModelMatrixInternal;
            var pass = passSelector(node);
            if (pass is null || pass.IsNull) continue;
            if (passTexture is not null) BindPassTexture(node.RenderCore, passTexture, passTextureRegister);
            if (effectParameters.HasValue && node.RenderCore is MeshRenderCore effectMesh) {
                var value = effectParameters.GetValueOrDefault();
                GetBindings(effectMesh).UpdateEffect(in value);
            }
            if (node.RenderCore is MeshRenderCore shadowedMesh) {
                var usesShadow = shadowParameters.HasValue && shadowedMesh.D3D12Material is
                    PhongMaterialCore { RenderShadowMap: true } or PbrMaterialCore { RenderShadowMap: true };
                var parameters = usesShadow ? shadowParameters.GetValueOrDefault() : default;
                GetBindings(shadowedMesh).UpdateShadow(in parameters,
                    usesShadow ? shadowMap?.Resource : null);
            }
            recorded += node.RenderCore switch {
                BoneSkinRenderCore bone => bone.TryRenderD3D12(context,
                    resources,
                    pass,
                    GetBindings(bone),
                    in transforms,
                    lights,
                    effectiveEnvironmentMap,
                    boneSkinningPassSelector?.Invoke(node))
                    ? 1
                    : 0,
                MeshRenderCore mesh => mesh.TryRenderD3D12(context,
                    resources,
                    pass,
                    GetBindings(mesh),
                    in transforms,
                    lights,
                    effectiveEnvironmentMap)
                    ? 1
                    : 0,
                PointLineRenderCore pointLine => pointLine.TryRenderD3D12(context,
                    resources,
                    pass,
                    GetBindings(pointLine),
                    in transforms)
                    ? 1
                    : 0,
                ScreenCloneRenderCore screen => GetScreenCaptureResources(screen).TryRender(context,
                    pass,
                    screen,
                    checked((uint)Math.Max(1, transforms.Viewport.X)),
                    checked((uint)Math.Max(1, transforms.Viewport.Y)))
                    ? 1
                    : 0,
                _ => 0
            };
        }
        return recorded;
    }

    /// <summary>
    ///     Records transparent geometry into weighted accumulation targets and composites it onto the main target.
    /// </summary>
    /// <param name="context">The open command context.</param>
    /// <param name="nodes">The transparent scene nodes.</param>
    /// <param name="passSelector">Selects each weighted material pass.</param>
    /// <param name="compositionPass">The weighted full-screen composition pass.</param>
    /// <param name="transforms">The current camera transforms and OIT parameters.</param>
    /// <param name="testFrustum">Whether camera-frustum culling is enabled.</param>
    /// <param name="frustum">The current camera frustum.</param>
    /// <param name="mainRenderTarget">The presentation render-target descriptor.</param>
    /// <param name="mainDepthStencil">The presentation depth/stencil descriptor.</param>
    /// <param name="width">The physical target width.</param>
    /// <param name="height">The physical target height.</param>
    /// <param name="lights">The shared light payload.</param>
    /// <param name="environmentMap">The optional environment map.</param>
    /// <param name="boneSkinningPassSelector">Selects optional bone precompute passes.</param>
    /// <param name="shadowParameters">The optional shadow-map constants.</param>
    /// <returns>The number of transparent geometry draws.</returns>
    internal int RenderWeightedTransparency(
        SilkD3D12CommandContext context,
        FastList<SceneNode> nodes,
        Func<SceneNode, ShaderPass?> passSelector,
        ShaderPass compositionPass,
        in GlobalTransformStruct transforms,
        bool testFrustum,
        ref BoundingFrustum frustum,
        SilkD3D12Descriptor mainRenderTarget,
        SilkD3D12Descriptor mainDepthStencil,
        uint width,
        uint height,
        LightsBufferModel? lights,
        TextureModel? environmentMap,
        Func<SceneNode, ShaderPass?> boneSkinningPassSelector,
        ShadowMapParamStruct? shadowParameters
    ) {
        if (nodes.Count == 0) return 0;
        transparencyResources ??= new SilkD3D12TransparencyResources(Device, resourceHeap, samplerHeap);
        transparencyResources.BeginWeighted(context, width, height, mainDepthStencil);
        var recorded = RenderVisible(context,
            nodes,
            passSelector,
            in transforms,
            testFrustum,
            ref frustum,
            lights,
            environmentMap,
            boneSkinningPassSelector,
            shadowParameters);
        transparencyResources.CompositeWeighted(context,
            compositionPass,
            in transforms,
            mainRenderTarget,
            width,
            height);
        context.SetRenderTargets(mainRenderTarget, mainDepthStencil);
        return recorded;
    }

    /// <summary>
    ///     Records dual-depth peeling and composites the accumulated layers onto the main target.
    /// </summary>
    /// <param name="context">The open command context.</param>
    /// <param name="nodes">The transparent scene nodes.</param>
    /// <param name="initialPassSelector">Selects each first min/max pass.</param>
    /// <param name="peelingPassSelector">Selects each three-target peeling pass.</param>
    /// <param name="compositionPass">The final full-screen composition pass.</param>
    /// <param name="iterations">The requested positive iteration count.</param>
    /// <param name="transforms">The current camera transforms.</param>
    /// <param name="testFrustum">Whether camera-frustum culling is enabled.</param>
    /// <param name="frustum">The current camera frustum.</param>
    /// <param name="mainColor">The presentation color resource.</param>
    /// <param name="mainRenderTarget">The presentation render-target descriptor.</param>
    /// <param name="mainDepthStencil">The presentation depth/stencil descriptor.</param>
    /// <param name="width">The physical target width.</param>
    /// <param name="height">The physical target height.</param>
    /// <param name="lights">The shared light payload.</param>
    /// <param name="environmentMap">The optional environment map.</param>
    /// <param name="boneSkinningPassSelector">Selects optional bone precompute passes.</param>
    /// <param name="shadowParameters">The optional shadow-map constants.</param>
    /// <returns>The number of transparent draws across every layer.</returns>
    internal int RenderDepthPeeling(
        SilkD3D12CommandContext context,
        FastList<SceneNode> nodes,
        Func<SceneNode, ShaderPass?> initialPassSelector,
        Func<SceneNode, ShaderPass?> peelingPassSelector,
        ShaderPass compositionPass,
        int iterations,
        in GlobalTransformStruct transforms,
        bool testFrustum,
        ref BoundingFrustum frustum,
        SilkD3D12Resource mainColor,
        SilkD3D12Descriptor mainRenderTarget,
        SilkD3D12Descriptor mainDepthStencil,
        uint width,
        uint height,
        LightsBufferModel? lights,
        TextureModel? environmentMap,
        Func<SceneNode, ShaderPass?> boneSkinningPassSelector,
        ShadowMapParamStruct? shadowParameters
    ) {
        if (nodes.Count == 0) return 0;
        if (iterations < 1) throw new ArgumentOutOfRangeException(nameof(iterations));
        transparencyResources ??= new SilkD3D12TransparencyResources(Device, resourceHeap, samplerHeap);
        transparencyResources.BeginDepthPeeling(context, mainColor, mainDepthStencil, width, height);
        var recorded = RenderVisible(context,
            nodes,
            initialPassSelector,
            in transforms,
            testFrustum,
            ref frustum,
            lights,
            environmentMap,
            boneSkinningPassSelector,
            shadowParameters);
        var lastLayer = 0;
        for (var layer = 1; layer < iterations; layer++) {
            var previousDepth = transparencyResources.BeginDepthPeelingLayer(context, layer, mainDepthStencil);
            recorded += RenderVisible(context,
                nodes,
                peelingPassSelector,
                in transforms,
                testFrustum,
                ref frustum,
                lights,
                environmentMap,
                boneSkinningPassSelector,
                shadowParameters,
                previousDepth,
                100);
            lastLayer = layer;
        }
        transparencyResources.CompositeDepthPeeling(context,
            compositionPass,
            lastLayer,
            in transforms,
            mainRenderTarget,
            width,
            height);
        context.SetRenderTargets(mainRenderTarget, mainDepthStencil);
        return recorded;
    }

    /// <summary>
    ///     Records supported opaque shadow casters into the shared shadow depth resource.
    /// </summary>
    /// <param name="context">The open Direct3D 12 command context.</param>
    /// <param name="candidates">The opaque shadow-caster candidates.</param>
    /// <param name="passSelector">Selects each node's shadow pass.</param>
    /// <param name="boneSkinningPassSelector">Selects the optional bone precompute pass.</param>
    /// <param name="transforms">The current camera transforms used by shared mesh bindings.</param>
    /// <param name="parameters">The b5 shadow-map payload.</param>
    /// <param name="frustum">The light-space culling frustum.</param>
    /// <returns>The number of recorded shadow-caster draws.</returns>
    internal int RenderShadows(
        SilkD3D12CommandContext context,
        FastList<SceneNode> candidates,
        Func<SceneNode, ShaderPass?> passSelector,
        Func<SceneNode, ShaderPass?> boneSkinningPassSelector,
        in GlobalTransformStruct transforms,
        in ShadowMapParamStruct parameters,
        ref BoundingFrustum frustum
    ) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        context.GuardNotNull();
        candidates.GuardNotNull();
        passSelector.GuardNotNull();
        boneSkinningPassSelector.GuardNotNull();
        var width = checked((uint)parameters.ShadowMapSize.X);
        var height = checked((uint)parameters.ShadowMapSize.Y);
        shadowMap ??= new SilkD3D12ShadowMap(Device, width, height);
        shadowMap.Resize(width, height);
        shadowMap.BeginDepthWrite(context);
        visibleNodes.Clear();
        SceneNodeFrustumSelector.AppendVisible(candidates, visibleNodes, true, ref frustum);
        context.SetDescriptorHeaps(resourceHeap, samplerHeap);

        var recorded = 0;
        for (var index = 0; index < visibleNodes.Count; index++) {
            var node = visibleNodes.Items[index];
            if (!node.RenderCore.IsThrowingShadow || !node.Attach()) continue;
            attachedNodes.Add(node);
            node.ComputeTransformMatrix();
            node.RenderCore.ModelMatrix = node.TotalModelMatrixInternal;
            var pass = passSelector(node);
            if (pass is null || pass.IsNull) continue;
            recorded += node.RenderCore switch {
                BoneSkinRenderCore bone => RenderShadowBone(context,
                    bone,
                    pass,
                    boneSkinningPassSelector(node),
                    in transforms,
                    in parameters),
                MeshRenderCore mesh => RenderShadowMesh(context,
                    mesh,
                    pass,
                    in transforms,
                    in parameters),
                _ => 0
            };
        }
        shadowMap.EndDepthWrite(context);
        return recorded;
    }

    /// <summary>
    ///     Records existing volume nodes through their backface, opaque-clipping, and final ray-march passes.
    /// </summary>
    /// <param name="context">The open Direct3D 12 command context.</param>
    /// <param name="volumes">The current volume nodes in scene order.</param>
    /// <param name="opaqueNodes">The opaque nodes that may clip volume rays.</param>
    /// <param name="backPassSelector">Selects the volume backface pass.</param>
    /// <param name="volumePassSelector">Selects each material's final volume pass.</param>
    /// <param name="positionPassSelector">Selects opaque world-position passes.</param>
    /// <param name="boneSkinningPassSelector">Selects optional bone precompute passes.</param>
    /// <param name="transforms">The current camera transforms.</param>
    /// <param name="testFrustum">Whether opaque clipping meshes use camera-frustum culling.</param>
    /// <param name="frustum">The current camera frustum.</param>
    /// <param name="mainRenderTarget">The presentation render-target descriptor.</param>
    /// <param name="mainDepthStencil">The presentation depth/stencil descriptor.</param>
    /// <param name="width">The physical viewport width.</param>
    /// <param name="height">The physical viewport height.</param>
    /// <returns>The number of completed volume draws.</returns>
    internal int RenderVolumes(
        SilkD3D12CommandContext context,
        FastList<SceneNode> volumes,
        FastList<SceneNode> opaqueNodes,
        Func<SceneNode, ShaderPass?> backPassSelector,
        Func<SceneNode, ShaderPass?> volumePassSelector,
        Func<SceneNode, ShaderPass?> positionPassSelector,
        Func<SceneNode, ShaderPass?> boneSkinningPassSelector,
        in GlobalTransformStruct transforms,
        bool testFrustum,
        ref BoundingFrustum frustum,
        SilkD3D12Descriptor mainRenderTarget,
        SilkD3D12Descriptor mainDepthStencil,
        uint width,
        uint height
    ) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (volumes.Count == 0) return 0;
        volumeResources ??= new SilkD3D12VolumeResources(Device);
        var recorded = 0;
        for (var index = 0; index < volumes.Count; index++) {
            var node = volumes.Items[index];
            if (node is not VolumeTextureNode || !node.Attach() ||
                node.RenderCore is not VolumeRenderCore core)
                continue;
            attachedNodes.Add(node);
            node.ComputeTransformMatrix();
            core.ModelMatrix = node.TotalModelMatrixInternal;
            var backPass = backPassSelector(node);
            var volumePass = volumePassSelector(node);
            if (backPass is null || backPass.IsNull || volumePass is null || volumePass.IsNull) continue;

            volumeResources.BeginBackPositions(context, width, height);
            var bindings = GetBindings(core);
            if (!core.PrepareD3D12(context, resources, bindings, volumeResources, in transforms)) {
                volumeResources.EndBackPositions(context);
                context.SetRenderTargets(mainRenderTarget, mainDepthStencil);
                context.SetViewport(width, height);
                continue;
            }
            VolumeRenderCore.DrawD3D12(context, backPass, bindings, volumeResources);
            _ = RenderVisible(context,
                opaqueNodes,
                positionPassSelector,
                in transforms,
                testFrustum,
                ref frustum,
                boneSkinningPassSelector: boneSkinningPassSelector);
            volumeResources.EndBackPositions(context);
            context.SetRenderTargets(mainRenderTarget, mainDepthStencil);
            context.SetViewport(width, height);
            VolumeRenderCore.DrawD3D12(context, volumePass, bindings, volumeResources);
            recorded++;
        }
        return recorded;
    }

    /// <summary>
    ///     Records visible particle simulations and their indirect geometry-shader draws.
    /// </summary>
    /// <param name="context">The open Direct3D 12 command context.</param>
    /// <param name="particles">The particle candidates in scene order.</param>
    /// <param name="updatePassSelector">Selects the compute update pass.</param>
    /// <param name="insertPassSelector">Selects the compute insertion pass.</param>
    /// <param name="renderPassSelector">Selects the particle graphics pass.</param>
    /// <param name="transforms">The current camera transforms.</param>
    /// <param name="testFrustum">Whether camera-frustum culling is enabled.</param>
    /// <param name="frustum">The current camera frustum.</param>
    /// <returns>The number of recorded particle draws.</returns>
    internal int RenderParticles(
        SilkD3D12CommandContext context,
        FastList<SceneNode> particles,
        Func<SceneNode, ShaderPass?> updatePassSelector,
        Func<SceneNode, ShaderPass?> insertPassSelector,
        Func<SceneNode, ShaderPass?> renderPassSelector,
        in GlobalTransformStruct transforms,
        bool testFrustum,
        ref BoundingFrustum frustum
    ) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        visibleNodes.Clear();
        SceneNodeFrustumSelector.AppendVisible(particles, visibleNodes, testFrustum, ref frustum);
        var recorded = 0;
        for (var index = 0; index < visibleNodes.Count; index++) {
            var node = visibleNodes.Items[index];
            if (node is not ParticleStormNode || !node.Attach() ||
                node.RenderCore is not ParticleRenderCore core)
                continue;
            attachedNodes.Add(node);
            node.ComputeTransformMatrix();
            core.ModelMatrix = node.TotalModelMatrixInternal;
            var updatePass = updatePassSelector(node);
            var insertPass = insertPassSelector(node);
            var renderPass = renderPassSelector(node);
            if (updatePass is null || updatePass.IsNull || insertPass is null || insertPass.IsNull ||
                renderPass is null || renderPass.IsNull)
                continue;
            var particle = GetParticleResources(core);
            if (particle.Render(context,
                    resources,
                    core,
                    updatePass,
                    insertPass,
                    renderPass,
                    in transforms))
                recorded++;
        }
        return recorded;
    }

    /// <summary>
    ///     Records one static mesh into the current shadow depth target.
    /// </summary>
    /// <param name="context">The open command context.</param>
    /// <param name="mesh">The existing mesh core.</param>
    /// <param name="pass">The native shadow pass.</param>
    /// <param name="transforms">The shared camera transforms.</param>
    /// <param name="parameters">The current shadow parameters.</param>
    /// <returns>One when the draw was recorded; otherwise zero.</returns>
    private int RenderShadowMesh(
        SilkD3D12CommandContext context,
        MeshRenderCore mesh,
        ShaderPass pass,
        in GlobalTransformStruct transforms,
        in ShadowMapParamStruct parameters
    ) {
        var bindings = GetBindings(mesh);
        bindings.UpdateShadow(in parameters, shadowMap?.Resource);
        return mesh.TryRenderD3D12(context, resources, pass, bindings, in transforms) ? 1 : 0;
    }

    /// <summary>
    ///     Records one bone-skinned mesh into the current shadow depth target.
    /// </summary>
    /// <param name="context">The open command context.</param>
    /// <param name="bone">The existing bone-skinned core.</param>
    /// <param name="pass">The native shadow pass.</param>
    /// <param name="preComputePass">The optional bone precompute pass.</param>
    /// <param name="transforms">The shared camera transforms.</param>
    /// <param name="parameters">The current shadow parameters.</param>
    /// <returns>One when the draw was recorded; otherwise zero.</returns>
    private int RenderShadowBone(
        SilkD3D12CommandContext context,
        BoneSkinRenderCore bone,
        ShaderPass pass,
        ShaderPass? preComputePass,
        in GlobalTransformStruct transforms,
        in ShadowMapParamStruct parameters
    ) {
        var bindings = GetBindings(bone);
        bindings.UpdateShadow(in parameters, shadowMap?.Resource);
        return bone.TryRenderD3D12(context,
            resources,
            pass,
            bindings,
            in transforms,
            null,
            null,
            preComputePass)
            ? 1
            : 0;
    }

    /// <summary>
    ///     Loads and validates the current environment cube map and updates its shared-light metadata.
    /// </summary>
    /// <param name="context">The command context receiving a first-use upload.</param>
    /// <param name="lights">The shared light payload.</param>
    /// <param name="environmentMap">The optional existing environment texture.</param>
    /// <returns>The validated cube map, or <see langword="null" /> when none is usable.</returns>
    internal TextureModel? PrepareEnvironment(
        SilkD3D12CommandContext context,
        LightsBufferModel lights,
        TextureModel? environmentMap
    ) {
        context.GuardNotNull();
        lights.GuardNotNull();
        lights.HasEnvironmentMap = false;
        lights.EnvironmentMapMipLevels = 0;
        if (environmentMap is null) return null;
        var texture = resources.GetOrCreate(context, environmentMap);
        if (!texture.IsCubeMap) return null;
        lights.HasEnvironmentMap = true;
        lights.EnvironmentMapMipLevels = texture.Resource.Description.MipLevels;
        return environmentMap;
    }

    /// <summary>
    ///     Rebuilds the shared light payload from existing visible light nodes in scene order.
    /// </summary>
    /// <param name="nodes">The current light nodes.</param>
    /// <param name="destination">The shared destination light model.</param>
    internal static void UpdateLights(FastList<SceneNode> nodes, LightsBufferModel destination) {
        nodes.GuardNotNull();
        destination.GuardNotNull();
        destination.ResetLightCount();
        for (var index = 0; index < nodes.Count && destination.LightCount < Constants.MaxLights; index++) {
            if (nodes.Items[index] is not LightNode { Visible: true } node) continue;
            node.ComputeTransformMatrix();
            if (node is AmbientLightNode) {
                destination.AmbientLight = node.Color;
                continue;
            }

            var light = new LightStruct {
                LightType = (int)node.LightType,
                LightColor = node.Color
            };
            switch (node) {
                case DirectionalLightNode directional:
                    light.LightDir = -SilkMath.TransformNormal(directional.Direction, node.TotalModelMatrixInternal)
                        .Normalized()
                        .ToVector4(0);
                    break;
                case SpotLightNode spot:
                    ApplyPoint(ref light, spot, node.TotalModelMatrixInternal);
                    light.LightDir = SilkMath.TransformNormal(spot.Direction, node.TotalModelMatrixInternal)
                        .Normalized()
                        .ToVector4(0);
                    light.LightSpot = new Vector4((float)Math.Cos(spot.OuterAngle / 360f * Math.PI),
                        (float)Math.Cos(spot.InnerAngle / 360f * Math.PI),
                        spot.FallOff,
                        0);
                    break;
                case PointLightNode point:
                    ApplyPoint(ref light, point, node.TotalModelMatrixInternal);
                    break;
                default:
                    continue;
            }

            destination.Lights[destination.LightCount] = light;
            destination.IncrementLightCount();
        }
    }

    /// <summary>
    ///     Applies the shared point-light position, attenuation, and range fields.
    /// </summary>
    /// <param name="light">The destination light structure.</param>
    /// <param name="point">The existing point or spot light.</param>
    /// <param name="modelMatrix">The light-node world transform.</param>
    private static void ApplyPoint(ref LightStruct light, PointLightNode point, in Matrix modelMatrix) {
        light.LightPos = (point.Position + modelMatrix.Row4.ToVector3()).ToVector4();
        light.LightAtt = point.Attenuation.ToVector4(point.Range);
    }

    /// <summary>
    ///     Gets or creates the descriptor tables for one mesh core.
    /// </summary>
    /// <param name="core">The existing mesh core.</param>
    /// <returns>The core's host-lifetime bindings.</returns>
    private SilkD3D12MeshBindings GetBindings(MeshRenderCore core) {
        if (meshBindings.TryGetValue(core, out var bindings)) return bindings;
        bindings = new SilkD3D12MeshBindings(Device, resourceHeap, samplerHeap);
        meshBindings.Add(core, bindings);
        return bindings;
    }

    /// <summary>
    ///     Gets or creates the descriptor tables for one point, line, or billboard core.
    /// </summary>
    /// <param name="core">The existing point/line core.</param>
    /// <returns>The core's host-lifetime bindings.</returns>
    private SilkD3D12PointLineBindings GetBindings(PointLineRenderCore core) {
        if (pointLineBindings.TryGetValue(core, out var bindings)) return bindings;
        bindings = new SilkD3D12PointLineBindings(Device, resourceHeap, samplerHeap);
        pointLineBindings.Add(core, bindings);
        return bindings;
    }

    /// <summary>
    ///     Gets or creates the descriptor tables for one volume core.
    /// </summary>
    /// <param name="core">The existing volume core.</param>
    /// <returns>The core's host-lifetime bindings.</returns>
    private SilkD3D12VolumeBindings GetBindings(VolumeRenderCore core) {
        if (volumeBindings.TryGetValue(core, out var bindings)) return bindings;
        bindings = new SilkD3D12VolumeBindings(Device, resourceHeap, samplerHeap);
        volumeBindings.Add(core, bindings);
        return bindings;
    }

    /// <summary>
    ///     Gets or recreates the complete native state for one particle core.
    /// </summary>
    /// <param name="core">The existing particle core.</param>
    /// <returns>The synchronized native particle resources.</returns>
    private SilkD3D12ParticleResources GetParticleResources(ParticleRenderCore core) {
        if (particleResources.TryGetValue(core, out var particle) &&
            !core.RequiresD3D12Recreate(particle.Capacity))
            return particle;
        if (particle is not null) {
            particle.Dispose();
            particleResources.Remove(core);
        }
        particle = new SilkD3D12ParticleResources(Device,
            resourceHeap,
            samplerHeap,
            checked((uint)core.ParticleCount));
        particleResources.Add(core, particle);
        core.CompleteD3D12Recreate();
        return particle;
    }

    /// <summary>
    ///     Gets or creates the Direct3D 12 upload and binding state for one desktop capture core.
    /// </summary>
    /// <param name="core">The existing capture core.</param>
    /// <returns>The core's host-lifetime resources.</returns>
    private SilkD3D12ScreenCaptureResources GetScreenCaptureResources(ScreenCloneRenderCore core) {
        if (screenCaptureResources.TryGetValue(core, out var capture)) return capture;
        capture = new SilkD3D12ScreenCaptureResources(Device, resourceHeap, samplerHeap);
        screenCaptureResources.Add(core, capture);
        return capture;
    }

    /// <summary>
    ///     Binds one pass-local texture to the existing per-core root table.
    /// </summary>
    /// <param name="core">The geometry core.</param>
    /// <param name="resource">The pass-local texture.</param>
    /// <param name="shaderRegister">The target t-register.</param>
    private void BindPassTexture(RenderCore core, SilkD3D12Resource resource, int shaderRegister) {
        if (shaderRegister < 0) throw new ArgumentOutOfRangeException(nameof(shaderRegister));
        switch (core) {
            case MeshRenderCore mesh:
                GetBindings(mesh).BindPassTexture(resource, shaderRegister);
                break;
            case PointLineRenderCore pointLine:
                GetBindings(pointLine).BindPassTexture(resource, shaderRegister);
                break;
        }
    }

    /// <summary>
    ///     Detaches nodes and releases per-core bindings and shared native resources.
    /// </summary>
    public void Dispose() {
        if (IsDisposed) return;
        foreach (var node in attachedNodes) node.Detach();
        foreach (var bindings in meshBindings.Values) bindings.Dispose();
        foreach (var bindings in pointLineBindings.Values) bindings.Dispose();
        foreach (var bindings in volumeBindings.Values) bindings.Dispose();
        foreach (var particle in particleResources.Values) particle.Dispose();
        foreach (var capture in screenCaptureResources.Values) capture.Dispose();
        attachedNodes.Clear();
        meshBindings.Clear();
        pointLineBindings.Clear();
        volumeBindings.Clear();
        particleResources.Clear();
        screenCaptureResources.Clear();
        visibleNodes.Clear();
        shadowMap?.Dispose();
        shadowMap = null;
        volumeResources?.Dispose();
        volumeResources = null;
        transparencyResources?.Dispose();
        transparencyResources = null;
        postProcessResources?.Dispose();
        postProcessResources = null;
        ssaoResources?.Dispose();
        ssaoResources = null;
        scene2DRenderer.Dispose();
        resources.Dispose();
        IsDisposed = true;
    }
}

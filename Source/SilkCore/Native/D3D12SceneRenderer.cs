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
using HelixToolkit.SharpDX.Core.Model.Scene.PostEffects;
using HelixToolkit.SharpDX.Core.ShaderManager;
using Silk.NET.DXGI;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Utilities;
using HelixToolkit.SharpDX.Core.Model.Scene2D.Abstract;
using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core.Native;

/// <summary>
///     Records visible existing geometry scene nodes through the productive Direct3D 12 render-core paths.
/// </summary>
internal sealed class SilkD3D12SceneRenderer : IDisposable {
    /// Per-core immutable GPU intersection generations, owned by this render host.
    private readonly Dictionary<MeshIntersectionRenderCore, SilkD3D12IntersectionResources> intersectionResources = [];
    /// Replaced generations retained until the next completed host fence.
    private readonly List<SilkD3D12IntersectionResources> retiredIntersections = [];
    /// The host-lifetime compute root signature shared by intersection generations.
    private SilkD3D12RootSignature? intersectionRoot;
    /// The host-lifetime compute pipeline cache shared by intersection generations.
    private readonly D3D12PipelineStateCache intersectionPipelines = new();
    /// <summary>
    ///     Gets the actual native resources used for selected-pair dispatch and indirect drawing.
    /// </summary>
    /// <param name="core">The intersection render core.</param>
    /// <returns>The current resources, or <see langword="null"/> before rendering.</returns>
    internal SilkD3D12IntersectionResources? FindIntersectionResources(MeshIntersectionRenderCore core) =>
        intersectionResources.GetValueOrDefault(core);
    /// <summary>
    ///     The shared shader-visible resource heap.
    /// </summary>
    private readonly SilkD3D12DescriptorHeap resourceHeap;

    /// <summary>
    ///     The shared shader-visible sampler heap.
    /// </summary>
    private readonly SilkD3D12DescriptorHeap samplerHeap;

    /// Shader-visible descriptor pages shared by all immutable transient effect draws in this renderer.
    private readonly D3D12EffectBindingPool _effectBindings;

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

    /// Mesh bindings with fixed peeling inputs; the first input borrows the ordinary per-core cache.
    private readonly Dictionary<SilkD3D12Resource, Dictionary<MeshRenderCore, SilkD3D12MeshBindings>>
        _peelingMeshBindings = [];

    /// Point, line and billboard peeling bindings; the first input borrows the ordinary per-core cache.
    private readonly Dictionary<SilkD3D12Resource, Dictionary<PointLineRenderCore, SilkD3D12PointLineBindings>>
        _peelingPointLineBindings = [];

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

    /// Full-resolution outline mask and stencil-excluded result.
    private SilkD3D12PostProcessResources? outlineResources;
    /// Source glow's half-resolution vertical blur target.
    private SilkD3D12PostProcessResources? outlineBlurResources;
    /// Isolated outline and XRay stencil ownership.
    private D3D12PostEffectStencil? effectStencil;
    /// Compatibility catalog for callers of the original low-level outline method.
    private D3D12ScenePassCatalog? outlineCatalog;
    /// Frame-local mesh effect bindings whose constants remain immutable until submission completes.
    private readonly List<SilkD3D12MeshBindings> effectMeshBindings = [];
    /// Frame-local point/line effect bindings whose constants remain immutable until submission completes.
    private readonly List<SilkD3D12PointLineBindings> effectLineBindings = [];
    /// The next unused mesh effect binding in this completed-fence frame.
    private int effectMeshIndex;
    /// The next unused point/line effect binding in this completed-fence frame.
    private int effectLineIndex;

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
        device.AsGuardNotNull();
        resourceHeap.AsGuardNotNull();
        samplerHeap.AsGuardNotNull();
        this.resourceHeap = resourceHeap;
        this.samplerHeap = samplerHeap;
        _effectBindings = new D3D12EffectBindingPool(device);
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

    /// Gets the descriptor pages used by recorded effects for native capacity and lifetime verification.
    internal D3D12EffectBindingPool EffectBindings => _effectBindings;

    /// <summary>
    ///     Prepares the active hierarchy before visibility pruning and native draw classification.
    /// </summary>
    /// <param name="roots">The original scene hierarchy roots.</param>
    /// <param name="context">The current frame context passed to each original node's update hook.</param>
    /// <returns>Visible nodes in preorder, excluding every subtree beneath an invisible or disposed ancestor.</returns>
    /// <exception cref="ObjectDisposedException">The renderer has been disposed.</exception>
    internal FastList<SceneNode> PrepareScene(IEnumerable<SceneNode> roots, RenderContext context) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        var snapshot = roots.ToArray();
        foreach (var node in snapshot.PreorderDft(node => !node.IsDisposed)) {
            if (!node.RenderCore.IsAttached && !node.Attach()) continue;
            attachedNodes.Add(node);
            node.Update(context);
        }
        var prepared = new FastList<SceneNode>();
        foreach (var node in snapshot.PreorderDft(node =>
                     !node.IsDisposed && node.Visible && node.RenderCore.IsAttached))
            prepared.Add(node);
        return prepared;
    }

    /// <summary>
    ///     Recycles descriptor slots only after the presentation host has completed the preceding frame fence.
    /// </summary>
    internal void BeginFrame() {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        postProcessResources?.BeginFrame();
        outlineResources?.BeginFrame();
        outlineBlurResources?.BeginFrame();
        effectStencil?.BeginFrame();
        effectMeshIndex = 0;
        effectLineIndex = 0;
        scene2DRenderer.BeginFrame();
        ReleaseDisposedResources(meshBindings);
        foreach (var texture in _peelingMeshBindings.Keys.ToArray()) {
            var cache = _peelingMeshBindings[texture];
            ReleaseDisposedResources(cache);
            if (!texture.IsDisposed) continue;
            if (!ReferenceEquals(cache, meshBindings))
                foreach (var binding in cache.Values) binding.Dispose();
            _peelingMeshBindings.Remove(texture);
        }
        foreach (var texture in _peelingPointLineBindings.Keys.ToArray()) {
            var cache = _peelingPointLineBindings[texture];
            ReleaseDisposedResources(cache);
            if (!texture.IsDisposed) continue;
            if (!ReferenceEquals(cache, pointLineBindings))
                foreach (var binding in cache.Values) binding.Dispose();
            _peelingPointLineBindings.Remove(texture);
        }
        ReleaseDisposedResources(pointLineBindings);
        ReleaseDisposedResources(volumeBindings);
        ReleaseDisposedResources(particleResources);
        ReleaseDisposedResources(screenCaptureResources);
        ReleaseDisposedResources(intersectionResources);
        foreach (var core in intersectionResources.Keys.Where(core => !core.IsAttached).ToArray()) {
            intersectionResources[core].Dispose();
            intersectionResources.Remove(core);
        }
        foreach (var retired in retiredIntersections) retired.Dispose();
        retiredIntersections.Clear();
    }


    /// <summary>
    ///     Releases cached resources belonging to disposed cores after the completed presentation fence.
    /// </summary>
    /// <typeparam name="TCore">The original render-core type.</typeparam>
    /// <typeparam name="TResource">The owned native resource type.</typeparam>
    /// <param name="cache">The per-core resource cache.</param>
    private static void ReleaseDisposedResources<TCore, TResource>(Dictionary<TCore, TResource> cache)
        where TCore : RenderCore
        where TResource : IDisposable {
        foreach (var core in cache.Keys.Where(core => core.IsDisposed).ToArray()) {
            var resource = cache[core];
            cache.Remove(core);
            resource.Dispose();
        }
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
        postProcessResources ??= new SilkD3D12PostProcessResources(Device, resourceHeap, samplerHeap, _effectBindings);
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
        postProcessResources ??= new SilkD3D12PostProcessResources(Device, resourceHeap, samplerHeap, _effectBindings);
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
    ///     Dispatches registered outline, border and XRay nodes using their actual attributes and source pass policies.
    /// </summary>
    /// <param name="context">The open command context.</param>
    /// <param name="candidates">Opaque and transparent geometry in original scene traversal order.</param>
    /// <param name="effects">The registered post-effect nodes in draw order.</param>
    /// <param name="catalog">The existing registered pass catalog.</param>
    /// <param name="transforms">The global camera and viewport constants.</param>
    /// <param name="testFrustum">Whether camera-frustum culling is enabled.</param>
    /// <param name="frustum">The current camera frustum.</param>
    /// <param name="mainColor">The presentation color resource.</param>
    /// <param name="mainRenderTarget">The presentation RTV.</param>
    /// <param name="sceneDepth">The original scene depth/stencil resource, preserved by each effect.</param>
    /// <param name="mainDepthStencil">The original scene DSV.</param>
    /// <param name="width">The physical width.</param>
    /// <param name="height">The physical height.</param>
    /// <returns>The number of recorded custom geometry draws.</returns>
    internal int RenderPostEffects(SilkD3D12CommandContext context, FastList<SceneNode> candidates,
        FastList<SceneNode> effects, D3D12ScenePassCatalog catalog, in GlobalTransformStruct transforms,
        bool testFrustum, ref BoundingFrustum frustum, SilkD3D12Resource mainColor,
        SilkD3D12Descriptor mainRenderTarget, SilkD3D12Resource sceneDepth,
        SilkD3D12Descriptor mainDepthStencil, uint width, uint height) {
        var recorded = 0;
        foreach (var effectNode in effects)
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
                    recorded += RenderOutline(context,
                        candidates,
                        node => node.TryGetPostEffect(effectName, out _)
                            ? catalog.ResolvePostEffectGeometry(node,
                                DefaultPassNames.EffectOutlineP1,
                                Format.FormatD32FloatS8X24Uint)
                            : null,
                        catalog.ResolvePostProcess(technique, DefaultPassNames.EffectBlurVertical),
                        catalog.ResolvePostProcess(technique, DefaultPassNames.EffectBlurHorizontal),
                        catalog.ResolvePostProcess(technique, DefaultPassNames.MeshOutline),
                        in outlineParameters,
                        outline.NumberOfBlurPass,
                        in transforms,
                        testFrustum,
                        ref frustum,
                        mainColor,
                        mainRenderTarget,
                        mainDepthStencil,
                        width,
                        height,
                        catalog.ResolvePostProcess(technique, DefaultPassNames.ScreenQuad),
                        effectName,
                        outline is NodePostEffectBorderHighlight border ? border.DrawMode : OutlineMode.Merged,
                        outline is not NodePostEffectBorderHighlight,
                        catalog.ResolveBoneSkinning);
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
                                Format.FormatD32FloatS8X24Uint)
                            : null);
                    selectors.Add(node => node.TryGetPostEffect(effectName, out _)
                        ? catalog.ResolvePostEffectGeometry(node,
                            DefaultPassNames.EffectMeshXRayP2,
                            Format.FormatD32FloatS8X24Uint,
                            xray.EnableDoublePass ? null : 0)
                        : null);
                    recorded += RenderXRay(context,
                        candidates,
                        selectors,
                        in xrayParameters,
                        in transforms,
                        testFrustum,
                        ref frustum,
                        mainRenderTarget,
                        mainDepthStencil,
                        width,
                        height, effectName, sceneDepth, catalog.ResolveBoneSkinning);
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
                                Format.FormatD32FloatS8X24Uint)
                            : null
                    ];
                    if (grid.UseDepthOcclusion)
                        selectors.Add(node => node.TryGetPostEffect(effectName, out _)
                            ? catalog.ResolvePostEffectGeometry(node,
                                DefaultPassNames.EffectMeshXRayGridP2,
                                Format.FormatD32FloatS8X24Uint)
                            : null);
                    selectors.Add(node => node.TryGetPostEffect(effectName, out _)
                        ? catalog.ResolvePostEffectGeometry(node, grid.XRayDrawingPassName, Format.FormatD32FloatS8X24Uint)
                        : null);
                    recorded += RenderXRay(context,
                        candidates,
                        selectors,
                        in gridParameters,
                        in transforms,
                        testFrustum,
                        ref frustum,
                        mainRenderTarget,
                        mainDepthStencil,
                        width,
                        height, effectName, sceneDepth, catalog.ResolveBoneSkinning);
                    break;
                }
            }
        return recorded;
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
    /// <param name="stencilPass">The original stencil-tested full-screen interior-exclusion pass.</param>
    /// <param name="effectName">The selected effect identity used to resolve per-node attributes.</param>
    /// <param name="drawMode">Whether mask interiors are merged or excluded separately per node.</param>
    /// <param name="useBlurCore">Whether to use the source half-resolution glow blur policy.</param>
    /// <param name="boneSkinningPassSelector">The original GPU skinning precompute selector.</param>
    /// <exception cref="ArgumentOutOfRangeException">The blur count is negative.</exception>
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
        uint height,
        ShaderPass? stencilPass = null,
        string? effectName = null,
        OutlineMode drawMode = OutlineMode.Merged,
        bool useBlurCore = false,
        Func<SceneNode, ShaderPass?>? boneSkinningPassSelector = null
    ) {
        ArgumentOutOfRangeException.ThrowIfNegative(blurPasses);
        outlineResources ??= new SilkD3D12PostProcessResources(Device, resourceHeap, samplerHeap, _effectBindings);
        effectStencil ??= new D3D12PostEffectStencil(Device);
        if (stencilPass is null) {
            outlineCatalog ??= new D3D12ScenePassCatalog(Device, mainColor.Description.Format,
                Format.FormatD32FloatS8X24Uint);
            stencilPass = outlineCatalog.ResolvePostProcess(DefaultRenderTechniqueNames.PostEffectMeshOutlineBlur,
                DefaultPassNames.ScreenQuad);
        }
        var recorded = 0;
        // Separated mode excludes only this object's interior before composing it, as the source does.
        var groups = drawMode == OutlineMode.Separated
            ? nodes.Select(node => new FastList<SceneNode> { node })
            : new[] { nodes };
        foreach (var group in groups) {
            var stencil = effectStencil.Begin(context, width, height);
            outlineResources.BeginTarget(context, 0, width, height, stencil);
            var count = RenderVisible(context, group, maskPassSelector, in transforms, testFrustum, ref frustum,
                boneSkinningPassSelector: boneSkinningPassSelector, effectParameters: parameters,
                effectName: effectName);
            recorded += count;
            if (count == 0) continue;
            for (var index = 0; index < blurPasses; index++) {
                var blurParameters = parameters;
                if (useBlurCore) {
                    outlineBlurResources ??= new SilkD3D12PostProcessResources(Device, resourceHeap, samplerHeap, _effectBindings);
                    blurParameters.ViewportScale = 2;
                    outlineBlurResources.DrawSource(context, verticalBlurPass, outlineResources.First, 0,
                        Math.Max(1u, width / 2), Math.Max(1u, height / 2), in transforms, in blurParameters);
                    blurParameters.ViewportScale = 1;
                    outlineResources.DrawSource(context, horizontalBlurPass, outlineBlurResources.First, 0,
                        width, height, in transforms, in blurParameters);
                } else {
                    outlineResources.Draw(context, horizontalBlurPass, 0, 1, in transforms, in blurParameters);
                    outlineResources.Draw(context, verticalBlurPass, 1, 0, in transforms, in blurParameters);
                }
            }
            outlineResources.DrawSource(context, stencilPass, outlineResources.First, 1, width, height,
                in transforms, in parameters, stencil);
            context.Transition(mainColor, Silk.NET.Direct3D12.ResourceStates.RenderTarget);
            outlineResources.DrawToPresentation(context, compositionPass, 1, mainRenderTarget,
                in transforms, in parameters);
        }
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
    /// <param name="effectName">The selected effect identity used to resolve per-node attributes.</param>
    /// <param name="sceneDepth">The scene depth copied into isolated stencil ownership when supplied.</param>
    /// <param name="boneSkinningPassSelector">The original GPU skinning precompute selector.</param>
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
        uint height,
        string? effectName = null,
        SilkD3D12Resource? sceneDepth = null,
        Func<SceneNode, ShaderPass?>? boneSkinningPassSelector = null
    ) {
        var recorded = 0;
        var stencil = mainDepthStencil;
        if (sceneDepth is not null) {
            effectStencil ??= new D3D12PostEffectStencil(Device);
            stencil = effectStencil.Begin(context, width, height, sceneDepth);
        }
        foreach (var selector in passSelectors) {
            context.SetRenderTargets(mainRenderTarget, stencil);
            context.SetViewport(width, height);
            recorded += RenderVisible(context,
                nodes,
                selector,
                in transforms,
                testFrustum,
                ref frustum,
                boneSkinningPassSelector: boneSkinningPassSelector,
                effectParameters: parameters, effectName: effectName);
        }
        context.SetRenderTargets(mainRenderTarget, mainDepthStencil);
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
    /// <param name="effectName">The exact selected effect whose original color attribute overrides b6.</param>
    /// <returns>The number of recorded scene-node draws.</returns>
    /// <exception cref="ObjectDisposedException">The renderer has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A selected pass texture has a negative shader register.</exception>
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
        BorderEffectStruct? effectParameters = null,
        string? effectName = null
    ) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        context.AsGuardNotNull();
        candidates.AsGuardNotNull();
        passSelector.AsGuardNotNull();
        visibleNodes.Clear();
        SceneNodeFrustumSelector.AppendVisible(candidates, visibleNodes, testFrustum, ref frustum);
        context.SetDescriptorHeaps(resourceHeap, samplerHeap);
        var effectiveEnvironmentMap = lights is null
            ? null
            : PrepareEnvironment(context, lights, environmentMap);

        var recorded = 0;
        for (var index = 0; index < visibleNodes.Count; index++) {
            var node = visibleNodes.Items[index];
            if (!node.RenderCore.IsAttached && !node.Attach()) continue;
            attachedNodes.Add(node);
            if (node.RenderCore is MeshRenderCore meshOwner) meshOwner.D3D12SceneNode = node;
            node.ComputeTransformMatrix();
            node.RenderCore.ModelMatrix = node.TotalModelMatrixInternal;
            var pass = passSelector(node);
            if (pass is null || pass.IsNull) continue;
            // A recorded descriptor table must retain its depth input while later layers record the other input.
            var drawMeshBindings = node.RenderCore is MeshRenderCore meshCore
                ? effectParameters.HasValue ? NextEffectMeshBindings()
                    : GetBindings(meshCore, passTextureRegister == 100 ? passTexture : null)
                : null;
            var drawPointLineBindings = node.RenderCore is PointLineRenderCore pointLineCore
                ? effectParameters.HasValue ? NextEffectLineBindings()
                    : GetBindings(pointLineCore, passTextureRegister == 100 ? passTexture : null)
                : null;
            if (passTexture is not null) {
                if (passTextureRegister < 0) throw new ArgumentOutOfRangeException(nameof(passTextureRegister));
                drawMeshBindings?.BindPassTexture(passTexture, passTextureRegister);
                drawPointLineBindings?.BindPassTexture(passTexture, passTextureRegister);
            }
            if (effectParameters.HasValue) {
                var value = effectParameters.GetValueOrDefault();
                if (effectName is not null && node.TryGetPostEffect(effectName, out var attributes) &&
                    attributes.TryGetAttribute(EffectAttributeNames.ColorAttributeName, out var attribute) &&
                    attribute is string color)
                    value.Color = color.ToColor4();
                drawMeshBindings?.UpdateEffect(in value);
                drawPointLineBindings?.UpdateEffect(in value);
            }
            if (node.RenderCore is MeshRenderCore shadowedMesh) {
                var usesShadow = shadowParameters.HasValue && shadowedMesh.D3D12Material is
                    ID3D12MeshMaterial { RenderShadowMap: true } or
                    PhongMaterialCore { RenderShadowMap: true } or PbrMaterialCore { RenderShadowMap: true };
                var parameters = usesShadow ? shadowParameters.GetValueOrDefault() : default;
                drawMeshBindings!.UpdateShadow(in parameters,
                    usesShadow ? shadowMap?.Resource : null);
            }
            drawMeshBindings?.BindHeaps(context);
            drawPointLineBindings?.BindHeaps(context);
            recorded += node.RenderCore switch {
                BoneSkinRenderCore bone => bone.TryRenderD3D12(context,
                    resources,
                    pass,
                    drawMeshBindings!,
                    in transforms,
                    lights,
                    effectiveEnvironmentMap,
                    boneSkinningPassSelector?.Invoke(node))
                    ? 1
                    : 0,
                MeshRenderCore mesh => mesh.TryRenderD3D12(context,
                    resources,
                    pass,
                    drawMeshBindings!,
                    in transforms,
                    lights,
                    effectiveEnvironmentMap)
                    ? 1
                    : 0,
                MeshIntersectionRenderCore { UseGpu: true } intersection =>
                    RenderIntersection(context, intersection, pass, in transforms, drawPointLineBindings) ? 1 : 0,
                PointLineRenderCore pointLine => pointLine.TryRenderD3D12(context,
                    resources,
                    pass,
                    drawPointLineBindings!,
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
        context.AsGuardNotNull();
        candidates.AsGuardNotNull();
        passSelector.AsGuardNotNull();
        boneSkinningPassSelector.AsGuardNotNull();
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
            if (node.RenderCore is MeshRenderCore meshOwner) meshOwner.D3D12SceneNode = node;
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
            if (node.RenderCore is MeshRenderCore meshOwner) meshOwner.D3D12SceneNode = node;
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
            if (node.RenderCore is MeshRenderCore meshOwner) meshOwner.D3D12SceneNode = node;
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
        context.AsGuardNotNull();
        lights.AsGuardNotNull();
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
        nodes.AsGuardNotNull();
        destination.AsGuardNotNull();
        destination.ResetLightCount();
        for (var index = 0; index < nodes.Count && destination.LightCount < Constants.MaxLights; index++) {
            if (nodes.Items[index] is not LightNode { Visible: true } node) continue;
            if (node.RenderCore is MeshRenderCore meshOwner) meshOwner.D3D12SceneNode = node;
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
    ///     Gets a mesh binding slot whose constants are not overwritten by another recorded effect draw.
    /// </summary>
    /// <returns>The next completed-fence reusable mesh binding slot.</returns>
    private SilkD3D12MeshBindings NextEffectMeshBindings() {
        if (effectMeshIndex == effectMeshBindings.Count)
            effectMeshBindings.Add(new SilkD3D12MeshBindings(Device, resourceHeap, samplerHeap, _effectBindings));
        return effectMeshBindings[effectMeshIndex++];
    }

    /// <summary>
    ///     Gets a point/line binding slot whose constants remain stable across all recorded effects.
    /// </summary>
    /// <returns>The next completed-fence reusable point/line binding slot.</returns>
    private SilkD3D12PointLineBindings NextEffectLineBindings() {
        if (effectLineIndex == effectLineBindings.Count)
            effectLineBindings.Add(new SilkD3D12PointLineBindings(Device, resourceHeap, samplerHeap, _effectBindings));
        return effectLineBindings[effectLineIndex++];
    }

    /// <summary>
    ///     Gets or creates the descriptor tables for one mesh core.
    /// </summary>
    /// <param name="core">The existing mesh core.</param>
    /// <param name="peelingTexture">The depth texture kept immutable across recorded peeling draws.</param>
    /// <returns>The core's host-lifetime bindings.</returns>
    private SilkD3D12MeshBindings GetBindings(MeshRenderCore core, SilkD3D12Resource? peelingTexture = null) {
        var cache = meshBindings;
        if (peelingTexture is not null && !_peelingMeshBindings.TryGetValue(peelingTexture, out cache)) {
            var primaryInUse = _peelingMeshBindings.Any(entry =>
                !entry.Key.IsDisposed && ReferenceEquals(entry.Value, meshBindings));
            cache = primaryInUse
                ? new Dictionary<MeshRenderCore, SilkD3D12MeshBindings>(ReferenceEqualityComparer.Instance)
                : meshBindings;
            _peelingMeshBindings.Add(peelingTexture, cache);
        }
        if (cache.TryGetValue(core, out var bindings)) return bindings;
        bindings = new SilkD3D12MeshBindings(Device, resourceHeap, samplerHeap);
        cache.Add(core, bindings);
        return bindings;
    }

    /// <summary>
    ///     Records compute output through the ordinary line material and custom effect passes.
    /// </summary>
    /// <param name="context">The host command context.</param>
    /// <param name="core">The immutable selected-pair input owner.</param>
    /// <param name="pass">The selected line or post-effect graphics pass.</param>
    /// <param name="transforms">The current camera constants.</param>
    /// <param name="effectBindings">The immutable selected post-effect bindings, when rendering a custom pass.</param>
    /// <returns>Whether the GPU indirect line draw was recorded.</returns>
    private bool RenderIntersection(SilkD3D12CommandContext context, MeshIntersectionRenderCore core,
        ShaderPass pass, in GlobalTransformStruct transforms, SilkD3D12PointLineBindings? effectBindings = null) {
        if (core.D3D12Material is null) return false;
        if (!intersectionResources.TryGetValue(core, out var native) ||
            !ReferenceEquals(native.Inputs, core.Inputs) || !native.CanReuse()) {
            intersectionRoot ??= Device.CreateDefaultRootSignature();
            var replacement = new SilkD3D12IntersectionResources(Device, resourceHeap, samplerHeap, resources,
                core.Inputs, intersectionRoot, intersectionPipelines);
            if (native is not null) retiredIntersections.Add(native);
            intersectionResources[core] = native = replacement;
        }
        var bindings = effectBindings ?? GetBindings(core);
        var model = new PointLineModelStruct { World = Matrix.Identity };
        bindings.Update(context, resources, in transforms, in model, core.D3D12Material);
        return native.Render(context, pass, bindings);
    }

    /// <summary>
    ///     Gets or creates the descriptor tables for one point, line, or billboard core.
    /// </summary>
    /// <param name="core">The existing point/line core.</param>
    /// <param name="peelingTexture">The depth texture kept immutable across recorded peeling draws.</param>
    /// <returns>The core's host-lifetime bindings.</returns>
    private SilkD3D12PointLineBindings GetBindings(PointLineRenderCore core,
        SilkD3D12Resource? peelingTexture = null) {
        var cache = pointLineBindings;
        if (peelingTexture is not null && !_peelingPointLineBindings.TryGetValue(peelingTexture, out cache)) {
            var primaryInUse = _peelingPointLineBindings.Any(entry =>
                !entry.Key.IsDisposed && ReferenceEquals(entry.Value, pointLineBindings));
            cache = primaryInUse
                ? new Dictionary<PointLineRenderCore, SilkD3D12PointLineBindings>(ReferenceEqualityComparer.Instance)
                : pointLineBindings;
            _peelingPointLineBindings.Add(peelingTexture, cache);
        }
        if (cache.TryGetValue(core, out var bindings)) return bindings;
        bindings = new SilkD3D12PointLineBindings(Device, resourceHeap, samplerHeap);
        cache.Add(core, bindings);
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
    ///     Detaches nodes and releases per-core bindings and shared native resources.
    /// </summary>
    public void Dispose() {
        if (IsDisposed) return;
        foreach (var cache in _peelingMeshBindings.Values)
            if (!ReferenceEquals(cache, meshBindings))
                foreach (var binding in cache.Values) binding.Dispose();
        _peelingMeshBindings.Clear();
        foreach (var cache in _peelingPointLineBindings.Values)
            if (!ReferenceEquals(cache, pointLineBindings))
                foreach (var binding in cache.Values) binding.Dispose();
        _peelingPointLineBindings.Clear();
        foreach (var node in attachedNodes) node.Detach();
        foreach (var intersection in intersectionResources.Values) intersection.Dispose();
        foreach (var retired in retiredIntersections) retired.Dispose();
        intersectionResources.Clear();
        retiredIntersections.Clear();
        intersectionPipelines.Dispose();
        intersectionRoot?.Dispose();
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
        foreach (var binding in effectMeshBindings) binding.Dispose();
        foreach (var binding in effectLineBindings) binding.Dispose();
        effectMeshBindings.Clear();
        effectLineBindings.Clear();
        effectStencil?.Dispose();
        outlineResources?.Dispose();
        outlineBlurResources?.Dispose();
        outlineCatalog?.Dispose();
        postProcessResources?.Dispose();
        postProcessResources = null;
        _effectBindings.Dispose();
        ssaoResources?.Dispose();
        ssaoResources = null;
        scene2DRenderer.Dispose();
        resources.Dispose();
        IsDisposed = true;
    }
}

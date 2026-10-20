using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model;
/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Shaders;
using Silk.NET.DXGI;

namespace HelixToolkit.SharpDX.Core.Native;

/// <summary>
///     Resolves and caches native Direct3D 12 passes for existing geometry scene nodes.
/// </summary>
internal sealed class D3D12ScenePassCatalog : IDisposable {
    /// <summary>
    ///     The Direct3D 12 device used to create native pipelines.
    /// </summary>
    private readonly SilkD3D12Device device;

    /// <summary>
    ///     The complete existing technique descriptions keyed by name.
    /// </summary>
    private readonly IReadOnlyDictionary<string, TechniqueDescription> techniques;

    /// <summary>
    ///     The optional application technique registry used by custom scene nodes.
    /// </summary>
    private readonly IEffectsManager? effectsManager;

    /// <summary>
    ///     Native passes keyed by technique, pass, and primitive topology.
    /// </summary>
    private readonly Dictionary<D3D12ScenePassKey, ShaderPass> passes = [];

    /// <summary>
    ///     The shared root signature.
    /// </summary>
    private readonly SilkD3D12RootSignature rootSignature;

    /// <summary>
    ///     The shared native pipeline cache.
    /// </summary>
    private readonly D3D12PipelineStateCache pipelineCache = new();

    /// <summary>
    ///     The swap-chain render-target format.
    /// </summary>
    private readonly Format renderTargetFormat;

    /// <summary>The immutable output coverage profile.</summary>
    private readonly D3D12ColorOutputMode outputMode;

    /// <summary>Pass descriptions indexed once, avoiding closures during warmed lookups.</summary>
    private readonly Dictionary<(string Technique, string Pass), ShaderPassDescription> descriptions = [];

    /// <summary>The retained weighted accumulation formats.</summary>
    private readonly Format[] weightedFormats = [SilkD3D12TransparencyResources.WeightedColorFormat,
        SilkD3D12TransparencyResources.WeightedAlphaFormat];

    /// <summary>The retained depth-peeling initialization format.</summary>
    private readonly Format[] initializationFormats = [SilkD3D12TransparencyResources.PeelingDepthFormat];

    /// <summary>The retained depth-peeling MRT formats.</summary>
    private readonly Format[] peelingFormats;

    /// <summary>
    ///     The presentation depth/stencil format.
    /// </summary>
    private readonly Format depthStencilFormat;

    /// <summary>
    ///     Initializes a pass catalog for one render-host lifetime.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="renderTargetFormat">The swap-chain render-target format.</param>
    /// <param name="depthStencilFormat">The presentation depth/stencil format.</param>
    /// <param name="customTechniques">Optional application technique descriptions.</param>
    /// <param name="effectsManager">The optional application effects manager.</param>
    /// <param name="outputMode">The immutable shader and blend coverage contract.</param>
    internal D3D12ScenePassCatalog(
        SilkD3D12Device device,
        Format renderTargetFormat,
        Format depthStencilFormat,
        IEnumerable<TechniqueDescription>? customTechniques = null,
        IEffectsManager? effectsManager = null,
        D3D12ColorOutputMode outputMode = D3D12ColorOutputMode.Legacy
    ) {
        device.AsGuardNotNull();
        if (renderTargetFormat == Format.FormatUnknown)
            throw new ArgumentOutOfRangeException(nameof(renderTargetFormat));
        if (depthStencilFormat == Format.FormatUnknown)
            throw new ArgumentOutOfRangeException(nameof(depthStencilFormat));
        this.outputMode = outputMode;
        peelingFormats = [SilkD3D12TransparencyResources.PeelingDepthFormat, renderTargetFormat, renderTargetFormat];
        this.device = device;
        this.renderTargetFormat = renderTargetFormat;
        this.depthStencilFormat = depthStencilFormat;
        this.effectsManager = effectsManager;
        rootSignature = device.CreateDefaultRootSignature();
        var descriptions = DefaultEffectsManager.LoadTechniqueDescriptions()
            .ToDictionary(description => description.Name!, StringComparer.Ordinal);
        if (customTechniques is not null)
            foreach (var description in customTechniques)
                if (description.Name is { } name)
                    descriptions[name] = description;
        techniques = descriptions;
        foreach (var technique in descriptions.Values)
            if (technique.PassDescriptions is { } registeredPasses)
                foreach (var pass in registeredPasses)
                    this.descriptions.Add((technique.Name!, pass.Name!), pass);
    }

    /// <summary>
    ///     Gets whether all cached passes, pipelines, and the root signature have been released.
    /// </summary>
    internal bool IsDisposed { get; private set; }

    /// <summary>
    ///     Resolves or creates the native pass for one already attached scene node.
    /// </summary>
    /// <param name="node">The existing geometry scene node.</param>
    /// <returns>The native pass, or <see langword="null" /> for unsupported nodes.</returns>
    internal ShaderPass? Resolve(SceneNode node) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        node.AsGuardNotNull();
        if (node.RenderCore is MeshIntersectionRenderCore { UseGpu: true })
            return Resolve("MeshIntersect", DefaultPassNames.Default, PrimitiveTopology.LineList);
        if (node is ScreenDuplicationNode)
            return Resolve(DefaultRenderTechniqueNames.ScreenDuplication,
                DefaultPassNames.Default,
                PrimitiveTopology.TriangleStrip);
        if (node.RenderCore is not GeometryRenderCore {GeometryBuffer: not EmptyGeometryBufferModel and { } geometryBuffer}) return null;
        var (defaultTechniqueName, passName) = GetSelection(node);
        var techniqueName = effectsManager is null
            ? defaultTechniqueName
            : ResolveTechniqueName(node);
        if (techniqueName is null || passName is null) return null;
        return ResolveMaterial(node, techniqueName, passName, geometryBuffer.Topology, DefaultPassNames.Wireframe);
    }

    /// <summary>
    ///     Resolves the wireframe overlay pass for a mesh that requests it.
    /// </summary>
    /// <param name="node">The existing mesh scene node.</param>
    /// <returns>The native wireframe pass, or <see langword="null" /> when no overlay is requested.</returns>
    internal ShaderPass? ResolveWireframe(SceneNode node) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (node.RenderCore is not GeometryRenderCore { GeometryBuffer: not EmptyGeometryBufferModel and { } geometryBuffer } geometryCore ||
            geometryCore is not IMeshRenderParams { RenderWireframe: true })
            return null;
        var techniqueName = effectsManager is null
            ? DefaultRenderTechniqueNames.Mesh
            : node.ResolveD3D12TechniqueName(effectsManager);
        if (techniqueName is null || !techniques.TryGetValue(techniqueName, out var technique) ||
            !descriptions.ContainsKey((techniqueName, DefaultPassNames.Wireframe)))
            return null;
        return Resolve(techniqueName, DefaultPassNames.Wireframe, geometryBuffer.Topology);
    }

    /// <summary>
    ///     Resolves the existing point-list stream-output precompute pass for a bone-skinned scene node.
    /// </summary>
    /// <param name="node">The existing scene node.</param>
    /// <returns>The native precompute pass, or <see langword="null" /> for non-bone geometry.</returns>
    internal ShaderPass? ResolveBoneSkinning(SceneNode node) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        node.AsGuardNotNull();
        if (node.RenderCore is not BoneSkinRenderCore) return null;
        return Resolve(DefaultRenderTechniqueNames.Mesh,
            DefaultPassNames.PreComputeMeshBoneSkinned,
            PrimitiveTopology.PointList);
    }

    /// <summary>
    ///     Resolves the existing depth-only shadow pass for a supported geometry node.
    /// </summary>
    /// <param name="node">The existing scene node.</param>
    /// <returns>The native shadow pass, or <see langword="null" /> for unsupported nodes.</returns>
    internal ShaderPass? ResolveShadow(SceneNode node) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        node.AsGuardNotNull();
        if (node is not MeshNode ||
            node.RenderCore is not GeometryRenderCore {GeometryBuffer: not EmptyGeometryBufferModel and { } geometryBuffer})
            return null;
        return Resolve(DefaultRenderTechniqueNames.Mesh,
            DefaultPassNames.ShadowPass,
            geometryBuffer.Topology,
            true);
    }

    /// <summary>
    ///     Resolves the existing backface pass for a volume offscreen target.
    /// </summary>
    /// <param name="node">The existing volume node.</param>
    /// <returns>The native volume backface pass, or <see langword="null" /> for unsupported nodes.</returns>
    internal ShaderPass? ResolveVolumeBack(SceneNode node) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (node is not VolumeTextureNode) return null;
        return Resolve(DefaultRenderTechniqueNames.Volume3D,
            DefaultPassNames.Backface,
            PrimitiveTopology.TriangleList,
            targetFormat: Format.FormatR16G16B16A16Float,
            depthFormat: Format.FormatD32FloatS8X24Uint);
    }

    /// <summary>
    ///     Resolves the current material's final volume pass.
    /// </summary>
    /// <param name="node">The existing volume node.</param>
    /// <returns>The native final volume pass, or <see langword="null" /> for unsupported nodes.</returns>
    internal ShaderPass? ResolveVolume(SceneNode node) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (node is not VolumeTextureNode || node.RenderCore is not VolumeRenderCore core) return null;
        return Resolve(DefaultRenderTechniqueNames.Volume3D,
            core.D3D12MaterialPassName,
            PrimitiveTopology.TriangleList);
    }

    /// <summary>
    ///     Resolves the existing mesh position pass for opaque clipping inside a volume.
    /// </summary>
    /// <param name="node">The existing mesh node.</param>
    /// <returns>The native offscreen position pass, or <see langword="null" /> for unsupported nodes.</returns>
    internal ShaderPass? ResolveVolumePositions(SceneNode node) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (node is not MeshNode || node.RenderCore is not GeometryRenderCore { GeometryBuffer: not EmptyGeometryBufferModel and { } geometry })
            return null;
        return Resolve(DefaultRenderTechniqueNames.Mesh,
            DefaultPassNames.Positions,
            geometry.Topology,
            targetFormat: Format.FormatR16G16B16A16Float,
            depthFormat: Format.FormatD32FloatS8X24Uint);
    }

    /// <summary>
    ///     Resolves the existing particle update compute pass.
    /// </summary>
    /// <param name="node">The existing particle node.</param>
    /// <returns>The native compute pass, or <see langword="null" /> for unsupported nodes.</returns>
    internal ShaderPass? ResolveParticleUpdate(SceneNode node) => node is ParticleStormNode
        ? Resolve(DefaultRenderTechniqueNames.ParticleStorm,
            DefaultParticlePassNames.Update,
            PrimitiveTopology.PointList)
        : null;

    /// <summary>
    ///     Resolves the existing particle insertion compute pass.
    /// </summary>
    /// <param name="node">The existing particle node.</param>
    /// <returns>The native compute pass, or <see langword="null" /> for unsupported nodes.</returns>
    internal ShaderPass? ResolveParticleInsert(SceneNode node) => node is ParticleStormNode
        ? Resolve(DefaultRenderTechniqueNames.ParticleStorm,
            DefaultParticlePassNames.Insert,
            PrimitiveTopology.PointList)
        : null;

    /// <summary>
    ///     Resolves the existing particle graphics pass.
    /// </summary>
    /// <param name="node">The existing particle node.</param>
    /// <returns>The native graphics pass, or <see langword="null" /> for unsupported nodes.</returns>
    internal ShaderPass? ResolveParticle(SceneNode node) => node is ParticleStormNode
        ? Resolve(DefaultRenderTechniqueNames.ParticleStorm,
            DefaultParticlePassNames.Default,
            PrimitiveTopology.PointList)
        : null;

    /// <summary>
    ///     Resolves the weighted-OIT material pass for one transparent geometry node.
    /// </summary>
    /// <param name="node">The transparent scene node.</param>
    /// <returns>The native two-target weighted pass, or <see langword="null" /> when unsupported.</returns>
    internal ShaderPass? ResolveWeightedTransparency(SceneNode node) => ResolveTransparency(node, false, false);

    /// <summary>
    ///     Resolves the first min/max-depth pass for one transparent geometry node.
    /// </summary>
    /// <param name="node">The transparent scene node.</param>
    /// <returns>The native initialization pass, or <see langword="null" /> when unsupported.</returns>
    internal ShaderPass? ResolveDepthPeelingInitialization(SceneNode node) => ResolveTransparency(node, true, true);

    /// <summary>
    ///     Resolves the three-target depth-peeling pass for one transparent geometry node.
    /// </summary>
    /// <param name="node">The transparent scene node.</param>
    /// <returns>The native peeling pass, or <see langword="null" /> when unsupported.</returns>
    internal ShaderPass? ResolveDepthPeeling(SceneNode node) => ResolveTransparency(node, true, false);

    /// <summary>
    ///     Resolves the existing normal/depth geometry pass used by SSAO.
    /// </summary>
    /// <param name="node">The opaque scene node.</param>
    /// <returns>The native SSAO geometry pass, or <see langword="null" /> when unsupported.</returns>
    internal ShaderPass? ResolveSsaoGeometry(SceneNode node) {
        if (node is not MeshNode || node.RenderCore is not GeometryRenderCore { GeometryBuffer: not EmptyGeometryBufferModel and { } geometry })
            return null;
        return Resolve(DefaultRenderTechniqueNames.Mesh,
            DefaultPassNames.MeshSsaoPass,
            geometry.Topology,
            targetFormat: SilkD3D12SsaoResources.NormalFormat,
            depthFormat: Format.FormatD32Float);
    }

    /// <summary>
    ///     Resolves the weighted-OIT full-screen composition pass.
    /// </summary>
    /// <returns>The native full-screen pass.</returns>
    internal ShaderPass ResolveWeightedComposition() => Resolve(DefaultRenderTechniqueNames.MeshOitQuad,
        DefaultPassNames.Default,
        PrimitiveTopology.TriangleStrip,
        depthFormat: Format.FormatUnknown);

    /// <summary>
    ///     Resolves the depth-peeling full-screen composition pass.
    /// </summary>
    /// <returns>The native full-screen pass.</returns>
    internal ShaderPass ResolveDepthPeelingComposition() => Resolve(
        DefaultRenderTechniqueNames.MeshOitDepthPeeling,
        DefaultPassNames.OitDepthPeelingFinal,
        PrimitiveTopology.TriangleStrip,
        depthFormat: Format.FormatUnknown);

    /// <summary>
    ///     Resolves a full-screen pass for its actual intermediate or presentation render-target format.
    /// </summary>
    /// <param name="techniqueName">The existing post-processing technique.</param>
    /// <param name="passName">The existing pass name.</param>
    /// <returns>The cached native full-screen pass.</returns>
    internal ShaderPass ResolvePostProcess(string techniqueName, string passName) {
        var format = renderTargetFormat;
        if (techniqueName == DefaultRenderTechniqueNames.Ssao)
            format = SilkD3D12SsaoResources.OcclusionFormat;
        return Resolve(techniqueName, passName, PrimitiveTopology.TriangleStrip,
            targetFormat: format, depthFormat: passName == DefaultPassNames.ScreenQuad &&
                techniqueName is DefaultRenderTechniqueNames.PostEffectMeshOutlineBlur or
                    DefaultRenderTechniqueNames.PostEffectMeshBorderHighlight
                ? Format.FormatD32FloatS8X24Uint : Format.FormatUnknown);
    }

    /// <summary>
    ///     Resolves the existing alpha-blended Sprite2D pass for native overlay rendering.
    /// </summary>
    /// <returns>The cached sprite pass.</returns>
    internal ShaderPass ResolveSprite2D() => Resolve(DefaultRenderTechniqueNames.Sprite2D,
        DefaultPassNames.Default,
        PrimitiveTopology.TriangleList,
        depthFormat: Format.FormatUnknown);

    /// <summary>
    ///     Resolves the existing full-screen desktop-duplication pass.
    /// </summary>
    /// <returns>The native screen-duplication pass.</returns>
    internal ShaderPass ResolveScreenDuplication() => Resolve(DefaultRenderTechniqueNames.ScreenDuplication,
        DefaultPassNames.Default,
        PrimitiveTopology.TriangleStrip);

    /// <summary>
    ///     Resolves a geometry custom pass for its outline-mask or presentation render-target format.
    /// </summary>
    /// <param name="node">The affected geometry node.</param>
    /// <param name="passName">The existing custom pass name.</param>
    /// <param name="depthFormat">The bound depth/stencil format.</param>
    /// <param name="stencilReference">The source single-pass XRay stencil reference override.</param>
    /// <returns>The native custom pass, or <see langword="null" /> when the node or pass is unsupported.</returns>
    internal ShaderPass? ResolvePostEffectGeometry(SceneNode node, string passName, Format depthFormat,
        int? stencilReference = null) {
        if (node.RenderCore is MeshIntersectionRenderCore { UseGpu: true })
            return descriptions.ContainsKey(("MeshIntersect", passName))
                ? Resolve("MeshIntersect", passName, PrimitiveTopology.LineList, depthFormat: depthFormat,
                    stencilReference: stencilReference)
                : null;
        if (node.RenderCore is not GeometryRenderCore { GeometryBuffer: not EmptyGeometryBufferModel and { } geometry } core) return null;
        var (techniqueName, _) = GetSelection(node);
        if (effectsManager is not null) techniqueName = ResolveTechniqueName(node);
        if (techniqueName is null || !techniques.TryGetValue(techniqueName, out var technique) ||
            !descriptions.ContainsKey((techniqueName, passName)))
            return null;
        return Resolve(techniqueName,
            passName,
            geometry.Topology,
            targetFormat: renderTargetFormat,
            depthFormat: depthFormat, stencilReference: stencilReference, rasterOverride: core.RasterDescription);
    }

    /// <summary>
    ///     Resolves and caches one named pass from an existing technique.
    /// </summary>
    /// <param name="techniqueName">The existing technique name.</param>
    /// <param name="passName">The existing pass name.</param>
    /// <param name="fallbackTopology">The geometry topology used when the pass has no override.</param>
    /// <param name="depthOnly">Whether the pipeline omits color render targets and uses D32 depth.</param>
    /// <param name="targetFormat">The optional single render-target format.</param>
    /// <param name="depthFormat">The optional depth/stencil format override.</param>
    /// <param name="targetFormats">The optional ordered render-target formats.</param>
    /// <param name="rasterOverride">The source wireframe rasterizer state, when creating an overlay.</param>
    /// <param name="wireframePass">The borrowed catalog-owned overlay attached only to material passes.</param>
    /// <param name="stencilReference">The optional source stencil reference override.</param>
    /// <returns>The cached native pass.</returns>
    /// <exception cref="InvalidOperationException">The requested technique or pass is not registered.</exception>
    private ShaderPass Resolve(
        string techniqueName,
        string passName,
        PrimitiveTopology fallbackTopology,
        bool depthOnly = false,
        Format? targetFormat = null,
        Format? depthFormat = null,
        IReadOnlyList<Format>? targetFormats = null,
        RasterizerStateDescription? rasterOverride = null,
        ShaderPass? wireframePass = null,
        int? stencilReference = null
    ) {
        if (!techniques.TryGetValue(techniqueName, out var technique))
            throw new InvalidOperationException($"DX12 technique '{techniqueName}' is not registered.");
        if (!descriptions.TryGetValue((techniqueName, passName), out var passDescription))
            throw new InvalidOperationException($"DX12 pass '{passName}' is not registered by '{techniqueName}'.");
        var topology = passDescription.Topology == PrimitiveTopology.Undefined
            ? fallbackTopology
            : passDescription.Topology;
        var targetCount = depthOnly ? 0 : targetFormats?.Count ?? 1;
        ulong targetBits = 0;
        for (var index = 0; index < targetCount; index++) {
            var format = targetFormats is null ? targetFormat ?? renderTargetFormat : targetFormats[index];
            targetBits |= (ulong)format << (index * 8);
        }
        var selectedDepthFormat = depthOnly ? Format.FormatD32Float : depthFormat ?? depthStencilFormat;
        var key = new D3D12ScenePassKey(techniqueName,
            passName,
            topology,
            targetBits,
            targetCount,
            selectedDepthFormat,
            CreateRasterKey(rasterOverride),
            wireframePass,
            stencilReference);
        if (passes.TryGetValue(key, out var pass)) return pass;
        var selectedTargetFormats = new Format[targetCount];
        for (var index = 0; index < targetCount; index++)
            selectedTargetFormats[index] = (Format)((targetBits >> (index * 8)) & 255);
        var blend = SelectOutputBlend(passDescription.BlendStateDescription, techniqueName, passName,
            selectedTargetFormats);
        if (rasterOverride.HasValue || stencilReference.HasValue || outputMode == D3D12ColorOutputMode.Premultiplied)
            passDescription = new ShaderPassDescription(passDescription.Name!) {
                ShaderList = SelectOutputShaders(passDescription.ShaderList),
                InputLayoutDescription = passDescription.InputLayoutDescription,
                BlendStateDescription = blend,
                DepthStencilStateDescription = passDescription.DepthStencilStateDescription,
                RasterStateDescription = rasterOverride ?? passDescription.RasterStateDescription,
                BlendFactor = passDescription.BlendFactor,
                SampleMask = passDescription.SampleMask,
                StencilRef = stencilReference ?? passDescription.StencilRef,
                Topology = passDescription.Topology
            };
        pass = passDescription.CreateD3D12(device,
            rootSignature,
            pipelineCache,
            technique.InputLayoutDescription,
            fallbackTopology,
            selectedTargetFormats.Where(format => format != Format.FormatUnknown).ToArray(),
            selectedDepthFormat);
        pass.D3D12WireframePass = wireframePass;
        passes.Add(key, pass);
        return pass;
    }

    /// <summary>Isolates profile-only global reads from each legacy shader's existing descriptor contract.</summary>
    /// <param name="shaders">The existing pass stages, retained for legacy or application shaders.</param>
    /// <returns>The original stages or private profile pixel-stage substitutions created on a pass-cache miss.</returns>
    private IList<ShaderDescription>? SelectOutputShaders(IList<ShaderDescription>? shaders) {
        if (outputMode != D3D12ColorOutputMode.Premultiplied || shaders is null) return shaders;
        var result = new ShaderDescription[shaders.Count];
        for (var index = 0; index < shaders.Count; index++) {
            var shader = shaders[index];
            var name = shader.ByteCodeName;
            var builtInStage = name switch {
                "psScreenDup" => DefaultPSShaderDescriptions.PsScreenDup,
                "psLuma" => DefaultPSShaderDescriptions.PsEffectLuma,
                "psFXAA" => DefaultPSShaderDescriptions.PsEffectFxaa,
                "psEffectBloomExtract" => DefaultPSShaderDescriptions.PsEffectBloomExtract,
                "psEffectBloomCombine" => DefaultPSShaderDescriptions.PsEffectBloomCombine,
                "psEffectMeshXRay" => DefaultPSShaderDescriptions.PsEffectMeshXRay,
                "psParticle" => DefaultPSShaderDescriptions.PsParticle,
                "psParticleOIT" => DefaultPSShaderDescriptions.PsParticleOit,
                "psParticleOITDP" => DefaultPSShaderDescriptions.PsParticleOitdp,
                "psVolume" => DefaultPSShaderDescriptions.PsVolume3D,
                "psVolumeDiffuse" => DefaultPSShaderDescriptions.PsVolumeDiffuse3D,
                "psMeshBlinnPhongOITQuad" => DefaultPSShaderDescriptions.PsMeshBlinnPhongOitQuad,
                "psMeshOITDPFinal" => DefaultPSShaderDescriptions.PsMeshOitdpFinal,
                _ => null
            };
            var profileStage = shader.ShaderType == ShaderStage.Pixel && ReferenceEquals(shader, builtInStage);
            result[index] = profileStage
                ? new ShaderDescription(
                    $"{name}Premultiplied", ShaderStage.Pixel,
                    new ShaderReflector(), $"{name}Premultiplied"
                )
                : shader;
        }

        return result;
    }

    /// <summary>Selects coverage-correct blending while retaining explicitly typed non-color accumulators.</summary>
    /// <param name="original">The existing pass blend state.</param>
    /// <param name="technique">The technique identity.</param>
    /// <param name="pass">The pass identity.</param>
    /// <param name="formats">The actual ordered render-target formats.</param>
    /// <returns>The original legacy state or an isolated premultiplied-output state.</returns>
    private BlendStateDescription? SelectOutputBlend(BlendStateDescription? original, string technique,
        string pass, IReadOnlyList<Format> formats) {
        if (outputMode != D3D12ColorOutputMode.Premultiplied) return original;
        if (formats.Count == 3 && formats[0] == SilkD3D12TransparencyResources.PeelingDepthFormat) {
            var peeling = original ?? new BlendStateDescription();
            peeling.RenderTarget = (RenderTargetBlendDescription[])peeling.RenderTarget.Clone();
            peeling.RenderTarget[2].SourceAlphaBlend = BlendOption.One;
            peeling.RenderTarget[2].DestinationAlphaBlend = BlendOption.InverseSourceAlpha;
            return peeling;
        }
        if (formats.Count != 1 || formats[0] is not (Format.FormatR8G8B8A8Unorm or Format.FormatB8G8R8A8Unorm))
            return original;
        if (technique is DefaultRenderTechniqueNames.PostEffectFxaa or DefaultRenderTechniqueNames.MeshOitDepthPeeling)
            return original;
        var postEffect = technique is DefaultRenderTechniqueNames.PostEffectBloom or
            DefaultRenderTechniqueNames.PostEffectMeshOutlineBlur or DefaultRenderTechniqueNames.PostEffectMeshBorderHighlight;
        if (postEffect && pass != DefaultPassNames.MeshOutline) return original;
        var result = original ?? new BlendStateDescription();
        result.RenderTarget = (RenderTargetBlendDescription[])result.RenderTarget.Clone();
        var target = result.RenderTarget[0];
        if (target.RenderTargetWriteMask == 0) return original;
        var additive = target.IsBlendEnabled && target.SourceBlend == BlendOption.One &&
            target.DestinationBlend == BlendOption.One && target.BlendOperation == BlendOperation.Add;
        target.IsBlendEnabled = true;
        target.SourceBlend = postEffect || technique == DefaultRenderTechniqueNames.Volume3D
            ? BlendOption.One : BlendOption.SourceAlpha;
        target.DestinationBlend = additive ? BlendOption.One : BlendOption.InverseSourceAlpha;
        target.BlendOperation = BlendOperation.Add;
        target.SourceAlphaBlend = BlendOption.One;
        // Summed coverage also bounds summed RGB when additive layers overlap on transparent targets.
        target.DestinationAlphaBlend = additive ? BlendOption.One : BlendOption.InverseSourceAlpha;
        target.AlphaBlendOperation = BlendOperation.Add;
        target.RenderTargetWriteMask = ColorWriteMaskFlags.All;
        result.RenderTarget[0] = target;
        return result;
    }

    /// <summary>
    ///     Resolves one transparent geometry stage using the existing material and technique names.
    /// </summary>
    /// <param name="node">The transparent scene node.</param>
    /// <param name="depthPeeling">Whether to select depth peeling instead of weighted OIT.</param>
    /// <param name="initialize">Whether to select the depth-only initialization pass.</param>
    /// <returns>The selected native pass, or <see langword="null" /> when unsupported.</returns>
    private ShaderPass? ResolveTransparency(SceneNode node, bool depthPeeling, bool initialize) {
        if (node.RenderCore is not GeometryRenderCore { GeometryBuffer: not EmptyGeometryBufferModel and { } geometry }) return null;
        var (technique, _) = GetSelection(node);
        if (effectsManager is not null) technique = ResolveTechniqueName(node);
        if (technique is null) return null;
        var passName = (effectsManager as ID3D12MeshShaderProvider)?.GetTransparencyPassName(
            node, depthPeeling, initialize) ?? GetTransparencyPassName(node, depthPeeling, initialize);
        if (passName is null) return null;
        IReadOnlyList<Format> formats = initialize ? initializationFormats
            : depthPeeling ? peelingFormats : weightedFormats;
        var wireframeName = initialize ? DefaultPassNames.OitDepthPeelingInit
            : depthPeeling ? DefaultPassNames.WireframeOitdpPass : DefaultPassNames.WireframeOitPass;
        return ResolveMaterial(node, technique, passName, geometry.Topology, wireframeName, formats);
    }

    /// <summary>
    ///     Pairs a productive material pass with its registered source wireframe pass and per-mesh rasterizer state.
    /// </summary>
    /// <param name="node">The current scene node.</param>
    /// <param name="techniqueName">The actual registered material technique.</param>
    /// <param name="passName">The productive material pass name.</param>
    /// <param name="topology">The original geometry topology.</param>
    /// <param name="wireframeName">The wireframe pass for the active transparency stage.</param>
    /// <param name="targetFormats">The ordered transparency targets, or the main target when omitted.</param>
    /// <returns>The material pass borrowing its optional overlay from this catalog.</returns>
    private ShaderPass ResolveMaterial(SceneNode node, string techniqueName, string passName,
        PrimitiveTopology topology, string wireframeName, IReadOnlyList<Format>? targetFormats = null) {
        ShaderPass? wireframe = null;
        if (node.RenderCore is MeshRenderCore { RenderWireframe: true } mesh &&
            descriptions.ContainsKey((techniqueName, wireframeName))) {
            var raster = mesh.RasterDescription;
            raster.FillMode = FillMode.Wireframe;
            raster.DepthBias = -100;
            raster.SlopeScaledDepthBias = -2f;
            raster.DepthBiasClamp = -0.00008f;
            wireframe = Resolve(techniqueName, wireframeName, topology,
                targetFormats: targetFormats, rasterOverride: raster);
        }
        var materialRaster = node.RenderCore is MeshRenderCore materialMesh
            ? materialMesh.RasterDescription : (RasterizerStateDescription?) null;
        return Resolve(techniqueName, passName, topology,
            targetFormats: targetFormats, rasterOverride: materialRaster, wireframePass: wireframe);
    }

    /// <summary>
    ///     Resolves explicit node overrides before an ordinary mesh material's technique.
    /// </summary>
    /// <param name="node">The current scene node.</param>
    /// <returns>The registered productive technique name.</returns>
    private string ResolveTechniqueName(SceneNode node) {
        var selected = node.ResolveD3D12TechniqueName(effectsManager!);
        if (effectsManager is ID3D12MeshShaderProvider provider) return provider.GetTechniqueName(node, selected);
        return selected == DefaultRenderTechniqueNames.Mesh &&
            node is MaterialGeometryNode { Material: ID3D12MeshMaterial material }
                ? material.TechniqueName : selected;
    }

    /// <summary>
    ///     Selects the repository pass name for one transparency stage.
    /// </summary>
    /// <param name="node">The transparent scene node.</param>
    /// <param name="depthPeeling">Whether depth peeling is active.</param>
    /// <param name="initialize">Whether the min/max initialization pass is active.</param>
    /// <returns>The existing pass name, or <see langword="null" /> for unsupported nodes.</returns>
    internal static string? GetTransparencyPassName(SceneNode node, bool depthPeeling, bool initialize) {
        if (initialize) return node is MeshNode or BillboardNode or LineNode or PointNode
            ? DefaultPassNames.OitDepthPeelingInit
            : null;
        if (node is MeshNode && node.RenderCore is MeshRenderCore mesh)
            return mesh.D3D12Material switch {
                ID3D12MeshMaterial => depthPeeling ? DefaultPassNames.OitDepthPeeling : DefaultPassNames.OitPass,
                DiffuseMaterialCore => depthPeeling ? DefaultPassNames.DiffuseOitdp : DefaultPassNames.DiffuseOit,
                PBRMaterialCore { EnableTessellation: true } => depthPeeling
                    ? DefaultPassNames.MeshPbrTriTessellationOitdp
                    : DefaultPassNames.MeshPbrTriTessellationOit,
                PBRMaterialCore => depthPeeling ? DefaultPassNames.PbroitdpPass : DefaultPassNames.PbroitPass,
                PhongMaterialCore { EnableTessellation: true } => depthPeeling
                    ? DefaultPassNames.MeshTriTessellationOitdp
                    : DefaultPassNames.MeshTriTessellationOit,
                PhongMaterialCore => depthPeeling ? DefaultPassNames.OitDepthPeeling : DefaultPassNames.OitPass,
                _ => null
            };
        return node is BillboardNode or LineNode or PointNode
            ? depthPeeling ? DefaultPassNames.OitDepthPeeling : DefaultPassNames.OitPass
            : null;
    }

    /// <summary>
    ///     Selects the existing technique and productive material pass for one supported scene node.
    /// </summary>
    /// <param name="node">The scene node.</param>
    /// <returns>The technique and pass names, or null names for unsupported nodes.</returns>
    internal static (string? Technique, string? Pass) GetSelection(SceneNode node) => node switch {
        ScreenDuplicationNode => (DefaultRenderTechniqueNames.ScreenDuplication, DefaultPassNames.Default),
        BillboardNode => (DefaultRenderTechniqueNames.BillboardText, DefaultPassNames.Default),
        LineNode => (DefaultRenderTechniqueNames.Lines, DefaultPassNames.Default),
        PointNode => (DefaultRenderTechniqueNames.Points, DefaultPassNames.Default),
        ParticleStormNode => (DefaultRenderTechniqueNames.ParticleStorm, DefaultParticlePassNames.Default),
        VolumeTextureNode when node.RenderCore is VolumeRenderCore volume =>
            (DefaultRenderTechniqueNames.Volume3D, volume.D3D12MaterialPassName),
        MeshNode when node.RenderCore is MeshRenderCore mesh =>
            (DefaultRenderTechniqueNames.Mesh, mesh.D3D12MaterialPassName),
        _ => (null, null)
    };

    /// <summary>
    ///     Releases every pass before its shared cache and root signature.
    /// </summary>
    public void Dispose() {
        if (IsDisposed) return;
        foreach (var pass in passes.Values) pass.Dispose();
        passes.Clear();
        pipelineCache.Dispose();
        rootSignature.Dispose();
        IsDisposed = true;
    }

    /// <summary>Creates a semantic value key without boxing the legacy rasterizer description.</summary>
    /// <param name="raster">The optional source rasterizer state.</param>
    /// <returns>The exact flags, bias and slope used to identify the pipeline.</returns>
    private static D3D12RasterKey CreateRasterKey(RasterizerStateDescription? raster) {
        if (raster is not { } value) return default;
        var flags = 1u << 31 | (uint)value.FillMode | (uint)value.CullMode << 4;
        if (value.IsFrontCounterClockwise) flags |= 1 << 8;
        if (value.IsDepthClipEnabled) flags |= 1 << 9;
        if (value.IsScissorEnabled) flags |= 1 << 10;
        if (value.IsMultisampleEnabled) flags |= 1 << 11;
        if (value.IsAntialiasedLineEnabled) flags |= 1 << 12;
        return new D3D12RasterKey(flags, value.DepthBias, value.DepthBiasClamp, value.SlopeScaledDepthBias);
    }

    /// <summary>Identifies a rasterizer without the legacy struct's object-based equality or hashing.</summary>
    /// <param name="Flags">The presence, fill, culling and boolean state bits.</param>
    /// <param name="DepthBias">The integer depth bias.</param>
    /// <param name="DepthBiasClamp">The depth-bias clamp.</param>
    /// <param name="SlopeScaledDepthBias">The slope-scaled depth bias.</param>
    private readonly record struct D3D12RasterKey(uint Flags, int DepthBias, float DepthBiasClamp,
        float SlopeScaledDepthBias);

    /// <summary>
    ///     Identifies one cached scene pass.
    /// </summary>
    /// <param name="Technique">The technique name.</param>
    /// <param name="Pass">The pass name.</param>
    /// <param name="Topology">The concrete draw topology.</param>
    /// <param name="TargetFormats">The packed ordered native color target formats.</param>
    /// <param name="TargetCount">The number of packed target formats.</param>
    /// <param name="DepthFormat">The native depth/stencil format.</param>
    /// <param name="RasterOverride">The optional per-mesh material or wireframe rasterizer state.</param>
    /// <param name="WireframePass">The optional borrowed overlay identity.</param>
    /// <param name="StencilReference">The optional source stencil reference override.</param>
    private readonly record struct D3D12ScenePassKey(
        string Technique,
        string Pass,
        PrimitiveTopology Topology,
        ulong TargetFormats,
        int TargetCount,
        Format DepthFormat,
        D3D12RasterKey RasterOverride,
        ShaderPass? WireframePass,
        int? StencilReference
    );
}

/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.DefaultShaders;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.ShaderManager;
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
    internal D3D12ScenePassCatalog(
        SilkD3D12Device device,
        Format renderTargetFormat,
        Format depthStencilFormat,
        IEnumerable<TechniqueDescription>? customTechniques = null,
        IEffectsManager? effectsManager = null
    ) {
        device.AsGuardNotNull();
        if (renderTargetFormat == Format.FormatUnknown)
            throw new ArgumentOutOfRangeException(nameof(renderTargetFormat));
        if (depthStencilFormat == Format.FormatUnknown)
            throw new ArgumentOutOfRangeException(nameof(depthStencilFormat));
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
        if (node.RenderCore is not GeometryRenderCore {GeometryBuffer: { } geometryBuffer}) return null;
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
        if (node.RenderCore is not GeometryRenderCore { GeometryBuffer: { } geometryBuffer } geometryCore ||
            geometryCore is not IMeshRenderParams { RenderWireframe: true })
            return null;
        var techniqueName = effectsManager is null
            ? DefaultRenderTechniqueNames.Mesh
            : node.ResolveD3D12TechniqueName(effectsManager);
        if (techniqueName is null || !techniques.TryGetValue(techniqueName, out var technique) ||
            technique.PassDescriptions?.Any(description => description.Name == DefaultPassNames.Wireframe) != true)
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
            node.RenderCore is not GeometryRenderCore {GeometryBuffer: { } geometryBuffer})
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
        if (node is not MeshNode || node.RenderCore is not GeometryRenderCore { GeometryBuffer: { } geometry })
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
        if (node is not MeshNode || node.RenderCore is not GeometryRenderCore { GeometryBuffer: { } geometry })
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
        else if (techniqueName is DefaultRenderTechniqueNames.PostEffectMeshOutlineBlur or
                 DefaultRenderTechniqueNames.PostEffectMeshBorderHighlight && passName != DefaultPassNames.MeshOutline)
            format = SilkD3D12PostProcessResources.TargetFormat;
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
            return techniques["MeshIntersect"].PassDescriptions!.Any(pass => pass.Name == passName)
                ? Resolve("MeshIntersect", passName, PrimitiveTopology.LineList, depthFormat: depthFormat,
                    stencilReference: stencilReference)
                : null;
        if (node.RenderCore is not GeometryRenderCore { GeometryBuffer: { } geometry } core) return null;
        var (techniqueName, _) = GetSelection(node);
        if (effectsManager is not null) techniqueName = ResolveTechniqueName(node);
        if (techniqueName is null || !techniques.TryGetValue(techniqueName, out var technique) ||
            technique.PassDescriptions?.Any(description => description.Name == passName) != true)
            return null;
        return Resolve(techniqueName,
            passName,
            geometry.Topology,
            targetFormat: passName == DefaultPassNames.EffectOutlineP1
                ? SilkD3D12PostProcessResources.TargetFormat
                : renderTargetFormat,
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
        var passDescription = technique.PassDescriptions?
            .SingleOrDefault(description => description.Name == passName)
            ?? throw new InvalidOperationException($"DX12 pass '{passName}' is not registered by '{techniqueName}'.");
        var topology = passDescription.Topology == PrimitiveTopology.Undefined
            ? fallbackTopology
            : passDescription.Topology;
        var selectedTargetFormats = depthOnly
            ? Array.Empty<Format>()
            : targetFormats?.ToArray() ?? [targetFormat ?? renderTargetFormat];
        var selectedDepthFormat = depthOnly ? Format.FormatD32Float : depthFormat ?? depthStencilFormat;
        var key = new D3D12ScenePassKey(techniqueName,
            passName,
            topology,
            string.Join(',', selectedTargetFormats),
            selectedDepthFormat,
            rasterOverride,
            wireframePass,
            stencilReference);
        if (passes.TryGetValue(key, out var pass)) return pass;
        if (rasterOverride.HasValue || stencilReference.HasValue)
            passDescription = new ShaderPassDescription(passDescription.Name!) {
                ShaderList = passDescription.ShaderList,
                InputLayoutDescription = passDescription.InputLayoutDescription,
                BlendStateDescription = passDescription.BlendStateDescription,
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

    /// <summary>
    ///     Resolves one transparent geometry stage using the existing material and technique names.
    /// </summary>
    /// <param name="node">The transparent scene node.</param>
    /// <param name="depthPeeling">Whether to select depth peeling instead of weighted OIT.</param>
    /// <param name="initialize">Whether to select the depth-only initialization pass.</param>
    /// <returns>The selected native pass, or <see langword="null" /> when unsupported.</returns>
    private ShaderPass? ResolveTransparency(SceneNode node, bool depthPeeling, bool initialize) {
        if (node.RenderCore is not GeometryRenderCore { GeometryBuffer: { } geometry }) return null;
        var (technique, _) = GetSelection(node);
        if (effectsManager is not null) technique = ResolveTechniqueName(node);
        if (technique is null) return null;
        var passName = GetTransparencyPassName(node, depthPeeling, initialize);
        if (passName is null) return null;
        IReadOnlyList<Format> formats = initialize
            ? [SilkD3D12TransparencyResources.PeelingDepthFormat]
            : depthPeeling
                ? [
                    SilkD3D12TransparencyResources.PeelingDepthFormat,
                    renderTargetFormat,
                    renderTargetFormat
                ]
                : [
                    SilkD3D12TransparencyResources.WeightedColorFormat,
                    SilkD3D12TransparencyResources.WeightedAlphaFormat
                ];
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
            techniques[techniqueName].PassDescriptions?.Any(description => description.Name == wireframeName) == true) {
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
                PbrMaterialCore { EnableTessellation: true } => depthPeeling
                    ? DefaultPassNames.MeshPbrTriTessellationOitdp
                    : DefaultPassNames.MeshPbrTriTessellationOit,
                PbrMaterialCore => depthPeeling ? DefaultPassNames.PbroitdpPass : DefaultPassNames.PbroitPass,
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

    /// <summary>
    ///     Identifies one cached scene pass.
    /// </summary>
    /// <param name="Technique">The technique name.</param>
    /// <param name="Pass">The pass name.</param>
    /// <param name="Topology">The concrete draw topology.</param>
    /// <param name="TargetFormats">The ordered native color target formats.</param>
    /// <param name="DepthFormat">The native depth/stencil format.</param>
    /// <param name="RasterOverride">The optional per-mesh material or wireframe rasterizer state.</param>
    /// <param name="WireframePass">The optional borrowed overlay identity.</param>
    /// <param name="StencilReference">The optional source stencil reference override.</param>
    private readonly record struct D3D12ScenePassKey(
        string Technique,
        string Pass,
        PrimitiveTopology Topology,
        string TargetFormats,
        Format DepthFormat,
        RasterizerStateDescription? RasterOverride,
        ShaderPass? WireframePass,
        int? StencilReference
    );
}

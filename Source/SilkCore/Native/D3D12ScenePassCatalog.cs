/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.DefaultShaders;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
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
        Format depthStencilFormat
    ) {
        device.AssertArgumentNotNull();
        if (renderTargetFormat == Format.FormatUnknown)
            throw new ArgumentOutOfRangeException(nameof(renderTargetFormat));
        if (depthStencilFormat == Format.FormatUnknown)
            throw new ArgumentOutOfRangeException(nameof(depthStencilFormat));
        this.device = device;
        this.renderTargetFormat = renderTargetFormat;
        this.depthStencilFormat = depthStencilFormat;
        rootSignature = device.CreateDefaultRootSignature();
        techniques = DefaultEffectsManager.LoadTechniqueDescriptions()
            .ToDictionary(description => description.Name, StringComparer.Ordinal);
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
        node.AssertArgumentNotNull();
        if (node.RenderCore is not GeometryRenderCore {GeometryBuffer: { } geometryBuffer}) return null;
        var (techniqueName, passName) = GetSelection(node);
        if (techniqueName is null || passName is null) return null;
        var key = new D3D12ScenePassKey(techniqueName, passName, geometryBuffer.Topology);
        if (passes.TryGetValue(key, out var pass)) return pass;
        if (!techniques.TryGetValue(techniqueName, out var technique))
            throw new InvalidOperationException($"DX12 technique '{techniqueName}' is not registered.");
        var passDescription = technique.PassDescriptions?
            .SingleOrDefault(description => description.Name == passName)
            ?? throw new InvalidOperationException($"DX12 pass '{passName}' is not registered by '{techniqueName}'.");
        pass = passDescription.CreateD3D12(device,
            rootSignature,
            pipelineCache,
            technique.InputLayoutDescription,
            geometryBuffer.Topology,
            [renderTargetFormat],
            depthStencilFormat);
        passes.Add(key, pass);
        return pass;
    }

    /// <summary>
    ///     Selects the existing technique and productive material pass for one supported scene node.
    /// </summary>
    /// <param name="node">The scene node.</param>
    /// <returns>The technique and pass names, or null names for unsupported nodes.</returns>
    internal static (string? Technique, string? Pass) GetSelection(SceneNode node) => node switch {
        BillboardNode => (DefaultRenderTechniqueNames.BillboardText, DefaultPassNames.Default),
        LineNode => (DefaultRenderTechniqueNames.Lines, DefaultPassNames.Default),
        PointNode => (DefaultRenderTechniqueNames.Points, DefaultPassNames.Default),
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
    private readonly record struct D3D12ScenePassKey(
        string Technique,
        string Pass,
        PrimitiveTopology Topology
    );
}

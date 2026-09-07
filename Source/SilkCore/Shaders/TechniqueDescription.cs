/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.Serialization;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Native;

namespace HelixToolkit.SharpDX.Core.Shaders;
/// <summary>
/// </summary>
[DataContract]
public sealed class TechniqueDescription {
    /// <summary>
    ///     Initializes a new instance of the <see cref="TechniqueDescription" /> class.
    /// </summary>
    public TechniqueDescription() { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="TechniqueDescription" /> class.
    /// </summary>
    /// <param name="name">The name.</param>
    public TechniqueDescription(string name) {
        Name = name;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="TechniqueDescription" /> class.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="inputLayout">The input layout.</param>
    public TechniqueDescription(string name, InputLayoutDescription inputLayout)
        : this(name) {
        InputLayoutDescription = inputLayout;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="TechniqueDescription" /> class.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="inputLayout">The input layout.</param>
    /// <param name="shaderPasses">The shader passes.</param>
    public TechniqueDescription(
        string name,
        InputLayoutDescription inputLayout,
        IList<ShaderPassDescription> shaderPasses
    )
        : this(name, inputLayout) {
        PassDescriptions = shaderPasses;
    }

    /// <summary>
    ///     Technique Name
    /// </summary>
    [DataMember(Name = @"Name")]
    public string? Name { get; set; }

    /// <summary>
    ///     Input Layout
    /// </summary>
    [DataMember(Name = @"InputLayoutDescription")]
    public InputLayoutDescription? InputLayoutDescription { get; set; }

    /// <summary>
    ///     Gets or sets the pass descriptions.
    /// </summary>
    /// <value>
    ///     The pass descriptions.
    /// </value>
    [DataMember(Name = @"PassDescriptions")]
    public IList<ShaderPassDescription>? PassDescriptions { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether this technique is null technique.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance is null; otherwise, <c>false</c>.
    /// </value>
    public bool IsNull { get; set; } = false;
}

/// <summary>
/// </summary>
[DataContract(Name = @"ShaderPassDescription")]
public sealed class ShaderPassDescription {
    public ShaderPassDescription() { }

    public ShaderPassDescription(string name) {
        Name = name;
    }

    /// <summary>
    ///     Resolves this pass's unique shader stages to immutable SM6 DXIL modules.
    /// </summary>
    /// <returns>The Direct3D 12 shader modules keyed by stage.</returns>
    public IReadOnlyDictionary<ShaderStage, D3D12ShaderModule> GetD3D12ShaderModules() {
        var result = new Dictionary<ShaderStage, D3D12ShaderModule>();
        foreach (var shader in ShaderList ?? []) {
            if (shader.ShaderType == ShaderStage.None)
                throw new InvalidOperationException($"Shader pass '{Name}' contains an unspecified shader stage.");
            var module = shader.D3D12Module
                         ?? throw new InvalidOperationException($"Shader '{shader.Name}' has no DXIL module.");
            if (!result.TryAdd(shader.ShaderType, module))
                throw new InvalidOperationException(
                    $"Shader pass '{Name}' contains more than one {shader.ShaderType} shader.");
        }

        if (result.ContainsKey(ShaderStage.Compute) && result.Count != 1)
            throw new InvalidOperationException($"Compute shader pass '{Name}' cannot contain graphics stages.");

        return result;
    }

    /// <summary>
    ///     Creates or reuses the Direct3D 12 pipeline for this pass and returns its command-binding wrapper.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="rootSignature">The shared root signature.</param>
    /// <param name="cache">The owning pipeline-state cache.</param>
    /// <param name="fallbackInputLayout">The technique input layout used when the pass has no override.</param>
    /// <param name="fallbackTopology">The draw topology used when the pass has no override.</param>
    /// <param name="renderTargetFormats">The ordered render-target formats.</param>
    /// <param name="depthStencilFormat">The optional depth/stencil format.</param>
    /// <returns>The Direct3D 12 shader pass.</returns>
    public ShaderPass CreateD3D12(
        SilkD3D12Device device,
        SilkD3D12RootSignature rootSignature,
        D3D12PipelineStateCache cache,
        InputLayoutDescription? fallbackInputLayout = null,
        PrimitiveTopology fallbackTopology = PrimitiveTopology.Undefined,
        IReadOnlyList<Silk.NET.DXGI.Format>? renderTargetFormats = null,
        Silk.NET.DXGI.Format depthStencilFormat = Silk.NET.DXGI.Format.FormatUnknown
    ) {
        device.GuardNotNull();
        rootSignature.GuardNotNull();
        cache.GuardNotNull();
        var modules = GetD3D12ShaderModules();
        if (modules.TryGetValue(ShaderStage.Compute, out var computeShader)) {
            var computeKey = D3D12PipelineStateKey.Compute(computeShader);
            var computePipeline = cache.GetOrCreate(computeKey,
                () => device.CreateComputePipelineState(rootSignature, computeShader));
            return ShaderPass.CreateD3D12(Name ?? throw new InvalidOperationException("Shader pass name is required."),
                rootSignature,
                computePipeline,
                true);
        }

        if (!modules.TryGetValue(ShaderStage.Vertex, out var vertexShader))
            throw new InvalidOperationException($"Graphics shader pass '{Name}' requires a vertex shader.");
        modules.TryGetValue(ShaderStage.Pixel, out var pixelShader);
        modules.TryGetValue(ShaderStage.Geometry, out var geometryShader);
        modules.TryGetValue(ShaderStage.Hull, out var hullShader);
        modules.TryGetValue(ShaderStage.Domain, out var domainShader);
        var topology = Topology == PrimitiveTopology.Undefined ? fallbackTopology : Topology;
        if (topology == PrimitiveTopology.Undefined)
            throw new InvalidOperationException($"Graphics shader pass '{Name}' requires a primitive topology.");
        var topologyType = GetD3D12TopologyType(topology);
        var inputElements = (InputLayoutDescription ?? fallbackInputLayout)?.D3D12InputElements ?? [];
        var streamOutput = (ShaderList ?? []).SingleOrDefault(shader => shader.IsGsStreamOut);
        var streamOutputOnly = streamOutput is {GssoRasterized: < 0};
        var formats = streamOutputOnly
            ? []
            : renderTargetFormats ?? [Silk.NET.DXGI.Format.FormatR8G8B8A8Unorm];
        var pipelineDepthFormat = streamOutputOnly ? Silk.NET.DXGI.Format.FormatUnknown : depthStencilFormat;
        var key = D3D12PipelineStateKey.Graphics(vertexShader,
            pixelShader,
            geometryShader,
            hullShader,
            domainShader,
            inputLayout: inputElements,
            streamOutputElements: streamOutput?.GssoElement,
            streamOutputStrides: streamOutput?.GssoStrides,
            rasterizedStream: streamOutput?.GssoRasterized ?? 0,
            topology: topologyType,
            blendState: BlendStateDescription,
            rasterizerState: RasterStateDescription,
            depthStencilState: DepthStencilStateDescription,
            sampleMask: unchecked((uint) SampleMask),
            renderTargetFormats: formats,
            depthStencilFormat: pipelineDepthFormat);
        var pipeline = cache.GetOrCreate(key,
            () => device.CreateGraphicsPipelineState(rootSignature,
                vertexShader,
                pixelShader,
                geometryShader,
                hullShader,
                domainShader,
                topologyType,
                formats,
                pipelineDepthFormat,
                inputElements,
                streamOutput?.GssoElement,
                streamOutput?.GssoStrides,
                streamOutput?.GssoRasterized ?? 0,
                BlendStateDescription,
                RasterStateDescription,
                DepthStencilStateDescription,
                unchecked((uint) SampleMask)));
        return ShaderPass.CreateD3D12(Name ?? throw new InvalidOperationException("Shader pass name is required."),
            rootSignature,
            pipeline,
            topology: topology,
            stencilReference: StencilRef);
    }

    /// <summary>
    ///     Converts a concrete draw topology to its Direct3D 12 PSO topology class.
    /// </summary>
    private static Silk.NET.Direct3D12.PrimitiveTopologyType GetD3D12TopologyType(PrimitiveTopology topology) =>
        topology switch {
            PrimitiveTopology.PointList => Silk.NET.Direct3D12.PrimitiveTopologyType.Point,
            PrimitiveTopology.LineList or PrimitiveTopology.LineStrip or PrimitiveTopology.LineListWithAdjacency
                or PrimitiveTopology.LineStripWithAdjacency => Silk.NET.Direct3D12.PrimitiveTopologyType.Line,
            >= PrimitiveTopology.PatchListWith1ControlPoint => Silk.NET.Direct3D12.PrimitiveTopologyType.Patch,
            _ => Silk.NET.Direct3D12.PrimitiveTopologyType.Triangle
        };

    /// <summary>
    ///     Pass Name
    /// </summary>
    [DataMember(Name = @"Name")]
    public string? Name { get; set; }

    /// <summary>
    ///     Shaders for this technique
    /// </summary>
    [DataMember(Name = @"ShaderList")]
    public IList<ShaderDescription>? ShaderList { get; set; }

    /// <summary>
    /// </summary>
    public BlendStateDescription? BlendStateDescription { get; set; }

    /// <summary>
    ///     Only used for data serialization
    /// </summary>
    [DataMember(Name = @"BlendStateDescSerialization")]
    public BlendStateDataContract? BlendStateDescSerialization {
        get {
            if (BlendStateDescription == null) return null;

            return new BlendStateDataContract((BlendStateDescription)BlendStateDescription);
        }
        set {
            if (value == null)
                BlendStateDescription = null;
            else
                BlendStateDescription = ((BlendStateDataContract)value).ToBlendStateDescription();
        }
    }

    /// <summary>
    ///     Gets or sets the color of the blend.
    /// </summary>
    /// <value>
    ///     The color of the blend.
    /// </value>
    [DataMember(Name = @"BlendFactor")]
    public Color4 BlendFactor { get; set; } = Color.White;

    /// <summary>
    ///     Gets or sets the blend sample mask.
    /// </summary>
    /// <value>
    ///     The sample mask.
    /// </value>
    [DataMember(Name = @"SampleMask")]
    public int SampleMask { get; set; } = -1;

    /// <summary>
    ///     Gets or sets the stencil reference.
    /// </summary>
    /// <value>
    ///     The stencil reference.
    /// </value>
    [DataMember(Name = @"StencilRef")]
    public int StencilRef { get; set; }

    /// <summary>
    ///     Gets or sets the topology. This is optional. Used if topology is different from vertex buffer topology
    /// </summary>
    /// <value>
    ///     The topology.
    /// </value>
    [DataMember(Name = @"Topology")]
    public PrimitiveTopology Topology { get; set; } = PrimitiveTopology.Undefined;

    /// <summary>
    /// </summary>
    public DepthStencilStateDescription? DepthStencilStateDescription { get; set; }

    /// <summary>
    ///     Only used for data serialization
    /// </summary>
    [DataMember(Name = @"DepthStencilStateDescSerialization")]
    public DepthStencilStateDataContract? DepthStencilStateDescSerialization {
        get {
            if (DepthStencilStateDescription == null) return null;

            return new DepthStencilStateDataContract(
                (DepthStencilStateDescription)DepthStencilStateDescription);
        }
        set {
            if (value == null)
                DepthStencilStateDescription = null;
            else
                DepthStencilStateDescription =
                    ((DepthStencilStateDataContract)value).ToDepthStencilStateDescription();
        }
    }

    /// <summary>
    /// </summary>
    public RasterizerStateDescription? RasterStateDescription { get; set; }

    /// <summary>
    ///     Only used for data serialization
    /// </summary>
    /// <value>
    ///     The rasterizer state data contract.
    /// </value>
    [DataMember(Name = @"RasterizerStateDescSerialization")]
    public RasterizerStateDataContract? RasterizerStateDescSerialization {
        get {
            if (RasterStateDescription == null) return null;

            return new RasterizerStateDataContract((RasterizerStateDescription)RasterStateDescription);
        }
        set {
            if (value == null)
                RasterStateDescription = null;
            else
                RasterStateDescription = ((RasterizerStateDataContract)value).ToRasterizerStateDescription();
        }
    }

    /// <summary>
    ///     Input Layout
    /// </summary>
    [DataMember(Name = @"InputLayoutDescription")]
    public InputLayoutDescription? InputLayoutDescription { get; set; }
}

#region Serializable descriptions

[DataContract(Name = @"DepthStencilOperationDataContract")]
public struct DepthStencilOperationDataContract {
    [DataMember(Name = @"FailOperation")]
    public int FailOperation { get; set; }

    [DataMember(Name = @"DepthFailOperation")]
    public int DepthFailOperation { get; set; }

    [DataMember(Name = @"PassOperation")]
    public int PassOperation { get; set; }

    [DataMember(Name = @"Comparison")]
    public int Comparison { get; set; }

    public DepthStencilOperationDescription ToDepthStencilOperationDescription() => new() {
        FailOperation = (StencilOperation)FailOperation,
        DepthFailOperation = (StencilOperation)DepthFailOperation,
        PassOperation = (StencilOperation)PassOperation,
        Comparison = (Comparison)Comparison
    };

    public DepthStencilOperationDataContract(DepthStencilOperationDescription desc) {
        FailOperation = (int)desc.FailOperation;
        DepthFailOperation = (int)desc.DepthFailOperation;
        PassOperation = (int)desc.PassOperation;
        Comparison = (int)desc.Comparison;
    }
}

[DataContract(Name = @"DepthStencilStateDataContract")]
public struct DepthStencilStateDataContract {
    [DataMember(Name = @"IsDepthEnabled")]
    public bool IsDepthEnabled { get; set; }

    [DataMember(Name = @"DepthWriteMask")]
    public int DepthWriteMask { get; set; }

    [DataMember(Name = @"DepthComparison")]
    public int DepthComparison { get; set; }

    [DataMember(Name = @"IsStencilEnabled")]
    public bool IsStencilEnabled { get; set; }

    [DataMember(Name = @"StencilReadMask")]
    public byte StencilReadMask { get; set; }

    [DataMember(Name = @"StencilWriteMask")]
    public byte StencilWriteMask { get; set; }

    [DataMember(Name = @"FrontFace")]
    public DepthStencilOperationDataContract FrontFace { get; set; }

    [DataMember(Name = @"BackFace")]
    public DepthStencilOperationDataContract BackFace { get; set; }

    public DepthStencilStateDataContract(DepthStencilStateDescription desc) {
        IsDepthEnabled = desc.IsDepthEnabled;
        IsStencilEnabled = desc.IsStencilEnabled;
        DepthWriteMask = (int)desc.DepthWriteMask;
        DepthComparison = (int)desc.DepthComparison;
        StencilReadMask = desc.StencilReadMask;
        StencilWriteMask = desc.StencilWriteMask;
        FrontFace = new DepthStencilOperationDataContract(desc.FrontFace);
        BackFace = new DepthStencilOperationDataContract(desc.BackFace);
    }

    public DepthStencilStateDescription ToDepthStencilStateDescription() => new() {
        IsDepthEnabled = IsDepthEnabled,
        DepthWriteMask = (DepthWriteMask)DepthWriteMask,
        DepthComparison = (Comparison)DepthComparison,
        IsStencilEnabled = IsStencilEnabled,
        StencilReadMask = StencilReadMask,
        StencilWriteMask = StencilWriteMask,
        FrontFace = FrontFace.ToDepthStencilOperationDescription(),
        BackFace = BackFace.ToDepthStencilOperationDescription()
    };
}

[DataContract(Name = @"RasterizerStateDataContract")]
public struct RasterizerStateDataContract {
    [DataMember(Name = @"FillMode")]
    public int FillMode { get; set; }

    [DataMember(Name = @"CullMode")]
    public int CullMode { get; set; }

    [DataMember(Name = @"IsFrontCounterClockwise")]
    public bool IsFrontCounterClockwise { get; set; }

    [DataMember(Name = @"DepthBias")]
    public int DepthBias { get; set; }

    [DataMember(Name = @"DepthBiasClamp")]
    public float DepthBiasClamp { get; set; }

    [DataMember(Name = @"SlopeScaledDepthBias")]
    public float SlopeScaledDepthBias { get; set; }

    [DataMember(Name = @"IsDepthClipEnabled")]
    public bool IsDepthClipEnabled { get; set; }

    [DataMember(Name = @"IsScissorEnabled")]
    public bool IsScissorEnabled { get; set; }

    [DataMember(Name = @"IsMultisampleEnabled")]
    public bool IsMultisampleEnabled { get; set; }

    [DataMember(Name = @"IsAntialiasedLineEnabled")]
    public bool IsAntialiasedLineEnabled { get; set; }

    public RasterizerStateDescription ToRasterizerStateDescription() => new() {
        FillMode = (FillMode)FillMode,
        CullMode = (CullMode)CullMode,
        IsFrontCounterClockwise = IsFrontCounterClockwise,
        DepthBias = DepthBias,
        DepthBiasClamp = DepthBiasClamp,
        SlopeScaledDepthBias = SlopeScaledDepthBias,
        IsDepthClipEnabled = IsDepthClipEnabled,
        IsScissorEnabled = IsScissorEnabled,
        IsMultisampleEnabled = IsMultisampleEnabled,
        IsAntialiasedLineEnabled = IsAntialiasedLineEnabled
    };

    public RasterizerStateDataContract(RasterizerStateDescription desc) {
        FillMode = (int)desc.FillMode;
        CullMode = (int)desc.CullMode;
        DepthBias = desc.DepthBias;
        IsFrontCounterClockwise = desc.IsFrontCounterClockwise;
        DepthBiasClamp = desc.DepthBiasClamp;
        SlopeScaledDepthBias = desc.SlopeScaledDepthBias;
        IsDepthClipEnabled = desc.IsDepthClipEnabled;
        IsScissorEnabled = desc.IsScissorEnabled;
        IsMultisampleEnabled = desc.IsMultisampleEnabled;
        IsAntialiasedLineEnabled = desc.IsAntialiasedLineEnabled;
    }
}

[DataContract(Name = @"BlendStateDataContract")]
public struct BlendStateDataContract {
    [DataMember(Name = @"AlphaToCoverageEnable")]
    public bool AlphaToCoverageEnable { get; set; }

    [DataMember(Name = @"IndependentBlendEnable")]
    public bool IndependentBlendEnable { get; set; }

    [DataMember(Name = @"RenderTarget")]
    public RenderTargetBlendDataContract[] RenderTarget { get; set; }

    public BlendStateDataContract(BlendStateDescription desc) {
        AlphaToCoverageEnable = desc.AlphaToCoverageEnable;
        IndependentBlendEnable = desc.IndependentBlendEnable;
        RenderTarget = new RenderTargetBlendDataContract[desc.RenderTarget.Length];
        for (var i = 0; i < desc.RenderTarget.Length; ++i)
            RenderTarget[i] = new RenderTargetBlendDataContract(desc.RenderTarget[i]);
    }

    public BlendStateDescription ToBlendStateDescription() {
        var desc = new BlendStateDescription {
            AlphaToCoverageEnable = AlphaToCoverageEnable,
            IndependentBlendEnable = IndependentBlendEnable
        };
        for (var i = 0; i < desc.RenderTarget.Length; ++i)
            desc.RenderTarget[i] = RenderTarget[i].ToRenderTargetBlendDescription();
        return desc;
    }
}

[DataContract(Name = @"RenderTargetBlendDataContract")]
public struct RenderTargetBlendDataContract {
    [DataMember(Name = @"IsBlendEnabled")]
    public bool IsBlendEnabled { get; set; }

    [DataMember(Name = @"SourceBlend")]
    public int SourceBlend { get; set; }

    [DataMember(Name = @"DestinationBlend")]
    public int DestinationBlend { get; set; }

    [DataMember(Name = @"BlendOperation")]
    public int BlendOperation { get; set; }

    [DataMember(Name = @"SourceAlphaBlend")]
    public int SourceAlphaBlend { get; set; }

    [DataMember(Name = @"DestinationAlphaBlend")]
    public int DestinationAlphaBlend { get; set; }

    [DataMember(Name = @"AlphaBlendOperation")]
    public int AlphaBlendOperation { get; set; }

    [DataMember(Name = @"RenderTargetWriteMask")]
    public int RenderTargetWriteMask { get; set; }

    public RenderTargetBlendDataContract(RenderTargetBlendDescription desc) {
        IsBlendEnabled = desc.IsBlendEnabled;
        SourceBlend = (int)desc.SourceBlend;
        DestinationBlend = (int)desc.DestinationBlend;
        BlendOperation = (int)desc.BlendOperation;
        SourceAlphaBlend = (int)desc.SourceAlphaBlend;
        DestinationAlphaBlend = (int)desc.DestinationAlphaBlend;
        AlphaBlendOperation = (int)desc.AlphaBlendOperation;
        RenderTargetWriteMask = (int)desc.RenderTargetWriteMask;
    }

    public RenderTargetBlendDescription ToRenderTargetBlendDescription() => new() {
        IsBlendEnabled = IsBlendEnabled,
        SourceBlend = (BlendOption)SourceBlend,
        DestinationBlend = (BlendOption)DestinationBlend,
        BlendOperation = (BlendOperation)BlendOperation,
        SourceAlphaBlend = (BlendOption)SourceAlphaBlend,
        DestinationAlphaBlend = (BlendOption)DestinationAlphaBlend,
        AlphaBlendOperation = (BlendOperation)AlphaBlendOperation,
        RenderTargetWriteMask = (ColorWriteMaskFlags)RenderTargetWriteMask
    };
}

#endregion

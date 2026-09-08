/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.CompilerServices;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Utilities;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.Shaders;

/// <summary>
///     Shader Pass
/// </summary>
public sealed class ShaderPass : DisposeObject {
    public static readonly ShaderPass NullPass = new();

    /// <summary>
    ///     The borrowed Direct3D 12 root signature used by this pass.
    /// </summary>
    private readonly SilkD3D12RootSignature? d3D12RootSignature;

    /// <summary>
    ///     The borrowed cache-owned Direct3D 12 pipeline state used by this pass.
    /// </summary>
    private readonly SilkD3D12PipelineState? d3D12PipelineState;

    /// <summary>
    ///     Whether the Direct3D 12 pipeline is a compute pipeline.
    /// </summary>
    private readonly bool isD3D12Compute;

    /// <summary>
    /// </summary>
    /// <param name="passDescription"></param>
    /// <param name="manager"></param>
    public ShaderPass(ShaderPassDescription passDescription, IEffectsManager manager) {
        Name = passDescription.Name.AsNotNull("Shader pass name must be initialized.");
        BlendFactor = passDescription.BlendFactor;
        StencilRef = passDescription.StencilRef;
        SampleMask = passDescription.SampleMask;
        Topology = passDescription.Topology;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ShaderPass" /> class.
    /// </summary>
    private ShaderPass() {
        Name = string.Empty;
        IsNull = true;
    }

    /// <summary>
    ///     Initializes a Direct3D 12 pass over cache-owned native objects.
    /// </summary>
    /// <param name="name">The pass name.</param>
    /// <param name="rootSignature">The borrowed shared root signature.</param>
    /// <param name="pipelineState">The borrowed cache-owned pipeline state.</param>
    /// <param name="isCompute">Whether the pipeline is compute-only.</param>
    /// <param name="topology">The graphics primitive topology.</param>
    /// <param name="stencilReference">The eight-bit output-merger stencil reference.</param>
    private ShaderPass(
        string name,
        SilkD3D12RootSignature rootSignature,
        SilkD3D12PipelineState pipelineState,
        bool isCompute,
        PrimitiveTopology topology,
        int stencilReference
    ) {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A pass name is required.", nameof(name));
        rootSignature.AsGuardNotNull();
        pipelineState.AsGuardNotNull();
        if (!isCompute && topology == PrimitiveTopology.Undefined)
            throw new ArgumentOutOfRangeException(nameof(topology));
        if ((uint) stencilReference > byte.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(stencilReference));

        d3D12RootSignature = rootSignature;
        d3D12PipelineState = pipelineState;
        Name = name;
        isD3D12Compute = isCompute;
        Topology = topology;
        StencilRef = stencilReference;
    }

    /// <summary>
    ///     Creates a Direct3D 12 pass that borrows its shared root signature and cache-owned pipeline state.
    /// </summary>
    /// <param name="name">The pass name.</param>
    /// <param name="rootSignature">The shared root signature.</param>
    /// <param name="pipelineState">The cache-owned pipeline state.</param>
    /// <param name="isCompute">Whether the pipeline is compute-only.</param>
    /// <param name="topology">The graphics primitive topology.</param>
    /// <param name="stencilReference">The eight-bit output-merger stencil reference.</param>
    /// <returns>The Direct3D 12 shader pass.</returns>
    public static ShaderPass CreateD3D12(
        string name,
        SilkD3D12RootSignature rootSignature,
        SilkD3D12PipelineState pipelineState,
        bool isCompute = false,
        PrimitiveTopology topology = PrimitiveTopology.TriangleList,
        int stencilReference = 0
    ) => new(name, rootSignature, pipelineState, isCompute, topology, stencilReference);

    /// <summary>
    ///     <see cref="ShaderPass.Name" />
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// </summary>
    public bool IsNull { get; }

    /// <summary>
    ///     Gets whether this pass uses Direct3D 12 command recording.
    /// </summary>
    public bool IsD3D12 => d3D12PipelineState is not null;

    /// <summary>
    ///     Gets the borrowed cache-owned Direct3D 12 pipeline state, if this is a Direct3D 12 pass.
    /// </summary>
    public SilkD3D12PipelineState? D3D12PipelineState => d3D12PipelineState;

    /// <summary>
    ///     Gets or sets the blend factor.
    /// </summary>
    /// <value>
    ///     The blend factor.
    /// </value>
    public Color4 BlendFactor { get; } = Color.White;

    /// <summary>
    ///     Gets or sets the sample mask.
    /// </summary>
    /// <value>
    ///     The sample mask.
    /// </value>
    public int SampleMask { get; } = -1;

    /// <summary>
    ///     Gets or sets the stencil reference.
    /// </summary>
    /// <value>
    ///     The stencil reference.
    /// </value>
    public int StencilRef { get; }

    /// <summary>
    ///     Gets or sets the topology.
    /// </summary>
    /// <value>
    ///     The topology.
    /// </value>
    public PrimitiveTopology Topology { get; set; } = PrimitiveTopology.Undefined;

    /// <summary>
    ///     Binds this Direct3D 12 pass to a command list.
    /// </summary>
    /// <param name="context">The Direct3D 12 command context.</param>
    public void BindShader(SilkD3D12CommandContext context) {
        context.AsGuardNotNull();
        if (d3D12RootSignature is null || d3D12PipelineState is null)
            throw new InvalidOperationException("The shader pass is not a Direct3D 12 pass.");

        if (isD3D12Compute) {
            context.SetComputePipeline(d3D12RootSignature, d3D12PipelineState);
            return;
        }

        context.SetGraphicsPipeline(d3D12RootSignature, d3D12PipelineState);
        context.SetStencilReference(StencilRef);
        context.SetPrimitiveTopology(Topology);
    }

}

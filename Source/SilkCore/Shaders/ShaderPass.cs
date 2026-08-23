/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.CompilerServices;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render.DeviceContextProxy;
using HelixToolkit.SharpDX.Core.Utilities;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.Shaders;

/// <summary>
///     Shader Pass
/// </summary>
public sealed class ShaderPass : DisposeObject {
    public static readonly ShaderPass NullPass = new();

    private readonly IEffectsManager? effectsManager;

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

    private BlendStateProxy? blendState = BlendStateProxy.Empty;
    private ComputeShader? computeShader = ComputeShader.NullComputeShader;

    private DepthStencilStateProxy? depthStencilState = DepthStencilStateProxy.Empty;
    private DomainShader? domainShader = DomainShader.NullDomainShader;
    private GeometryShader? geometryShader = GeometryShader.NullGeometryShader;
    private HullShader? hullShader = HullShader.NullHullShader;

    private InputLayoutProxy? layout;
    private PixelShader? pixelShader = PixelShader.NullPixelShader;

    private RasterizerStateProxy? rasterState = RasterizerStateProxy.Empty;
    private VertexShader? vertexShader = VertexShader.NullVertexShader;

    /// <summary>
    /// </summary>
    /// <param name="passDescription"></param>
    /// <param name="manager"></param>
    public ShaderPass(ShaderPassDescription passDescription, IEffectsManager manager) {
        Name = passDescription.Name.AssertNotNull("Shader pass name must be initialized.");
        effectsManager = manager;
        if (passDescription.ShaderList != null)
            foreach (var shader in passDescription.ShaderList) {
                var s = manager.ShaderManager.RegisterShader(shader);
                switch (shader.ShaderType) {
                    case ShaderStage.Vertex:
                        vertexShader = s as VertexShader ?? VertexShader.NullVertexShader;
                        break;
                    case ShaderStage.Domain:
                        domainShader = s as DomainShader ?? DomainShader.NullDomainShader;
                        break;
                    case ShaderStage.Hull:
                        hullShader = s as HullShader ?? HullShader.NullHullShader;
                        break;
                    case ShaderStage.Geometry:
                        geometryShader = s as GeometryShader ?? GeometryShader.NullGeometryShader;
                        break;
                    case ShaderStage.Pixel:
                        pixelShader = s as PixelShader ?? PixelShader.NullPixelShader;
                        break;
                    case ShaderStage.Compute:
                        computeShader = s as ComputeShader ?? ComputeShader.NullComputeShader;
                        break;
                }
            }

        blendState = passDescription.BlendStateDescription != null
            ? manager.StateManager.Register(
                (BlendStateDescription) passDescription.BlendStateDescription)
            : BlendStateProxy.Empty;

        depthStencilState = passDescription.DepthStencilStateDescription != null
            ? manager.StateManager.Register(
                (DepthStencilStateDescription) passDescription.DepthStencilStateDescription)
            : DepthStencilStateProxy.Empty;

        rasterState = passDescription.RasterStateDescription != null
            ? manager.StateManager.Register(
                (RasterizerStateDescription) passDescription.RasterStateDescription)
            : RasterizerStateProxy.Empty;

        BlendFactor = passDescription.BlendFactor;

        StencilRef = passDescription.StencilRef;

        SampleMask = passDescription.SampleMask;

        Topology = passDescription.Topology;
        if (passDescription.InputLayoutDescription != null)
            layout = manager.ShaderManager.RegisterInputLayout(passDescription.InputLayoutDescription);
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
    private ShaderPass(
        string name,
        SilkD3D12RootSignature rootSignature,
        SilkD3D12PipelineState pipelineState,
        bool isCompute,
        PrimitiveTopology topology
    ) {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A pass name is required.", nameof(name));
        rootSignature.AssertArgumentNotNull();
        pipelineState.AssertArgumentNotNull();
        if (!isCompute && topology == PrimitiveTopology.Undefined)
            throw new ArgumentOutOfRangeException(nameof(topology));

        d3D12RootSignature = rootSignature;
        d3D12PipelineState = pipelineState;
        Name = name;
        isD3D12Compute = isCompute;
        Topology = topology;
    }

    /// <summary>
    ///     Creates a Direct3D 12 pass that borrows its shared root signature and cache-owned pipeline state.
    /// </summary>
    /// <param name="name">The pass name.</param>
    /// <param name="rootSignature">The shared root signature.</param>
    /// <param name="pipelineState">The cache-owned pipeline state.</param>
    /// <param name="isCompute">Whether the pipeline is compute-only.</param>
    /// <param name="topology">The graphics primitive topology.</param>
    /// <returns>The Direct3D 12 shader pass.</returns>
    public static ShaderPass CreateD3D12(
        string name,
        SilkD3D12RootSignature rootSignature,
        SilkD3D12PipelineState pipelineState,
        bool isCompute = false,
        PrimitiveTopology topology = PrimitiveTopology.TriangleList
    ) => new(name, rootSignature, pipelineState, isCompute, topology);

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

    public VertexShader VertexShader => vertexShader ?? VertexShader.NullVertexShader;
    public DomainShader DomainShader => domainShader ?? DomainShader.NullDomainShader;
    public HullShader HullShader => hullShader ?? HullShader.NullHullShader;
    public PixelShader PixelShader => pixelShader ?? PixelShader.NullPixelShader;
    public GeometryShader GeometryShader => geometryShader ?? GeometryShader.NullGeometryShader;
    public ComputeShader ComputeShader => computeShader ?? ComputeShader.NullComputeShader;

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
    ///     <see cref="ShaderPass.BlendState" />
    /// </summary>
    public BlendStateProxy BlendState => blendState ?? BlendStateProxy.Empty;

    /// <summary>
    ///     <see cref="ShaderPass.DepthStencilState" />
    /// </summary>
    public DepthStencilStateProxy DepthStencilState => depthStencilState ?? DepthStencilStateProxy.Empty;

    /// <summary>
    ///     <see cref="ShaderPass.RasterState" />
    /// </summary>
    public RasterizerStateProxy RasterState => rasterState ?? RasterizerStateProxy.Empty;

    /// <summary>
    ///     Gets or sets the input layout. This is customized layout used for this ShaderPass only.
    ///     If this is not set, default is using <see cref="Technique.Layout" /> from
    ///     <see cref="TechniqueDescription.InputLayoutDescription" />
    /// </summary>
    /// <value>
    ///     The input layout.
    /// </value>
    public InputLayoutProxy? Layout => layout;

    /// <summary>
    ///     Gets or sets the topology.
    /// </summary>
    /// <value>
    ///     The topology.
    /// </value>
    public PrimitiveTopology Topology { get; set; } = PrimitiveTopology.Undefined;

    /// <summary>
    ///     Bind shaders and its constant buffer for this technique
    /// </summary>
    /// <param name="context"></param>
    /// <param name="bindConstantBuffer"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindShader(DeviceContextProxy context, bool bindConstantBuffer = true) {
        context.SetShaderPass(this, bindConstantBuffer);
        if (Layout != null) context.InputLayout = Layout;
    }

    /// <summary>
    ///     Binds this Direct3D 12 pass to a command list.
    /// </summary>
    /// <param name="context">The Direct3D 12 command context.</param>
    public void BindShader(SilkD3D12CommandContext context) {
        context.AssertArgumentNotNull();
        if (d3D12RootSignature is null || d3D12PipelineState is null)
            throw new InvalidOperationException("The shader pass is not a Direct3D 12 pass.");

        if (isD3D12Compute) {
            context.SetComputePipeline(d3D12RootSignature, d3D12PipelineState);
            return;
        }

        context.SetGraphicsPipeline(d3D12RootSignature, d3D12PipelineState);
        context.SetPrimitiveTopology(Topology);
    }

    #region Set Shaders

    /// <summary>
    ///     Sets the shader.
    /// </summary>
    /// <param name="shader">The shader.</param>
    public void SetShader(ShaderBase shader) {
        switch (shader.ShaderType) {
            case ShaderStage.Vertex:
                RemoveAndDispose(ref vertexShader);
                vertexShader = shader as VertexShader ?? VertexShader.NullVertexShader;
                break;
            case ShaderStage.Pixel:
                RemoveAndDispose(ref pixelShader);
                pixelShader = shader as PixelShader ?? PixelShader.NullPixelShader;
                break;
            case ShaderStage.Compute:
                RemoveAndDispose(ref computeShader);
                computeShader = shader as ComputeShader ?? ComputeShader.NullComputeShader;
                break;
            case ShaderStage.Hull:
                RemoveAndDispose(ref hullShader);
                hullShader = shader as HullShader ?? HullShader.NullHullShader;
                break;
            case ShaderStage.Domain:
                RemoveAndDispose(ref domainShader);
                domainShader = shader as DomainShader ?? DomainShader.NullDomainShader;
                break;
            case ShaderStage.Geometry:
                RemoveAndDispose(ref geometryShader);
                geometryShader = shader as GeometryShader ?? GeometryShader.NullGeometryShader;
                break;
        }
    }

    #endregion

    /// <summary>
    ///     Binds the states.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="type">The type.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindStates(DeviceContextProxy context, StateType type) {
        if (type == StateType.None || IsNull) return;
        if (EnumHelper.HasFlag(type, StateType.BlendState))
            context.SetBlendState(BlendState, BlendFactor, SampleMask);
        if (EnumHelper.HasFlag(type, StateType.DepthStencilState))
            context.SetDepthStencilState(DepthStencilState, StencilRef);
        if (EnumHelper.HasFlag(type, StateType.RasterState)) context.SetRasterState(RasterState);
    }

    /// <summary>
    ///     Sets the state.
    /// </summary>
    /// <param name="blendStateDesc">The blend state desc.</param>
    public void SetState(BlendStateDescription? blendStateDesc) {
        if (IsNull) return;
        if (BlendState != BlendStateProxy.Empty) RemoveAndDispose(ref blendState);
        blendState = blendStateDesc != null
            ? effectsManager.AssertNotNull()
                .StateManager.Register(blendStateDesc.Value)
            : BlendStateProxy.Empty;
    }

    /// <summary>
    ///     Sets the state.
    /// </summary>
    /// <param name="depthStencilStateDesc">The depth stencil state desc.</param>
    public void SetState(DepthStencilStateDescription? depthStencilStateDesc) {
        if (IsNull) return;
        if (DepthStencilState != DepthStencilStateProxy.Empty) RemoveAndDispose(ref depthStencilState);
        depthStencilState = depthStencilStateDesc != null
            ? effectsManager.AssertNotNull()
                .StateManager.Register(depthStencilStateDesc.Value)
            : DepthStencilStateProxy.Empty;
    }

    /// <summary>
    ///     Sets the state.
    /// </summary>
    /// <param name="rasterizerStateDesc">The rasterizer state desc.</param>
    public void SetState(RasterizerStateDescription? rasterizerStateDesc) {
        if (IsNull) return;
        if (RasterState != RasterizerStateProxy.Empty) RemoveAndDispose(ref rasterState);
        rasterState = rasterizerStateDesc != null
            ? effectsManager.AssertNotNull()
                .StateManager.Register(rasterizerStateDesc.Value)
            : RasterizerStateProxy.Empty;
    }

    protected override void OnDispose(bool disposeManagedResources) {
        if (BlendState != BlendStateProxy.Empty) RemoveAndDispose(ref blendState);
        if (DepthStencilState != DepthStencilStateProxy.Empty) RemoveAndDispose(ref depthStencilState);
        if (RasterState != RasterizerStateProxy.Empty) RemoveAndDispose(ref rasterState);
        RemoveAndDispose(ref layout);
        RemoveAndDispose(ref vertexShader);
        RemoveAndDispose(ref domainShader);
        RemoveAndDispose(ref hullShader);
        RemoveAndDispose(ref geometryShader);
        RemoveAndDispose(ref pixelShader);
        RemoveAndDispose(ref computeShader);
        base.OnDispose(disposeManagedResources);
    }

    #region Get Shaders

    /// <summary>
    ///     <see cref="ShaderPass.GetShader(ShaderStage)" />
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ShaderBase? GetShader(ShaderStage type) {
        switch (type) {
            case ShaderStage.Vertex:
                return VertexShader;
            case ShaderStage.Pixel:
                return PixelShader;
            case ShaderStage.Compute:
                return ComputeShader;
            case ShaderStage.Hull:
                return HullShader;
            case ShaderStage.Domain:
                return DomainShader;
            case ShaderStage.Geometry:
                return GeometryShader;
            default:
                return null;
        }
    }

    /// <summary>
    ///     Gets the shader.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VertexShader GetShader(VertexShaderType type) => VertexShader;

    /// <summary>
    ///     Gets the shader.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HullShader GetShader(HullShaderType type) => HullShader;

    /// <summary>
    ///     Gets the shader.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DomainShader GetShader(DomainShaderType type) => DomainShader;

    /// <summary>
    ///     Gets the shader.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GeometryShader GetShader(GeometryShaderType type) => GeometryShader;

    /// <summary>
    ///     Gets the shader.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PixelShader GetShader(PixelShaderType type) => PixelShader;

    /// <summary>
    ///     Gets the shader.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ComputeShader GetShader(ComputeShaderType type) => ComputeShader;

    #endregion
}

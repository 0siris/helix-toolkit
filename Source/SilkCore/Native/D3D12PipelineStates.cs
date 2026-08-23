/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using System.Security.Cryptography;
using HelixToolkit.SharpDX.Core.DefaultShaders;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using SilkD3D12PipelineStatePtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D12.ID3D12PipelineState>;

namespace HelixToolkit.SharpDX.Core.Native;

/// <summary>
///     Describes one vertex input element used to build a native DX12 input layout.
/// </summary>
/// <param name="SemanticName">The HLSL semantic name.</param>
/// <param name="SemanticIndex">The HLSL semantic index.</param>
/// <param name="Format">The element format.</param>
/// <param name="InputSlot">The vertex-buffer slot.</param>
/// <param name="AlignedByteOffset">The byte offset, or <see cref="uint.MaxValue" /> for append-aligned.</param>
/// <param name="Classification">Whether data advances per vertex or per instance.</param>
/// <param name="InstanceDataStepRate">The instance step rate.</param>
public readonly record struct D3D12InputElementDescription(
    string SemanticName,
    uint SemanticIndex,
    Format Format,
    uint InputSlot,
    uint AlignedByteOffset,
    Silk.NET.Direct3D12.InputClassification Classification =
        Silk.NET.Direct3D12.InputClassification.PerVertexData,
    uint InstanceDataStepRate = 0
);

/// <summary>
///     Holds immutable SM6 DXIL and its pipeline-cache identity.
/// </summary>
public sealed class D3D12ShaderModule {
    private static readonly HashSet<string> SupportedStages = new(StringComparer.OrdinalIgnoreCase) {
        "VS", "PS", "GS", "HS", "DS", "CS"
    };

    /// <summary>
    ///     Creates a shader module from precompiled DXIL.
    /// </summary>
    /// <param name="stage">The two-letter shader stage.</param>
    /// <param name="name">The stable shader name.</param>
    /// <param name="entryPoint">The compiled entry point.</param>
    /// <param name="byteCode">The precompiled SM6 DXIL.</param>
    public D3D12ShaderModule(string stage, string name, string entryPoint, ReadOnlySpan<byte> byteCode) {
        if (!SupportedStages.Contains(stage))
            throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unsupported shader stage.");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A shader name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(entryPoint))
            throw new ArgumentException("A shader entry point is required.", nameof(entryPoint));
        if (byteCode.IsEmpty) throw new ArgumentException("Shader byte code cannot be empty.", nameof(byteCode));

        Stage = stage.ToUpperInvariant();
        Name = name;
        EntryPoint = entryPoint;
        ByteCode = byteCode.ToArray();
        ContentHash = Convert.ToHexString(SHA256.HashData(ByteCode.Span));
    }

    /// <summary>Gets the two-letter shader stage.</summary>
    public string Stage { get; }

    /// <summary>Gets the stable shader name.</summary>
    public string Name { get; }

    /// <summary>Gets the compiled entry point.</summary>
    public string EntryPoint { get; }

    /// <summary>Gets the immutable shader byte code.</summary>
    public ReadOnlyMemory<byte> ByteCode { get; }

    /// <summary>Gets the SHA-256 content identity used by PSO keys.</summary>
    public string ContentHash { get; }

    /// <summary>
    ///     Loads an embedded repository shader.
    /// </summary>
    /// <param name="stage">The two-letter shader stage.</param>
    /// <param name="name">The shader file name without extension.</param>
    /// <param name="entryPoint">The compiled entry point, or <see langword="null" /> to resolve it from the manifest.</param>
    /// <returns>The immutable shader module.</returns>
    public static D3D12ShaderModule Load(string stage, string name, string? entryPoint = null) {
        var resolvedEntryPoint = entryPoint ?? D3D12ShaderManifest.ResolveEntryPoint(stage, name);
        return new D3D12ShaderModule(stage,
            name,
            resolvedEntryPoint,
            UwpShaderBytePool.ReadDxil(stage, name, resolvedEntryPoint));
    }
}

/// <summary>
///     Resolves the explicit DXC inventory embedded by the shader build.
/// </summary>
public static class D3D12ShaderManifest {
    private static readonly Lazy<IReadOnlyDictionary<string, string>> EntryPoints = new(ReadEntryPoints);

    /// <summary>
    ///     Resolves the single compiled entry point for a shader stage and source name.
    /// </summary>
    /// <param name="stage">The two-letter shader stage.</param>
    /// <param name="name">The shader file name without extension.</param>
    /// <returns>The explicit DXC entry point.</returns>
    public static string ResolveEntryPoint(string stage, string name) {
        if (string.IsNullOrWhiteSpace(stage)) throw new ArgumentException("A shader stage is required.", nameof(stage));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A shader name is required.", nameof(name));

        var key = $"{stage.ToUpperInvariant()}/{name}";
        return EntryPoints.Value.TryGetValue(key, out var entryPoint)
            ? entryPoint
            : throw new FileNotFoundException($"Shader declaration was not found in the DXC manifest: {key}", name);
    }

    /// <summary>
    ///     Reads the embedded manifest into its case-insensitive stage/name index.
    /// </summary>
    /// <returns>The immutable entry-point index.</returns>
    private static IReadOnlyDictionary<string, string> ReadEntryPoints() {
        var assembly = typeof(D3D12ShaderManifest).Assembly;
        using var stream = assembly.GetManifestResourceStream("SilkCore.Resources.DX12.shader-manifest.txt")
                           ?? throw new FileNotFoundException("The embedded DXC shader manifest is missing.");
        using var reader = new StreamReader(stream);
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in reader.ReadToEnd()
                     .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
            var parts = line.Split('|');
            if (parts.Length != 4)
                throw new InvalidDataException($"Invalid DXC shader manifest line: {line}");

            var output = parts[3].Replace('\\', '/');
            var slash = output.IndexOf('/');
            var stage = slash < 0 ? string.Empty : output[..slash];
            var suffix = $".{parts[1]}.dxil";
            var fileName = Path.GetFileName(output);
            if (!fileName.EndsWith(suffix, StringComparison.Ordinal))
                throw new InvalidDataException($"Invalid DXC shader output identity: {output}");
            var name = fileName[..^suffix.Length];
            if (!result.TryAdd($"{stage}/{name}", parts[1]))
                throw new InvalidDataException($"Duplicate DXC shader declaration: {stage}/{name}");
        }

        return result;
    }
}

/// <summary>
///     Identifies every immutable input that can change a Direct3D 12 pipeline state.
/// </summary>
/// <param name="VertexShader">The vertex-shader content hash.</param>
/// <param name="PixelShader">The pixel-shader content hash.</param>
/// <param name="GeometryShader">The geometry-shader content hash.</param>
/// <param name="HullShader">The hull-shader content hash.</param>
/// <param name="DomainShader">The domain-shader content hash.</param>
/// <param name="ComputeShader">The compute-shader content hash.</param>
/// <param name="InputLayout">The complete input-layout identity.</param>
/// <param name="StreamOutput">The complete stream-output identity.</param>
/// <param name="Topology">The primitive-topology type.</param>
/// <param name="BlendState">The complete blend-state identity.</param>
/// <param name="RasterizerState">The complete rasterizer-state identity.</param>
/// <param name="DepthStencilState">The complete depth/stencil-state identity.</param>
/// <param name="SampleMask">The multisample coverage mask.</param>
/// <param name="RenderTargetFormats">The ordered render-target formats.</param>
/// <param name="DepthStencilFormat">The depth/stencil format.</param>
public readonly record struct D3D12PipelineStateKey(
    string VertexShader,
    string PixelShader,
    string GeometryShader,
    string HullShader,
    string DomainShader,
    string ComputeShader,
    string InputLayout,
    string StreamOutput,
    PrimitiveTopologyType Topology,
    string BlendState,
    string RasterizerState,
    string DepthStencilState,
    uint SampleMask,
    string RenderTargetFormats,
    Format DepthStencilFormat
) {
    /// <summary>
    ///     Creates a compute-only pipeline key.
    /// </summary>
    /// <param name="computeShader">The compute shader.</param>
    /// <returns>The compute pipeline key.</returns>
    public static D3D12PipelineStateKey Compute(D3D12ShaderModule computeShader) {
        computeShader.AssertArgumentNotNull();
        if (computeShader.Stage != "CS")
            throw new ArgumentException("A compute pipeline requires a compute shader.", nameof(computeShader));

        return new D3D12PipelineStateKey(string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            computeShader.ContentHash,
            string.Empty,
            string.Empty,
            PrimitiveTopologyType.Undefined,
            string.Empty,
            string.Empty,
            string.Empty,
            uint.MaxValue,
            string.Empty,
            Format.FormatUnknown);
    }

    /// <summary>
    ///     Creates a graphics pipeline key from shader and fixed-function state identities.
    /// </summary>
    /// <param name="vertexShader">The required vertex shader.</param>
    /// <param name="pixelShader">The optional pixel shader.</param>
    /// <param name="geometryShader">The optional geometry shader.</param>
    /// <param name="hullShader">The optional hull shader.</param>
    /// <param name="domainShader">The optional domain shader.</param>
    /// <param name="inputLayout">The input elements.</param>
    /// <param name="streamOutputElements">The stream-output declarations.</param>
    /// <param name="streamOutputStrides">The stream-output buffer strides.</param>
    /// <param name="rasterizedStream">The rasterized stream, or -1 to disable rasterization.</param>
    /// <param name="topology">The primitive-topology type.</param>
    /// <param name="blendState">The blend-state description.</param>
    /// <param name="rasterizerState">The rasterizer-state description.</param>
    /// <param name="depthStencilState">The depth/stencil-state description.</param>
    /// <param name="sampleMask">The multisample coverage mask.</param>
    /// <param name="renderTargetFormats">The ordered render-target formats.</param>
    /// <param name="depthStencilFormat">The depth/stencil format.</param>
    /// <returns>The complete graphics pipeline key.</returns>
    public static D3D12PipelineStateKey Graphics(
        D3D12ShaderModule vertexShader,
        D3D12ShaderModule? pixelShader,
        D3D12ShaderModule? geometryShader = null,
        D3D12ShaderModule? hullShader = null,
        D3D12ShaderModule? domainShader = null,
        IReadOnlyList<D3D12InputElementDescription>? inputLayout = null,
        IReadOnlyList<StreamOutputElement>? streamOutputElements = null,
        IReadOnlyList<int>? streamOutputStrides = null,
        int rasterizedStream = 0,
        PrimitiveTopologyType topology = PrimitiveTopologyType.Triangle,
        BlendStateDescription? blendState = null,
        RasterizerStateDescription? rasterizerState = null,
        DepthStencilStateDescription? depthStencilState = null,
        uint sampleMask = uint.MaxValue,
        IReadOnlyList<Format>? renderTargetFormats = null,
        Format depthStencilFormat = Format.FormatUnknown
    ) {
        ValidateStage(vertexShader, "VS", nameof(vertexShader));
        ValidateOptionalStage(pixelShader, "PS", nameof(pixelShader));
        ValidateOptionalStage(geometryShader, "GS", nameof(geometryShader));
        ValidateOptionalStage(hullShader, "HS", nameof(hullShader));
        ValidateOptionalStage(domainShader, "DS", nameof(domainShader));
        if ((hullShader is null) != (domainShader is null))
            throw new ArgumentException("Hull and domain shaders must be supplied together.");

        return new D3D12PipelineStateKey(vertexShader.ContentHash,
            pixelShader?.ContentHash ?? string.Empty,
            geometryShader?.ContentHash ?? string.Empty,
            hullShader?.ContentHash ?? string.Empty,
            domainShader?.ContentHash ?? string.Empty,
            string.Empty,
            GetInputLayoutIdentity(inputLayout),
            GetStreamOutputIdentity(streamOutputElements, streamOutputStrides, rasterizedStream),
            topology,
            GetBlendStateIdentity(blendState),
            GetRasterizerStateIdentity(rasterizerState),
            GetDepthStencilStateIdentity(depthStencilState),
            sampleMask,
            string.Join(',', renderTargetFormats ?? [Format.FormatR8G8B8A8Unorm]),
            depthStencilFormat);
    }

    /// <summary>
    ///     Creates a complete input-layout identity.
    /// </summary>
    private static string GetInputLayoutIdentity(IReadOnlyList<D3D12InputElementDescription>? elements) =>
        string.Join(';', (elements ?? []).Select(element =>
            $"{element.SemanticName}:{element.SemanticIndex}:{(int) element.Format}:{element.InputSlot}:" +
            $"{element.AlignedByteOffset}:{(int) element.Classification}:{element.InstanceDataStepRate}"));

    /// <summary>
    ///     Creates a complete stream-output identity.
    /// </summary>
    private static string GetStreamOutputIdentity(
        IReadOnlyList<StreamOutputElement>? elements,
        IReadOnlyList<int>? strides,
        int rasterizedStream
    ) => $"{rasterizedStream}|{string.Join(',', strides ?? [])}|" + string.Join(';',
        (elements ?? []).Select(element =>
            $"{element.Stream}:{element.SemanticName}:{element.SemanticIndex}:{element.StartComponent}:" +
            $"{element.ComponentCount}:{element.OutputSlot}"));

    /// <summary>
    ///     Creates a complete blend-state identity.
    /// </summary>
    private static string GetBlendStateIdentity(BlendStateDescription? description) {
        if (description is not { } state) return string.Empty;
        return $"{state.AlphaToCoverageEnable}:{state.IndependentBlendEnable}:" + string.Join(';',
            state.RenderTarget.Select(target =>
                $"{target.IsBlendEnabled}:{(int) target.SourceBlend}:{(int) target.DestinationBlend}:" +
                $"{(int) target.BlendOperation}:{(int) target.SourceAlphaBlend}:" +
                $"{(int) target.DestinationAlphaBlend}:{(int) target.AlphaBlendOperation}:" +
                $"{(int) target.RenderTargetWriteMask}"));
    }

    /// <summary>
    ///     Creates a complete rasterizer-state identity.
    /// </summary>
    private static string GetRasterizerStateIdentity(RasterizerStateDescription? description) => description is { } state
        ? $"{(int) state.FillMode}:{(int) state.CullMode}:{state.IsFrontCounterClockwise}:{state.DepthBias}:" +
          $"{state.DepthBiasClamp:R}:{state.SlopeScaledDepthBias:R}:{state.IsDepthClipEnabled}:" +
          $"{state.IsScissorEnabled}:{state.IsMultisampleEnabled}:{state.IsAntialiasedLineEnabled}"
        : string.Empty;

    /// <summary>
    ///     Creates a complete depth/stencil-state identity.
    /// </summary>
    private static string GetDepthStencilStateIdentity(DepthStencilStateDescription? description) =>
        description is { } state
            ? $"{state.IsDepthEnabled}:{(int) state.DepthWriteMask}:{(int) state.DepthComparison}:" +
              $"{state.IsStencilEnabled}:{state.StencilReadMask}:{state.StencilWriteMask}:" +
              $"{GetStencilIdentity(state.FrontFace)}:{GetStencilIdentity(state.BackFace)}"
            : string.Empty;

    /// <summary>
    ///     Creates a complete stencil-face identity.
    /// </summary>
    private static string GetStencilIdentity(DepthStencilOperationDescription state) =>
        $"{(int) state.FailOperation},{(int) state.DepthFailOperation},{(int) state.PassOperation}," +
        $"{(int) state.Comparison}";

    /// <summary>
    ///     Validates a required shader stage.
    /// </summary>
    /// <param name="shader">The shader module.</param>
    /// <param name="stage">The expected stage.</param>
    /// <param name="parameterName">The public parameter name.</param>
    private static void ValidateStage(D3D12ShaderModule shader, string stage, string parameterName) {
        shader.AssertArgumentNotNull(parameterName);
        if (shader.Stage != stage)
            throw new ArgumentException($"The shader must use the {stage} stage.", parameterName);
    }

    /// <summary>
    ///     Validates an optional shader stage.
    /// </summary>
    /// <param name="shader">The optional shader module.</param>
    /// <param name="stage">The expected stage.</param>
    /// <param name="parameterName">The public parameter name.</param>
    private static void ValidateOptionalStage(D3D12ShaderModule? shader, string stage, string parameterName) {
        if (shader is not null) ValidateStage(shader, stage, parameterName);
    }
}

/// <summary>
///     Owns one native Direct3D 12 pipeline state.
/// </summary>
public sealed unsafe class SilkD3D12PipelineState : IDisposable {
    private SilkD3D12PipelineStatePtr nativePipelineState;

    /// <summary>
    ///     Creates a pipeline-state wrapper.
    /// </summary>
    /// <param name="nativePipelineState">The native pipeline state.</param>
    internal SilkD3D12PipelineState(SilkD3D12PipelineStatePtr nativePipelineState) {
        if (nativePipelineState.Handle == null) throw new ArgumentNullException(nameof(nativePipelineState));
        this.nativePipelineState = nativePipelineState;
    }

    /// <summary>Gets the native pipeline-state pointer.</summary>
    public nint NativePointer => (nint) nativePipelineState.Handle;

    /// <summary>Gets the native pipeline-state handle for command recording.</summary>
    internal ID3D12PipelineState* Handle {
        get {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            return nativePipelineState.Handle;
        }
    }

    /// <summary>Gets whether the native pipeline state has been disposed.</summary>
    public bool IsDisposed { get; private set; }

    /// <summary>Releases the native pipeline state.</summary>
    public void Dispose() {
        if (IsDisposed) return;

        nativePipelineState.Dispose();
        IsDisposed = true;
    }
}

/// <summary>
///     Owns and reuses immutable pipeline states by their complete keys.
/// </summary>
public sealed class D3D12PipelineStateCache : IDisposable {
    private readonly Dictionary<D3D12PipelineStateKey, SilkD3D12PipelineState> pipelineStates = [];

    /// <summary>Gets the number of cached pipeline states.</summary>
    public int Count => pipelineStates.Count;

    /// <summary>Gets whether the cache and all owned states have been disposed.</summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    ///     Returns the existing pipeline state or creates and owns one new state.
    /// </summary>
    /// <param name="key">The complete pipeline key.</param>
    /// <param name="factory">The native pipeline-state factory.</param>
    /// <returns>The cached pipeline state.</returns>
    public SilkD3D12PipelineState GetOrCreate(
        D3D12PipelineStateKey key,
        Func<SilkD3D12PipelineState> factory
    ) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        factory.AssertArgumentNotNull();
        if (pipelineStates.TryGetValue(key, out var existing)) return existing;

        var created = factory() ?? throw new InvalidOperationException("The pipeline factory returned null.");
        pipelineStates.Add(key, created);
        return created;
    }

    /// <summary>Releases every cached native pipeline state.</summary>
    public void Dispose() {
        if (IsDisposed) return;

        foreach (var pipelineState in pipelineStates.Values) pipelineState.Dispose();
        pipelineStates.Clear();
        IsDisposed = true;
    }
}

/// <summary>
///     Creates Direct3D 12 pipeline-state objects from immutable shader modules.
/// </summary>
public static unsafe class SilkD3D12PipelineExtensions {
    /// <summary>
    ///     Creates a compute pipeline using the shared root signature.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="rootSignature">The compatible root signature.</param>
    /// <param name="computeShader">The compute shader.</param>
    /// <returns>The created compute pipeline state.</returns>
    public static SilkD3D12PipelineState CreateComputePipelineState(
        this SilkD3D12Device device,
        SilkD3D12RootSignature rootSignature,
        D3D12ShaderModule computeShader
    ) {
        device.AssertArgumentNotNull();
        rootSignature.AssertArgumentNotNull();
        computeShader.AssertArgumentNotNull();
        if (computeShader.Stage != "CS")
            throw new ArgumentException("A compute pipeline requires a compute shader.", nameof(computeShader));

        fixed (byte* byteCode = computeShader.ByteCode.Span) {
            var description = new ComputePipelineStateDesc {
                PRootSignature = rootSignature.Handle,
                CS = new ShaderBytecode(byteCode, (nuint) computeShader.ByteCode.Length)
            };
            SilkMarshal.ThrowHResult(device.NativeDevice.CreateComputePipelineState(in description,
                out SilkD3D12PipelineStatePtr pipelineState));
            return new SilkD3D12PipelineState(pipelineState);
        }
    }

    /// <summary>
    ///     Creates a graphics pipeline using the shared root signature and default opaque fixed-function state.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="rootSignature">The compatible root signature.</param>
    /// <param name="vertexShader">The vertex shader.</param>
    /// <param name="pixelShader">The optional pixel shader.</param>
    /// <param name="geometryShader">The optional geometry shader.</param>
    /// <param name="hullShader">The optional hull shader.</param>
    /// <param name="domainShader">The optional domain shader.</param>
    /// <param name="topology">The primitive-topology type.</param>
    /// <param name="renderTargetFormats">The ordered render-target formats.</param>
    /// <param name="depthStencilFormat">The optional depth/stencil format.</param>
    /// <param name="inputElements">The optional vertex input layout.</param>
    /// <param name="streamOutputElements">The optional stream-output declarations.</param>
    /// <param name="streamOutputStrides">The stream-output buffer strides.</param>
    /// <param name="rasterizedStream">The rasterized stream, or -1 to disable rasterization.</param>
    /// <param name="blendState">The optional blend state.</param>
    /// <param name="rasterizerState">The optional rasterizer state.</param>
    /// <param name="depthStencilState">The optional depth/stencil state.</param>
    /// <param name="sampleMask">The multisample coverage mask.</param>
    /// <returns>The created graphics pipeline state.</returns>
    public static SilkD3D12PipelineState CreateGraphicsPipelineState(
        this SilkD3D12Device device,
        SilkD3D12RootSignature rootSignature,
        D3D12ShaderModule vertexShader,
        D3D12ShaderModule? pixelShader,
        D3D12ShaderModule? geometryShader = null,
        D3D12ShaderModule? hullShader = null,
        D3D12ShaderModule? domainShader = null,
        PrimitiveTopologyType topology = PrimitiveTopologyType.Triangle,
        IReadOnlyList<Format>? renderTargetFormats = null,
        Format depthStencilFormat = Format.FormatUnknown,
        IReadOnlyList<D3D12InputElementDescription>? inputElements = null,
        IReadOnlyList<StreamOutputElement>? streamOutputElements = null,
        IReadOnlyList<int>? streamOutputStrides = null,
        int rasterizedStream = 0,
        BlendStateDescription? blendState = null,
        RasterizerStateDescription? rasterizerState = null,
        DepthStencilStateDescription? depthStencilState = null,
        uint sampleMask = uint.MaxValue
    ) {
        device.AssertArgumentNotNull();
        rootSignature.AssertArgumentNotNull();
        _ = D3D12PipelineStateKey.Graphics(vertexShader,
            pixelShader,
            geometryShader,
            hullShader,
            domainShader,
            inputElements,
            streamOutputElements,
            streamOutputStrides,
            rasterizedStream,
            topology: topology,
            blendState: blendState,
            rasterizerState: rasterizerState,
            depthStencilState: depthStencilState,
            sampleMask: sampleMask,
            renderTargetFormats: renderTargetFormats,
            depthStencilFormat: depthStencilFormat);

        var vertexBytes = vertexShader.ByteCode.Span;
        var pixelBytes = pixelShader is null ? ReadOnlySpan<byte>.Empty : pixelShader.ByteCode.Span;
        var geometryBytes = geometryShader is null ? ReadOnlySpan<byte>.Empty : geometryShader.ByteCode.Span;
        var hullBytes = hullShader is null ? ReadOnlySpan<byte>.Empty : hullShader.ByteCode.Span;
        var domainBytes = domainShader is null ? ReadOnlySpan<byte>.Empty : domainShader.ByteCode.Span;
        var elements = inputElements ?? Array.Empty<D3D12InputElementDescription>();
        var outputElements = streamOutputElements ?? Array.Empty<StreamOutputElement>();
        var outputStrides = streamOutputStrides ?? Array.Empty<int>();
        if (outputElements.Count > 0 && geometryShader is null)
            throw new ArgumentException("Stream output requires a geometry shader.", nameof(streamOutputElements));
        if (outputElements.Count > 0 && outputStrides.Count == 0)
            throw new ArgumentException("Stream output requires at least one buffer stride.",
                nameof(streamOutputStrides));
        if (outputStrides.Any(stride => stride <= 0))
            throw new ArgumentOutOfRangeException(nameof(streamOutputStrides));
        var semanticNames = new nint[elements.Count];
        var outputSemanticNames = new nint[outputElements.Count];
        try {
            var nativeElements = stackalloc InputElementDesc[elements.Count];
            for (var index = 0; index < elements.Count; index++) {
                var element = elements[index];
                if (string.IsNullOrWhiteSpace(element.SemanticName))
                    throw new ArgumentException("Input semantics cannot be empty.", nameof(inputElements));
                semanticNames[index] = SilkMarshal.StringToPtr(element.SemanticName);
                nativeElements[index] = new InputElementDesc {
                    SemanticName = (byte*) semanticNames[index],
                    SemanticIndex = element.SemanticIndex,
                    Format = element.Format,
                    InputSlot = element.InputSlot,
                    AlignedByteOffset = element.AlignedByteOffset,
                    InputSlotClass = element.Classification,
                    InstanceDataStepRate = element.InstanceDataStepRate
                };
            }
            var nativeOutputElements = stackalloc SODeclarationEntry[outputElements.Count];
            for (var index = 0; index < outputElements.Count; index++) {
                var element = outputElements[index];
                if (string.IsNullOrWhiteSpace(element.SemanticName))
                    throw new ArgumentException("Stream-output semantics cannot be empty.",
                        nameof(streamOutputElements));
                outputSemanticNames[index] = SilkMarshal.StringToPtr(element.SemanticName);
                nativeOutputElements[index] = new SODeclarationEntry {
                    Stream = checked((uint) element.Stream),
                    SemanticName = (byte*) outputSemanticNames[index],
                    SemanticIndex = checked((uint) element.SemanticIndex),
                    StartComponent = element.StartComponent,
                    ComponentCount = element.ComponentCount,
                    OutputSlot = element.OutputSlot
                };
            }
            var nativeOutputStrides = stackalloc uint[outputStrides.Count];
            for (var index = 0; index < outputStrides.Count; index++)
                nativeOutputStrides[index] = checked((uint) outputStrides[index]);

            fixed (byte* vertexByteCode = vertexBytes)
            fixed (byte* pixelByteCode = pixelBytes)
            fixed (byte* geometryByteCode = geometryBytes)
            fixed (byte* hullByteCode = hullBytes)
            fixed (byte* domainByteCode = domainBytes) {
            var formats = renderTargetFormats ?? [Format.FormatR8G8B8A8Unorm];
            if (formats.Count > 8)
                throw new ArgumentOutOfRangeException(nameof(renderTargetFormats));
            var nativeBlendState = CreateBlendState(blendState);
            var description = new GraphicsPipelineStateDesc {
                PRootSignature = rootSignature.Handle,
                VS = new ShaderBytecode(vertexByteCode, (nuint) vertexBytes.Length),
                PS = pixelBytes.IsEmpty
                    ? default
                    : new ShaderBytecode(pixelByteCode, (nuint) pixelBytes.Length),
                GS = geometryBytes.IsEmpty
                    ? default
                    : new ShaderBytecode(geometryByteCode, (nuint) geometryBytes.Length),
                HS = hullBytes.IsEmpty
                    ? default
                    : new ShaderBytecode(hullByteCode, (nuint) hullBytes.Length),
                DS = domainBytes.IsEmpty
                    ? default
                    : new ShaderBytecode(domainByteCode, (nuint) domainBytes.Length),
                StreamOutput = new StreamOutputDesc {
                    PSODeclaration = nativeOutputElements,
                    NumEntries = (uint) outputElements.Count,
                    PBufferStrides = nativeOutputStrides,
                    NumStrides = (uint) outputStrides.Count,
                    RasterizedStream = rasterizedStream < 0 ? uint.MaxValue : checked((uint) rasterizedStream)
                },
                BlendState = nativeBlendState,
                SampleMask = sampleMask,
                RasterizerState = CreateRasterizerState(rasterizerState),
                DepthStencilState = CreateDepthStencilState(depthStencilState, depthStencilFormat),
                InputLayout = new InputLayoutDesc {
                    PInputElementDescs = nativeElements,
                    NumElements = (uint) elements.Count
                },
                IBStripCutValue = IndexBufferStripCutValue.ValueDisabled,
                PrimitiveTopologyType = topology,
                NumRenderTargets = (uint) formats.Count,
                DSVFormat = depthStencilFormat,
                SampleDesc = new SampleDesc(1, 0),
                Flags = PipelineStateFlags.None
            };
            for (var index = 0; index < formats.Count; index++)
                SetRenderTargetFormat(ref description, index, formats[index]);
            SilkMarshal.ThrowHResult(device.NativeDevice.CreateGraphicsPipelineState(in description,
                out SilkD3D12PipelineStatePtr pipelineState));
            return new SilkD3D12PipelineState(pipelineState);
            }
        } finally {
            foreach (var semanticName in semanticNames)
                if (semanticName != 0)
                    SilkMarshal.Free(semanticName);
            foreach (var semanticName in outputSemanticNames)
                if (semanticName != 0)
                    SilkMarshal.Free(semanticName);
        }
    }

    /// <summary>
    ///     Translates the renderer blend state into a native Direct3D 12 description.
    /// </summary>
    private static BlendDesc CreateBlendState(BlendStateDescription? source) {
        var result = new BlendDesc {
            AlphaToCoverageEnable = source?.AlphaToCoverageEnable ?? false,
            IndependentBlendEnable = source?.IndependentBlendEnable ?? false
        };
        var targets = source?.RenderTarget;
        for (var index = 0; index < 8; index++) {
            var target = targets is {Length: > 0} && index < targets.Length
                ? targets[index]
                : new RenderTargetBlendDescription {
                    SourceBlend = BlendOption.One,
                    DestinationBlend = BlendOption.Zero,
                    BlendOperation = BlendOperation.Add,
                    SourceAlphaBlend = BlendOption.One,
                    DestinationAlphaBlend = BlendOption.Zero,
                    AlphaBlendOperation = BlendOperation.Add,
                    RenderTargetWriteMask = ColorWriteMaskFlags.All
                };
            result.RenderTarget[index] = new RenderTargetBlendDesc {
                BlendEnable = target.IsBlendEnabled,
                LogicOpEnable = false,
                SrcBlend = (Blend) target.SourceBlend,
                DestBlend = (Blend) target.DestinationBlend,
                BlendOp = (BlendOp) target.BlendOperation,
                SrcBlendAlpha = (Blend) target.SourceAlphaBlend,
                DestBlendAlpha = (Blend) target.DestinationAlphaBlend,
                BlendOpAlpha = (BlendOp) target.AlphaBlendOperation,
                LogicOp = LogicOp.Noop,
                RenderTargetWriteMask = (byte) target.RenderTargetWriteMask
            };
        }

        return result;
    }

    /// <summary>
    ///     Translates the renderer rasterizer state into a native Direct3D 12 description.
    /// </summary>
    private static RasterizerDesc CreateRasterizerState(RasterizerStateDescription? source) {
        var state = source ?? new RasterizerStateDescription {
            FillMode = HelixToolkit.SharpDX.Core.Native.FillMode.Solid,
            CullMode = HelixToolkit.SharpDX.Core.Native.CullMode.None,
            IsDepthClipEnabled = true
        };
        return new RasterizerDesc {
            FillMode = (Silk.NET.Direct3D12.FillMode) state.FillMode,
            CullMode = (Silk.NET.Direct3D12.CullMode) state.CullMode,
            FrontCounterClockwise = state.IsFrontCounterClockwise,
            DepthBias = state.DepthBias,
            DepthBiasClamp = state.DepthBiasClamp,
            SlopeScaledDepthBias = state.SlopeScaledDepthBias,
            DepthClipEnable = state.IsDepthClipEnabled,
            MultisampleEnable = state.IsMultisampleEnabled,
            AntialiasedLineEnable = state.IsAntialiasedLineEnabled,
            ForcedSampleCount = 0,
            ConservativeRaster = ConservativeRasterizationMode.Off
        };
    }

    /// <summary>
    ///     Translates the renderer depth/stencil state into a native Direct3D 12 description.
    /// </summary>
    private static DepthStencilDesc CreateDepthStencilState(
        DepthStencilStateDescription? source,
        Format depthStencilFormat
    ) {
        var state = source ?? new DepthStencilStateDescription {
            IsDepthEnabled = depthStencilFormat != Format.FormatUnknown,
            DepthWriteMask = HelixToolkit.SharpDX.Core.Native.DepthWriteMask.All,
            DepthComparison = Comparison.Less,
            StencilReadMask = byte.MaxValue,
            StencilWriteMask = byte.MaxValue
        };
        return new DepthStencilDesc {
            DepthEnable = state.IsDepthEnabled,
            DepthWriteMask = (Silk.NET.Direct3D12.DepthWriteMask) state.DepthWriteMask,
            DepthFunc = (ComparisonFunc) state.DepthComparison,
            StencilEnable = state.IsStencilEnabled,
            StencilReadMask = state.StencilReadMask,
            StencilWriteMask = state.StencilWriteMask,
            FrontFace = CreateStencilOperation(state.FrontFace),
            BackFace = CreateStencilOperation(state.BackFace)
        };
    }

    /// <summary>
    ///     Translates one renderer stencil face into a native Direct3D 12 description.
    /// </summary>
    private static DepthStencilopDesc CreateStencilOperation(DepthStencilOperationDescription source) => new() {
        StencilFailOp = (StencilOp) source.FailOperation,
        StencilDepthFailOp = (StencilOp) source.DepthFailOperation,
        StencilPassOp = (StencilOp) source.PassOperation,
        StencilFunc = (ComparisonFunc) source.Comparison
    };

    /// <summary>
    ///     Assigns one render-target format in the native fixed-size array.
    /// </summary>
    private static void SetRenderTargetFormat(
        ref GraphicsPipelineStateDesc description,
        int index,
        Format format
    ) {
        switch (index) {
            case 0: description.RTVFormats.Element0 = format; break;
            case 1: description.RTVFormats.Element1 = format; break;
            case 2: description.RTVFormats.Element2 = format; break;
            case 3: description.RTVFormats.Element3 = format; break;
            case 4: description.RTVFormats.Element4 = format; break;
            case 5: description.RTVFormats.Element5 = format; break;
            case 6: description.RTVFormats.Element6 = format; break;
            case 7: description.RTVFormats.Element7 = format; break;
            default: throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}

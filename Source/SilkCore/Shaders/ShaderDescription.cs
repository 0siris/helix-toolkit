/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.Serialization;
using HelixToolkit.SharpDX.Core.DefaultShaders;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.ShaderManager;

namespace HelixToolkit.SharpDX.Core.Shaders;
/// <summary>
/// </summary>
[DataContract]
public sealed class ShaderDescription {
    private static LoggerLib.ILog Logger => LoggerLib.Logger.Current;

    private readonly IShaderByteCodeReader? byteCodeReader;
    private D3D12ShaderModule? d3D12Module;

    /// <summary>
    ///     Create a empty description
    /// </summary>
    public ShaderDescription() {
        ShaderReflector = new ShaderReflector();
    }

    /// <summary>
    /// </summary>
    /// <param name="name"></param>
    /// <param name="type"></param>
    /// <param name="byteCode"></param>
    public ShaderDescription(string name, ShaderStage type, byte[] byteCode) {
        Name = name;
        ShaderType = type;
        ByteCode = byteCode;
        ShaderReflector = new ShaderReflector();
    }

    /// <summary>
    ///     Manually specifiy buffer mappings.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="type"></param>
    /// <param name="featureLevel"></param>
    /// <param name="byteCode"></param>
    /// <param name="constantBuffers"></param>
    /// <param name="textures"></param>
    /// <param name="samplers"></param>
    public ShaderDescription(
        string name,
        ShaderStage type,
        FeatureLevel featureLevel,
        byte[] byteCode,
        ConstantBufferMapping[]? constantBuffers = null,
        TextureMapping[]? textures = null,
        SamplerMapping[]? samplers = null
    )
        : this(name, type, byteCode) {
        Level = featureLevel;
        ConstantBufferMappings = constantBuffers;
        TextureMappings = textures;
        SamplerMappings = samplers;
    }

    /// <summary>
    ///     Create shader using reflector to get buffer mapping directly from shader codes.
    ///     <para>Actual creation happened when calling <see cref="CreateShader(NativeD3DDevice, IConstantBufferPool)" /></para>
    /// </summary>
    /// <param name="name"></param>
    /// <param name="type"></param>
    /// <param name="reflector"></param>
    /// <param name="byteCode"></param>
    public ShaderDescription(string name, ShaderStage type, IShaderReflector reflector, byte[] byteCode) {
        Name = name;
        ShaderType = type;
        ByteCode = byteCode;
        ShaderReflector = reflector;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ShaderDescription" /> class. Pass <see cref="IShaderByteCodeReader" />
    ///     to read external custom shader bytecodes.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="type">The type.</param>
    /// <param name="reflector">The reflector.</param>
    /// <param name="byteCodeName">Name of the byte code.</param>
    /// <param name="byteCodeReader">Used to read external custom shader byte codes</param>
    public ShaderDescription(
        string name,
        ShaderStage type,
        IShaderReflector reflector,
        string byteCodeName,
        IShaderByteCodeReader? byteCodeReader = null
    ) {
        Name = name;
        ShaderType = type;
        ByteCodeName = byteCodeName;
        ShaderReflector = reflector;
        this.byteCodeReader = byteCodeReader ?? UwpShaderBytePool.InternalByteCodeReader;
    }

    /// <summary>
    ///     Gets or sets the name.
    /// </summary>
    /// <value>
    ///     The name.
    /// </value>
    [DataMember]
    public string? Name { get; set; }

    /// <summary>
    ///     Gets or sets the type of the shader.
    /// </summary>
    /// <value>
    ///     The type of the shader.
    /// </value>
    [DataMember]
    public ShaderStage ShaderType { get; set; }

    /// <summary>
    ///     Gets or sets the level.
    /// </summary>
    /// <value>
    ///     The level.
    /// </value>
    [IgnoreDataMember]
    public FeatureLevel Level { get; private set; }

    /// <summary>
    ///     Gets or sets the byte code.
    /// </summary>
    /// <value>
    ///     The byte code.
    /// </value>
    [DataMember]
    public byte[]? ByteCode {
        get {
            if (field == null && !string.IsNullOrEmpty(ByteCodeName)) {
                var stage = GetD3D12Stage(ShaderType);
                field = ReferenceEquals(byteCodeReader, UwpShaderBytePool.InternalByteCodeReader)
                    ? UwpShaderBytePool.ReadDxil(stage,
                        ByteCodeName,
                        D3D12ShaderManifest.ResolveEntryPoint(stage, ByteCodeName))
                    : byteCodeReader?.ReadDxil(stage, ByteCodeName, "main");
            }
            return field;
        }
        set {
            field = value;
            d3D12Module = null;
        }
    }

    /// <summary>
    ///     Gets the immutable SM6 DXIL module used by the Direct3D 12 pipeline cache.
    /// </summary>
    [IgnoreDataMember]
    public D3D12ShaderModule? D3D12Module {
        get {
            if (d3D12Module is not null) return d3D12Module;
            if (ByteCode is not {Length: > 0} byteCode) return null;

            var name = ByteCodeName ?? Name ?? throw new InvalidOperationException("Shader name is required.");
            var stage = GetD3D12Stage(ShaderType);
            var entryPoint = ByteCodeName is null ? "main" : D3D12ShaderManifest.ResolveEntryPoint(stage, name);
            return d3D12Module = new D3D12ShaderModule(stage, name, entryPoint, byteCode);
        }
    }

    /// <summary>
    ///     Gets or sets the name of the byte code.
    /// </summary>
    /// <value>
    ///     The name of the byte code.
    /// </value>
    [IgnoreDataMember]
    public string? ByteCodeName { get; }

    /// <summary>
    ///     Gets or sets the constant buffer mappings.
    /// </summary>
    /// <value>
    ///     The constant buffer mappings.
    /// </value>
    [IgnoreDataMember]
    public ConstantBufferMapping[]? ConstantBufferMappings { get; private set; }

    /// <summary>
    ///     Gets or sets the texture mappings.
    /// </summary>
    /// <value>
    ///     The texture mappings.
    /// </value>
    [IgnoreDataMember]
    public TextureMapping[]? TextureMappings { get; private set; }

    /// <summary>
    ///     Gets or sets the uav mappings.
    /// </summary>
    /// <value>
    ///     The uav mappings.
    /// </value>
    [IgnoreDataMember]
    public UavMapping[]? UavMappings { get; private set; }

    /// <summary>
    ///     Gets or sets the sampler mappings.
    /// </summary>
    /// <value>
    ///     The sampler mappings.
    /// </value>
    [IgnoreDataMember]
    public SamplerMapping[]? SamplerMappings { get; private set; }

    /// <summary>
    ///     Gets or sets the shader reflector.
    /// </summary>
    /// <value>
    ///     The shader reflector.
    /// </value>
    [IgnoreDataMember]
    public IShaderReflector ShaderReflector { get; private set; }

    /// <summary>
    ///     Clones this instance.
    /// </summary>
    /// <returns></returns>
    public ShaderDescription Clone() {
        return new ShaderDescription(Name ?? throw new InvalidOperationException("Shader name is required."),
                                     ShaderType,
                                     Level,
                                     ByteCode ?? throw new InvalidOperationException("Shader byte code is required."),
                                     [.. (ConstantBufferMappings ?? []).Select(x => x.Clone())],
                                     [.. (TextureMappings ?? []).Select(x => x.Clone())]);
    }

    /// <summary>
    ///     Converts the public shader-stage enum to the DXC manifest stage.
    /// </summary>
    /// <param name="stage">The shader stage.</param>
    /// <returns>The two-letter DXC stage.</returns>
    private static string GetD3D12Stage(ShaderStage stage) => stage switch {
        ShaderStage.Vertex => "VS",
        ShaderStage.Pixel => "PS",
        ShaderStage.Geometry => "GS",
        ShaderStage.Hull => "HS",
        ShaderStage.Domain => "DS",
        ShaderStage.Compute => "CS",
        _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unsupported shader stage.")
    };

    #region GS Stream output Only

    /// <summary>
    ///     Gets or sets a value indicating whether this instance is gs stream out.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance is gs stream out; otherwise, <c>false</c>.
    /// </value>
    [DataMember]
    public bool IsGsStreamOut { get; set; }

    /// <summary>
    ///     Gets or sets the gs stream output element.
    /// </summary>
    /// <value>
    ///     The gsso element.
    /// </value>
    [DataMember]
    public StreamOutputElement[] GssoElement { get; set; } = [];

    /// <summary>
    ///     Gets or sets the gs stream output strides.
    /// </summary>
    /// <value>
    ///     The gsso strides.
    /// </value>
    [DataMember]
    public int[] GssoStrides { get; set; } = [];

    /// <summary>
    ///     Gets or sets the gs stream output rasterized stream index.
    /// </summary>
    /// <value>
    ///     The gsso rasterized stream index.
    /// </value>
    [DataMember]
    public int GssoRasterized { get; set; } = -1;

    #endregion
}

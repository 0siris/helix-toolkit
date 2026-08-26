/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.Serialization;
using HelixToolkit.SharpDX.Core.DefaultShaders;
using HelixToolkit.SharpDX.Core.Native;

namespace HelixToolkit.SharpDX.Core.Shaders;
/// <summary>
/// </summary>
[DataContract]
public sealed class InputLayoutDescription {
    /// <summary>
    ///     The empty input layout
    /// </summary>
    public static readonly InputLayoutDescription EmptyInputLayout = new();

    private readonly IShaderByteCodeReader byteCodeReader = UwpShaderBytePool.InternalByteCodeReader;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InputLayoutDescription" /> class.
    /// </summary>
    /// <param name="byteCode">The byte code.</param>
    /// <param name="elements">The elements.</param>
    public InputLayoutDescription(byte[] byteCode, InputElement[] elements) {
        ShaderByteCode = byteCode;
        InputElements = elements;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="InputLayoutDescription" /> class. Pass custom
    ///     <see cref="IShaderByteCodeReader" /> to read external shader bytecodes.
    /// </summary>
    /// <param name="byteCodeName">The byte code name.</param>
    /// <param name="elements">The elements.</param>
    /// <param name="byteCodeReader"></param>
    public InputLayoutDescription(
        string byteCodeName,
        InputElement[] elements,
        IShaderByteCodeReader? byteCodeReader = null
    ) {
        ShaderByteCodeName = byteCodeName;
        InputElements = elements;
        this.byteCodeReader = byteCodeReader ?? UwpShaderBytePool.InternalByteCodeReader;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="InputLayoutDescription" /> class.
    /// </summary>
    public InputLayoutDescription() { }

    /// <summary>
    ///     Gets or sets the shader byte code.
    /// </summary>
    /// <value>
    ///     The shader byte code.
    /// </value>
    [DataMember]
    public byte[]? ShaderByteCode {
        get {
            if (field == null && ShaderByteCodeName is { } name) {
                var entryPoint = D3D12ShaderManifest.ResolveEntryPoint("VS", name);
                field = ReferenceEquals(byteCodeReader, UwpShaderBytePool.InternalByteCodeReader)
                    ? UwpShaderBytePool.ReadDxil("VS", name, entryPoint)
                    : byteCodeReader.ReadDxil("VS", name, entryPoint);
            }
            return field;
        }
        set;
    }

    [IgnoreDataMember]
    public string? ShaderByteCodeName { get; }

    /// <summary>
    ///     Gets or sets the input elements.
    /// </summary>
    /// <value>
    ///     The input elements.
    /// </value>
    [DataMember]
    public InputElement[] InputElements { get; set; } = [];

    /// <summary>
    ///     Gets the Direct3D 12 input elements without requiring vertex-shader bytecode reflection.
    /// </summary>
    [IgnoreDataMember]
    public IReadOnlyList<D3D12InputElementDescription> D3D12InputElements => InputElements.Select(element =>
        new D3D12InputElementDescription(element.SemanticName,
            checked((uint) element.SemanticIndex),
            (Silk.NET.DXGI.Format) element.Format,
            checked((uint) element.Slot),
            element.AlignedByteOffset == InputElement.AppendAligned
                ? uint.MaxValue
                : checked((uint) element.AlignedByteOffset),
            element.Classification == InputClassification.PerInstanceData
                ? Silk.NET.Direct3D12.InputClassification.PerInstanceData
                : Silk.NET.Direct3D12.InputClassification.PerVertexData,
            checked((uint) element.InstanceDataStepRate))).ToArray();

    public KeyValuePair<byte[], InputElement[]> Description => new(ShaderByteCode ?? [], InputElements);
}

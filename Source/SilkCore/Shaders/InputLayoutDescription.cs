/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.Serialization;
using HelixToolkit.SharpDX.Core.Helper;

namespace HelixToolkit.SharpDX.Core.Shaders;
/// <summary>
/// </summary>
[DataContract]
public sealed class InputLayoutDescription {
    /// <summary>
    ///     The empty input layout
    /// </summary>
    public static readonly InputLayoutDescription EmptyInputLayout = new();

    private readonly IShaderByteCodeReader byteCodeReader;

    private byte[] shaderByteCode;

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
        this.byteCodeReader = byteCodeReader ?? UWPShaderBytePool.InternalByteCodeReader;
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
    public byte[] ShaderByteCode {
        get {
            if (shaderByteCode == null && !string.IsNullOrEmpty(ShaderByteCodeName))
                shaderByteCode = UWPShaderBytePool.Read(ShaderByteCodeName, byteCodeReader);
            return shaderByteCode;
        }
        set => shaderByteCode = value;
    }

    [IgnoreDataMember]
    public string ShaderByteCodeName { get; }

    /// <summary>
    ///     Gets or sets the input elements.
    /// </summary>
    /// <value>
    ///     The input elements.
    /// </value>
    [DataMember]
    public InputElement[] InputElements { get; set; } = [];

    public KeyValuePair<byte[], InputElement[]> Description => new(ShaderByteCode, InputElements);
}

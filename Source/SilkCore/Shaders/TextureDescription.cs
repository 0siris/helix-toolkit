/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.Serialization;

namespace HelixToolkit.SharpDX.Core.Shaders;

public enum TextureType {
    Texture,
    Structured,
    TextureBuffer,
    ByteAddress
}

/// <summary>
/// </summary>
[DataContract]
public sealed class TextureDescription {
    public TextureDescription() { }

    public TextureDescription(string name, ShaderStage shaderType, TextureType type) {
        Name = name;
        ShaderType = shaderType;
        Type = type;
    }

    [DataMember]
    public string Name { get; set; }

    [DataMember]
    public ShaderStage ShaderType { get; set; }

    [DataMember]
    public TextureType Type { get; set; }

    public TextureMapping CreateMapping(int slot) => new(slot, this);

    public TextureDescription Clone() => new(Name, ShaderType, Type);
}

/// <summary>
/// </summary>
[DataContract]
public sealed class TextureMapping {
    public TextureMapping(int slot, TextureDescription description) {
        Slot = slot;
        Description = description;
    }

    [DataMember]
    public int Slot { get; set; }

    [DataMember]
    public TextureDescription Description { get; set; }

    public TextureMapping Clone() => new(Slot, Description.Clone());
}

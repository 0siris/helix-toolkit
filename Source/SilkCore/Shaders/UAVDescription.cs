using System.Runtime.Serialization;

namespace HelixToolkit.SharpDX.Core.Shaders;
public enum UnorderedAccessViewType {
    AppendStructured,
    ConsumeStructured,
    RwByteAddress,
    RwStructuredWithCounter,
    RwTyped,
    RwStructured
}

public sealed class UavDescription {
    [DataMember] public ShaderStage ShaderType;

    [DataMember] public UnorderedAccessViewType Type;

    public UavDescription() { }

    public UavDescription(string name, ShaderStage shaderType, UnorderedAccessViewType type) {
        Name = name;
        ShaderType = shaderType;
        Type = type;
    }

    [DataMember]
    public string Name { get; set; }

    public UavMapping CreateMapping(int slot) => new(slot, this);

    public UavDescription Clone() => new(Name, ShaderType, Type);
}

[DataContract]
public sealed class UavMapping {
    public UavMapping(int slot, UavDescription description) {
        Slot = slot;
        Description = description;
    }

    [DataMember]
    public int Slot { get; set; }

    [DataMember]
    public UavDescription Description { get; set; }

    public UavMapping Clone() => new(Slot, Description.Clone());
}

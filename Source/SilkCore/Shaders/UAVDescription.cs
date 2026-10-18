using HelixToolkit.SharpDX.Core;
using System.Runtime.Serialization;
using HelixToolkit.SharpDX.Core.Interface;

namespace HelixToolkit.SharpDX.Core.Shaders;
public enum UnorderedAccessViewType {
    AppendStructured,
    ConsumeStructured,
    RwByteAddress,
    RwStructuredWithCounter,
    RwTyped,
    RwStructured
}

public sealed class UAVDescription {
    [DataMember] public ShaderStage ShaderType;

    [DataMember] public UnorderedAccessViewType Type;

    public UAVDescription() { }

    public UAVDescription(string name, ShaderStage shaderType, UnorderedAccessViewType type) {
        Name = name;
        ShaderType = shaderType;
        Type = type;
    }

    [DataMember]
    public string Name { get; set; } = string.Empty;

    public UAVMapping CreateMapping(int slot) => new(slot, this);

    public UAVDescription Clone() => new(Name, ShaderType, Type);
}

[DataContract]
public sealed class UAVMapping {
    public UAVMapping(int slot, UAVDescription description) {
        Slot = slot;
        Description = description;
    }

    [DataMember]
    public int Slot { get; set; }

    [DataMember]
    public UAVDescription Description { get; set; }

    public UAVMapping Clone() => new(Slot, Description.Clone());
}

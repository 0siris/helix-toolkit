using System.Runtime.Serialization;

namespace HelixToolkit.SharpDX.Core.Shaders;
public enum UnorderedAccessViewType {
    AppendStructured,
    ConsumeStructured,
    RWByteAddress,
    RWStructuredWithCounter,
    RWTyped,
    RWStructured
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
    public string Name { get; set; }

    public UAVMapping CreateMapping(int slot) {
        return new UAVMapping(slot, this);
    }

    public UAVDescription Clone() {
        return new UAVDescription(Name, ShaderType, Type);
    }
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

    public UAVMapping Clone() {
        return new UAVMapping(Slot, Description.Clone());
    }
}

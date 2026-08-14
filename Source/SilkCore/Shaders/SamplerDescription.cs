using System.Runtime.Serialization;

namespace HelixToolkit.SharpDX.Core.Shaders;

/// <summary>
/// </summary>
[DataContract]
public sealed class SamplerMapping {
    /// <summary>
    ///     The shader type
    /// </summary>
    [DataMember] public ShaderStage ShaderType;

    public SamplerMapping() { }

    public SamplerMapping(int slot, string name, ShaderStage type) {
        Slot = slot;
        Name = name;
        ShaderType = type;
    }

    /// <summary>
    ///     Gets or sets the name.
    /// </summary>
    /// <value>
    ///     The name.
    /// </value>
    [DataMember]
    public string Name { get; set; }

    /// <summary>
    ///     Gets or sets the slot.
    /// </summary>
    /// <value>
    ///     The slot.
    /// </value>
    [DataMember]
    public int Slot { get; set; }

    public SamplerMapping Clone() => new(Slot, Name, ShaderType);
}

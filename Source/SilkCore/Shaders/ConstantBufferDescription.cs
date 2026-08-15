/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.Serialization;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Utilities;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.Shaders;

public struct ConstantBufferVariable {
    //
    // Summary:
    //     The variable name.
    public string Name;

    //
    // Summary:
    //     Offset from the start of the parent structure to the beginning of the variable.
    public int StartOffset;

    //
    // Summary:
    //     Size of the variable (in bytes).
    public int Size;
}

public sealed class ConstantBufferDescription {
    public ConstantBufferDescription(string name, int structSize, int strideSize = 0) {
        Name = name;
        StructSize = structSize;
        StrideSize = strideSize;
    }

    public ConstantBufferDescription(
        string name,
        int structSize,
        IEnumerable<ConstantBufferVariable> variables,
        int strideSize = 0
    )
        : this(name, structSize, strideSize) {
        foreach (var variable in variables) Variables.Add(variable);
    }

    public string Name { get; set; }

    public int StructSize { get; set; }

    public int StrideSize { get; set; }

    public BindFlags BindFlags { get; set; } = BindFlags.ConstantBuffer;

    public CpuAccessFlags CpuAccessFlags { get; set; } = CpuAccessFlags.Write;

    public ResourceOptionFlags OptionFlags { get; set; } = ResourceOptionFlags.None;

    public ResourceUsage Usage { get; set; } = ResourceUsage.Dynamic;

    public ShaderStage Stage { get; set; }

    public int Slot { get; set; }

    public List<ConstantBufferVariable> Variables { get; } = [];

    public ConstantBufferProxy CreateBuffer() => new(this);

    public ConstantBufferMapping CreateMapping(int slot) => new(slot, this);

    public ConstantBufferDescription Clone() => new(Name, StructSize, StrideSize) {
        BindFlags = BindFlags,
        CpuAccessFlags = CpuAccessFlags,
        OptionFlags = OptionFlags,
        Usage = Usage
    };
}

[DataContract]
public sealed class ConstantBufferMapping {
    public ConstantBufferMapping(int slot, ConstantBufferDescription description) {
        Slot = slot;
        Description = description;
    }

    [DataMember]
    public int Slot { get; set; }

    [DataMember]
    public ConstantBufferDescription Description { get; set; }

    public static ConstantBufferMapping Create(int slot, ConstantBufferDescription description)
        => new(slot, description);

    public ConstantBufferMapping Clone() => new(Slot, Description.Clone());
}
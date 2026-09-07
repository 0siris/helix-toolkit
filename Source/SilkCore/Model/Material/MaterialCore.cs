/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Interface;

namespace HelixToolkit.SharpDX.Core.Model.Material;
/// <summary>
/// </summary>
public abstract class MaterialCore : ObservableObject, IMaterial {
    public string Name {
        get;
        set => Set(ref field, value);
    } = "Material";

    public Guid Guid { get; } = Guid.NewGuid();
}

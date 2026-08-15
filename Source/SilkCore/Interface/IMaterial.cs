/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.ComponentModel;
using HelixToolkit.SharpDX.Core.Model.Material.Variables;

namespace HelixToolkit.SharpDX.Core.Interface;

/// <summary>
/// </summary>
public interface IMaterial : INotifyPropertyChanged {
    string Name { get; set; }

    Guid Guid { get; }

    MaterialVariable CreateMaterialVariables(IEffectsManager manager, IRenderTechnique technique);
}

/// <summary>
/// </summary>
public interface IMaterialVariablePool : IDisposable {
    int Count { get; }

    MaterialVariable Register(IMaterial material, IRenderTechnique technique);
}

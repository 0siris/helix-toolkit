/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.ComponentModel;

namespace HelixToolkit.SharpDX.Core.Interface;

/// <summary>
/// </summary>
public interface IMaterial : INotifyPropertyChanged {
    string Name { get; set; }

    Guid Guid { get; }
}

/// <summary>
/// </summary>
public interface IMaterialVariablePool : IDisposable {
    int Count { get; }
}

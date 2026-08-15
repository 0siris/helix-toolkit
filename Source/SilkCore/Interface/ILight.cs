/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

namespace HelixToolkit.SharpDX.Core.Interface;

/// <summary>
/// </summary>
public interface ILight3D {
    /// <summary>
    ///     Gets the type of the light.
    /// </summary>
    /// <value>
    ///     The type of the light.
    /// </value>
    LightType LightType { get; }
}

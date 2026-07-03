/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Model.Scene;

namespace HelixToolkit.Wpf.SharpDX;

/// <summary>
///     Interface for 3D exporters.
/// </summary>
public interface IExporter {
    /// <summary>
    ///     Exports the specified viewport.
    /// </summary>
    /// <param name="viewport">
    ///     The viewport.
    /// </param>
    void Export(Viewport3DX viewport);

    /// <summary>
    ///     Exports the specified model.
    /// </summary>
    /// <param name="model">
    ///     The model.
    /// </param>
    void Export(SceneNode model);
}

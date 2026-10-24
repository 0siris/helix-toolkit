/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core2D;
using HelixToolkit.SharpDX.Core.Model.Scene2D;
using HelixToolkit.SharpDX.Core.Model.Scene2D.Abstract;
using HelixToolkit.Wpf.SharpDX.Model.Elements2D.Abstract;

namespace HelixToolkit.Wpf.SharpDX.Model.Elements2D;

/// <summary>Wraps the existing path core without WPF color conversion or a second point collection.</summary>
/// <remarks>
///     Callers publish replacement Figures and exact native brushes through PathCore. Screen callers supply
///     physical coordinates, StrokeWidth=3 and identity node transform, independent of logical layout DPI.
///     Existing inherited StrokeWidth assignment requires InvalidateRender when changed outside a frame callback.
/// </remarks>
public class PathModel2D : Abstract.Element2D {
    /// <summary>Gets the one existing path render core that owns derived presentation figures and shape resources.</summary>
    public PathRenderCore2D PathCore => (PathRenderCore2D) SceneNode.RenderCore;

    /// <summary>Creates the noninteractive Scene2D path node.</summary>
    /// <returns>A fresh path node.</returns>
    protected override SceneNode2D OnCreateSceneNode() => new PathNode2D();

    /// <summary>Disposes the native path core even when the ordinary 2D host has never attached this model.</summary>
    /// <param name="disposing">Whether managed resources are being disposed.</param>
    protected override void Dispose(bool disposing) {
        if (disposing) SceneNode.Dispose();
        base.Dispose(disposing);
    }
}

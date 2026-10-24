/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core2D;
using HelixToolkit.SharpDX.Core.Core2D.Abstract;
using HelixToolkit.SharpDX.Core.Model.Scene2D.Abstract;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Model.Scene2D;

/// <summary>Exposes the existing path core as a noninteractive Scene2D node.</summary>
/// <remarks>Native D3D12 support is a stroke-only consumer; a fill brush is explicitly rejected.</remarks>
public class PathNode2D : SceneNode2D {
    /// <summary>Creates the existing path core without introducing another figure or point owner.</summary>
    /// <returns>A fresh existing path render core.</returns>
    protected override RenderCore2D CreateRenderCore() => new PathRenderCore2D();

    /// <summary>Declines hit testing because path preview geometry does not own input.</summary>
    /// <param name="mousePoint">The incoming point, left unchanged.</param>
    /// <param name="hitResult">Receives null.</param>
    /// <returns>False for every point.</returns>
    protected override bool OnHitTest(ref Vector2 mousePoint, out HitTest2DResult? hitResult) {
        hitResult = null;
        return false;
    }
}
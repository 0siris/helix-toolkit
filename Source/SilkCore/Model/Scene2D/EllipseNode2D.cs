/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core2D;
using HelixToolkit.SharpDX.Core.Core2D.Abstract;
using HelixToolkit.SharpDX.Core.Model.Scene2D.Abstract;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Model.Scene2D;
public class EllipseNode2D : ShapeNode2D {
    protected override ShapeRenderCore2DBase CreateShapeRenderCore() => new EllipseRenderCore2D();

    protected override bool OnHitTest(ref Vector2 mousePoint, out HitTest2DResult? hitResult) {
        hitResult = null;
        return false;
    }
}

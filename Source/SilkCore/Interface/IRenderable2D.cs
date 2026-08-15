/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Interface;

public interface IHitable2D {
    bool IsHitTestVisible { get; set; }

    bool HitTest(Vector2 mousePoint, out HitTest2DResult? hitResult);
}

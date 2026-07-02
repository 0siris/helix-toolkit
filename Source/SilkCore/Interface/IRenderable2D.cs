/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


namespace HelixToolkit.SharpDX.Core;

public interface IHitable2D
{
    bool IsHitTestVisible { get; set; }

    bool HitTest(Vector2 mousePoint, out HitTest2DResult hitResult);
}
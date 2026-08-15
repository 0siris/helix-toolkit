/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Native;

namespace HelixToolkit.SharpDX.Core.Core2D.Models;
/// <summary>
///     <see href="https://jeremiahmorrill.wordpress.com/2013/02/06/direct2d-gui-librarygraphucks/" />
/// </summary>
public class LineSegment : Segment {
    public readonly Vector2 Point;

    public LineSegment(Vector2 point) {
        Point = point;
    }

    public override void Create(GeometrySink sink) {
        sink.AddLine(Point);
    }
}

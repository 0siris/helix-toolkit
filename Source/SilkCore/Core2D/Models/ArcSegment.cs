/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


namespace HelixToolkit.SharpDX.Core.Core2D;
/// <summary>
///     <see href="https://jeremiahmorrill.wordpress.com/2013/02/06/direct2d-gui-librarygraphucks/" />
/// </summary>
public class ArcSegment : Segment {
    public readonly ArcSize ArcSize;
    public readonly Vector2 Point;
    public readonly float Rotation;
    public readonly Size2F Size;
    public readonly SweepDirection SweepDirection;

    public ArcSegment(
        Vector2 point,
        Size2F size,
        float rotation,
        SweepDirection sweepDirection,
        ArcSize arcSize
    ) {
        Point = point;
        Size = size;
        Rotation = rotation;
        SweepDirection = sweepDirection;
        ArcSize = arcSize;
    }

    public override void Create(GeometrySink sink) {
        sink.AddArc(new ArcSegmentData {
            ArcSize = ArcSize, Point = Point, RotationAngle = Rotation, Size = Size,
            SweepDirection = SweepDirection
        });
    }
}

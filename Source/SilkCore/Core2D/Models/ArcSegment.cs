/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


#if !NETFX_CORE
namespace HelixToolkit.Wpf.SharpDX
#else
#if CORE
namespace HelixToolkit.SharpDX.Core
#else
namespace HelixToolkit.UWP
#endif
#endif
{
    namespace Core2D
    {
        /// <summary>
        /// <see href="https://jeremiahmorrill.wordpress.com/2013/02/06/direct2d-gui-librarygraphucks/"/>
        /// </summary>
        public class ArcSegment : Segment
        {
            public readonly Vector2 Point;
            public readonly Size2F Size;
            public readonly float Rotation;
            public readonly SweepDirection SweepDirection;
            public readonly ArcSize ArcSize;

            public ArcSegment(Vector2 point, Size2F size, float rotation, SweepDirection sweepDirection, ArcSize arcSize)
            {
                Point = point;
                Size = size;
                Rotation = rotation;
                SweepDirection = sweepDirection;
                ArcSize = arcSize;
            }

            public override void Create(GeometrySink sink)
            {
                sink.AddArc(new ArcSegmentData() { ArcSize = ArcSize, Point = Point, RotationAngle = Rotation, Size = Size, SweepDirection = SweepDirection });
            }
        }
    }
}

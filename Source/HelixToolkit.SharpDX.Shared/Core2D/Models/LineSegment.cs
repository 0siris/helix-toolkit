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
        public class LineSegment : Segment
        {
            public readonly Vector2 Point;
            public LineSegment(Vector2 point)
            {
                Point = point;
            }

            public override void Create(GeometrySink sink)
            {
                sink.AddLine(Point);
            }
        }
    }
}

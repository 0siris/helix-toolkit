/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


namespace HelixToolkit.SharpDX.Core
{
    namespace Core2D
    {
        /// <summary>
        /// <see href="https://jeremiahmorrill.wordpress.com/2013/02/06/direct2d-gui-librarygraphucks/"/>
        /// </summary>
        public interface ISegment
        {
            bool IsDirty
            {
                get;
            }
            void Create(GeometrySink sink);
        }

        /// <summary>
        /// <see href="https://jeremiahmorrill.wordpress.com/2013/02/06/direct2d-gui-librarygraphucks/"/>
        /// </summary>
        public abstract class Segment : ISegment
        {
            public bool IsDirty { private set; get; } = false;

            protected void Invalidate()
            {
                IsDirty = true;
            }

            public abstract void Create(GeometrySink sink);
        }
    }
}

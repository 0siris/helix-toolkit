/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

namespace HelixToolkit.SharpDX.Core {
    namespace Utilities {
        public class UniformRandomVectorGenerator : IRandomVector {
            private readonly Random random = new(Environment.TickCount);
            public Vector3 MinVector { get; set; } = -Vector3.One;

            public Vector3 MaxVector { get; set; } = Vector3.One;

            public Vector3 RandomVector3 => random.NextVector3(MinVector, MaxVector);

            public uint Seed => (uint)Math.Abs(random.Next());
        }
    }
}

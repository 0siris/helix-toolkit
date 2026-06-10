/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using System.Runtime.InteropServices;

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
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Bool4
    {
        private int x;
        private int y;
        private int z;
        private int w;

        public bool X
        {
            get => x != 0;
            set => x = value ? 1 : 0;
        }

        public bool Y
        {
            get => y != 0;
            set => y = value ? 1 : 0;
        }

        public bool Z
        {
            get => z != 0;
            set => z = value ? 1 : 0;
        }

        public bool W
        {
            get => w != 0;
            set => w = value ? 1 : 0;
        }
    }

    public struct FrustumCameraParams
    {
        public Vector3 Position;
        public Vector3 LookAtDir;
        public Vector3 UpDir;
        public float FOV;
        public float AspectRatio;
        public float ZNear;
        public float ZFar;
    }

    public struct ViewportF
    {
        public ViewportF(float x, float y, float width, float height, float minDepth = 0, float maxDepth = 1)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            MinDepth = minDepth;
            MaxDepth = maxDepth;
        }

        public float X;
        public float Y;
        public float Width;
        public float Height;
        public float MinDepth;
        public float MaxDepth;
    }
}

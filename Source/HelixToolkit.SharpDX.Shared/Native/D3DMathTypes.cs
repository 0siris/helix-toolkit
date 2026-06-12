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

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Size2
    {
        public Size2(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public int Width;
        public int Height;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Matrix3x2
    {
        public Matrix3x2(float m11, float m12, float m21, float m22, float m31, float m32)
        {
            M11 = m11;
            M12 = m12;
            M21 = m21;
            M22 = m22;
            M31 = m31;
            M32 = m32;
        }

        public float M11;
        public float M12;
        public float M21;
        public float M22;
        public float M31;
        public float M32;

        public static Matrix3x2 Identity => new Matrix3x2(1, 0, 0, 1, 0, 0);

        public static Matrix3x2 Translation(float x, float y)
        {
            return new Matrix3x2(1, 0, 0, 1, x, y);
        }

        public static Matrix3x2 operator *(Matrix3x2 left, Matrix3x2 right)
        {
            return new Matrix3x2(
                (left.M11 * right.M11) + (left.M12 * right.M21),
                (left.M11 * right.M12) + (left.M12 * right.M22),
                (left.M21 * right.M11) + (left.M22 * right.M21),
                (left.M21 * right.M12) + (left.M22 * right.M22),
                (left.M31 * right.M11) + (left.M32 * right.M21) + right.M31,
                (left.M31 * right.M12) + (left.M32 * right.M22) + right.M32);
        }
    }
}

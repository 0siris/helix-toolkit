using System.Runtime.CompilerServices;
using System.Windows.Media.Media3D;
using HelixToolkit.SharpDX.Core;
using Media = System.Windows.Media;
using Point = System.Windows.Point;

namespace HelixToolkit.Wpf.SharpDX;

public static class Media3DExtension {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3D ToVector3D(this Vector3 vector) {
        return new Vector3D(vector.X, vector.Y, vector.Z);
    }

    public static Matrix3X3 ToMatrix3x3(this Media.Matrix m) {
        return new Matrix3X3((float)m.M11,
                             (float)m.M12,
                             0,
                             (float)m.M21,
                             (float)m.M22,
                             0f,
                             (float)m.OffsetX,
                             (float)m.OffsetY,
                             1f);
    }

    public static Matrix3X2 ToMatrix3x2(this Media.Matrix m) {
        return new Matrix3X2((float)m.M11,
                             (float)m.M12,
                             (float)m.M21,
                             (float)m.M22,
                             (float)m.OffsetX,
                             (float)m.OffsetY);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3D ToVector3D(this Transform3D trafo) {
        var matrix = trafo.Value;
        var w = 1.0 / matrix.M44;
        return new Vector3D(w * matrix.OffsetX, w * matrix.OffsetY, w * matrix.OffsetZ);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Point3D ToPoint3D(this Vector3 vector) {
        return new Point3D(vector.X, vector.Y, vector.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Size3D ToSize3D(this Vector3 vector) {
        return new Size3D(vector.X, vector.Y, vector.Z);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix3D ToMatrix3D(this Matrix m) {
        return new Matrix3D(m.M11,
                            m.M12,
                            m.M13,
                            m.M14,
                            m.M21,
                            m.M22,
                            m.M23,
                            m.M24,
                            m.M31,
                            m.M32,
                            m.M33,
                            m.M34,
                            m.M41,
                            m.M42,
                            m.M43,
                            m.M44);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 ToVector3(this Point3D point) {
        return new Vector3((float)point.X, (float)point.Y, (float)point.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 ToVector4(this Point3D point, float w = 1f) {
        return new Vector4((float)point.X, (float)point.Y, (float)point.Z, w);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 ToVector3(this Vector3D vector) {
        return new Vector3((float)vector.X, (float)vector.Y, (float)vector.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 ToVector4(this Vector3D vector, float w = 1f) {
        return new Vector4((float)vector.X, (float)vector.Y, (float)vector.Z, w);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 ToVector4(this Transform3D trafo) {
        var matrix = trafo.Value;
        return new Vector4((float)matrix.OffsetX, (float)matrix.OffsetY, (float)matrix.OffsetZ, (float)matrix.M44);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 ToVector3(this Transform3D trafo) {
        var matrix = trafo.Value;
        return new Vector3((float)matrix.OffsetX, (float)matrix.OffsetY, (float)matrix.OffsetZ);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix ToMatrix(this Transform3D trafo) {
        var m = trafo.Value;
        return new Matrix((float)m.M11,
                          (float)m.M12,
                          (float)m.M13,
                          (float)m.M14,
                          (float)m.M21,
                          (float)m.M22,
                          (float)m.M23,
                          (float)m.M24,
                          (float)m.M31,
                          (float)m.M32,
                          (float)m.M33,
                          (float)m.M34,
                          (float)m.OffsetX,
                          (float)m.OffsetY,
                          (float)m.OffsetZ,
                          (float)m.M44);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix ToMatrix(this Matrix3D m) {
        return new Matrix((float)m.M11,
                          (float)m.M12,
                          (float)m.M13,
                          (float)m.M14,
                          (float)m.M21,
                          (float)m.M22,
                          (float)m.M23,
                          (float)m.M24,
                          (float)m.M31,
                          (float)m.M32,
                          (float)m.M33,
                          (float)m.M34,
                          (float)m.OffsetX,
                          (float)m.OffsetY,
                          (float)m.OffsetZ,
                          (float)m.M44);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Transform3D AppendTransform(this Transform3D t1, Transform3D t2) {
        var g = new Transform3DGroup();
        g.Children.Add(t1);
        g.Children.Add(t2);
        return g;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Transform3D PrependTransform(this Transform3D t1, Transform3D t2) {
        var g = new Transform3DGroup();
        g.Children.Add(t2);
        g.Children.Add(t1);
        return g;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 ToVector2(this Point vector) {
        return new Vector2((float)vector.X, (float)vector.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Point ToPoint(this Vector2 vector) {
        return new Point(vector.X, vector.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color4 ToColor4(this Media.Color color) {
        return new Color4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Media.Color ToColor(this Color4 color) {
        return Media.Color.FromArgb((byte)(color.W * 255),
                                    (byte)(color.X * 255),
                                    (byte)(color.Y * 255),
                                    (byte)(color.Z * 255));
        //return System.Windows.Media.Color.FromScRgb(color.Alpha, color.Red, color.Green, color.Blue);
    }
}

using System.Runtime.CompilerServices;

namespace HelixToolkit.SharpDX.Core;

using DoubleOrSingle = float;
using Point3D = Color3;
using Vector = Vector2;
using Vector3D = Color3;

/// <summary>
///     Functions for the Shared Projects to simplify the Code
/// </summary>
internal static class SharedFunctions {
    /// <summary>
    /// </summary>
    /// <param name="first"></param>
    /// <param name="second"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3D CrossProduct(ref Vector3D first, ref Vector3D second) {
        return SilkMath.Cross(first, second);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3D CrossProduct(Vector3D first, Vector3D second) {
        return SilkMath.Cross(first, second);
    }

    /// <summary>
    /// </summary>
    /// <param name="first"></param>
    /// <param name="second"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DoubleOrSingle DotProduct(ref Vector3D first, ref Vector3D second) {
        return first.X * second.X + first.Y * second.Y + first.Z * second.Z;
    }

    /// <summary>
    /// </summary>
    /// <param name="first"></param>
    /// <param name="second"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DoubleOrSingle DotProduct(ref Vector first, ref Vector second) {
        return first.X * second.X + first.Y * second.Y;
    }

    /// <summary>
    /// </summary>
    /// <param name="vector"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DoubleOrSingle LengthSquared(ref Vector3D vector) {
        return vector.X * vector.X + vector.Y * vector.Y + vector.Z * vector.Z;
    }

    /// <summary>
    ///     Lengthes the squared.
    /// </summary>
    /// <param name="vector">The vector.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DoubleOrSingle LengthSquared(ref Vector vector) {
        return vector.X * vector.X + vector.Y * vector.Y;
    }

    /// <summary>
    /// </summary>
    /// <param name="vector"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DoubleOrSingle Length(ref Vector3D vector) {
        return (DoubleOrSingle)Math.Sqrt(LengthSquared(ref vector));
    }

    /// <summary>
    /// </summary>
    /// <param name="vector"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3D ToVector3D(Point3D vector) {
        return new Vector3D(vector.X, vector.Y, vector.Z);
    }
    /// <summary>
    ///     Finds the intersection between the plane and a line.
    /// </summary>
    /// <param name="plane">
    ///     The plane.
    /// </param>
    /// <param name="la">
    ///     The first point defining the line.
    /// </param>
    /// <param name="lb">
    ///     The second point defining the line.
    /// </param>
    /// <returns>
    ///     The intersection point.
    /// </returns>
    public static Point3D? LineIntersection(this Plane plane, Point3D la, Point3D lb) {
        // https://graphics.stanford.edu/~mdfisher/Code/Engine/Plane.cpp.html
        var diff = la - lb;
        var d = SilkMath.Dot(diff, plane.Normal);
        if (d == 0) return null;
        var u = (SilkMath.Dot(la, plane.Normal) + plane.D) / d;
        return la + u * (lb - la);
    }
}

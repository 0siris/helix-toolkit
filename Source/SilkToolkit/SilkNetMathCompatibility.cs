using System;
using System.Numerics;
using System.Runtime.CompilerServices;

internal static class SilkMath {
    public const int Vector2SizeInBytes = sizeof(float) * 2;
    public const int Vector3SizeInBytes = sizeof(float) * 3;
    public const int Vector4SizeInBytes = sizeof(float) * 4;
    public const int MatrixSizeInBytes = sizeof(float) * 16;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Multiply(Vector2 left, Vector2 right) => left * right;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Normalize(Vector2 value) {
        var length = value.Length;
        return length > 0 ? value / length : value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Normalize(Vector3 value) {
        var length = value.Length;
        return length > 0 ? value / length : value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 Normalize(Vector4 value) {
        var length = value.Length;
        return length > 0 ? value / length : value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Dot(Vector2 left, Vector2 right) => left.X * right.X + left.Y * right.Y;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Dot(Vector3 left, Vector3 right) => left.X * right.X + left.Y * right.Y + left.Z * right.Z;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Dot(Vector4 left, Vector4 right) => left.X * right.X + left.Y * right.Y + left.Z * right.Z + left.W * right.W;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Dot(ref Vector3 left, ref Vector3 right, out float result) {
        result = Dot(left, right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Cross(Vector3 left, Vector3 right) => new(left.Y * right.Z - left.Z * right.Y,
        left.Z * right.X - left.X * right.Z,
        left.X * right.Y - left.Y * right.X);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Min(Vector3 left, Vector3 right) => new(Math.Min(left.X, right.X),
        Math.Min(left.Y, right.Y),
        Math.Min(left.Z, right.Z));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Max(Vector3 left, Vector3 right) => new(Math.Max(left.X, right.X),
        Math.Max(left.Y, right.Y),
        Math.Max(left.Z, right.Z));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Clamp(Vector3 value, Vector3 minimum, Vector3 maximum) => Min(Max(value, minimum), maximum);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Lerp(Vector3 start, Vector3 end, float amount) => start + (end - start) * amount;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Subtract(ref Vector3 left, ref Vector3 right, out Vector3 result) {
        result = left - right;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Min(ref Vector3 left, ref Vector3 right, out Vector3 result) {
        result = Min(left, right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Max(ref Vector3 left, ref Vector3 right, out Vector3 result) {
        result = Max(left, right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add(ref Vector3 left, ref Vector3 right, out Vector3 result) {
        result = left + right;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float DistanceSquared(Vector3 left, Vector3 right) => (left - right).LengthSquared;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DistanceSquared(ref Vector3 left, ref Vector3 right, out float result) {
        result = DistanceSquared(left, right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 TransformCoordinate(Vector3 value, Matrix matrix) {
        var x = value.X * matrix.M11 + value.Y * matrix.M21 + value.Z * matrix.M31 + matrix.M41;
        var y = value.X * matrix.M12 + value.Y * matrix.M22 + value.Z * matrix.M32 + matrix.M42;
        var z = value.X * matrix.M13 + value.Y * matrix.M23 + value.Z * matrix.M33 + matrix.M43;
        var w = value.X * matrix.M14 + value.Y * matrix.M24 + value.Z * matrix.M34 + matrix.M44;

        return w != 0f && w != 1f
                   ? new Vector3(x / w, y / w, z / w)
                   : new Vector3(x, y, z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TransformCoordinate(ref Vector3 value, ref Matrix matrix, out Vector3 result) {
        result = TransformCoordinate(value, matrix);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 TransformNormal(Vector3 value, Matrix matrix) => new(value.X * matrix.M11 + value.Y * matrix.M21 + value.Z * matrix.M31,
        value.X * matrix.M12 + value.Y * matrix.M22 + value.Z * matrix.M32,
        value.X * matrix.M13 + value.Y * matrix.M23 + value.Z * matrix.M33);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 Transform(Vector3 value, Matrix matrix) => new(value.X * matrix.M11 + value.Y * matrix.M21 + value.Z * matrix.M31 + matrix.M41,
        value.X * matrix.M12 + value.Y * matrix.M22 + value.Z * matrix.M32 + matrix.M42,
        value.X * matrix.M13 + value.Y * matrix.M23 + value.Z * matrix.M33 + matrix.M43,
        value.X * matrix.M14 + value.Y * matrix.M24 + value.Z * matrix.M34 + matrix.M44);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Transform(ref Vector3 value, ref Matrix matrix, out Vector4 result) {
        result = Transform(value, matrix);
    }

    public static Matrix Translation(Vector3 value) {
        var result = Matrix.Identity;
        result.M41 = value.X;
        result.M42 = value.Y;
        result.M43 = value.Z;
        return result;
    }

    public static Matrix Scaling(Vector3 value) => Scaling(value.X, value.Y, value.Z);

    public static Matrix Scaling(float x, float y, float z) => new(x,
        0,
        0,
        0,
        0,
        y,
        0,
        0,
        0,
        0,
        z,
        0,
        0,
        0,
        0,
        1);

    public static Matrix RotationQuaternion(Quaternion value) => FromNumerics(Matrix4x4.CreateFromQuaternion(
        new System.Numerics.Quaternion(value.X, value.Y, value.Z, value.W)));

    public static Quaternion QuaternionRotationAxis(Vector3 axis, float angle) {
        axis = Normalize(axis);
        var halfAngle = angle * 0.5f;
        var scale = (float)Math.Sin(halfAngle);
        return new Quaternion(axis.X * scale, axis.Y * scale, axis.Z * scale, (float)Math.Cos(halfAngle));
    }

    public static float QuaternionAngle(Quaternion value) {
        var w = Math.Max(-1f, Math.Min(1f, value.W));
        return 2f * (float)Math.Acos(w);
    }

    public static Matrix RotationAxis(Vector3 axis, float angle) {
        axis = Normalize(axis);
        return FromNumerics(Matrix4x4.CreateFromAxisAngle(new System.Numerics.Vector3(axis.X, axis.Y, axis.Z),
                                                          angle));
    }

    public static Matrix RotationX(float angle) => FromNumerics(Matrix4x4.CreateRotationX(angle));

    public static Matrix RotationY(float angle) => FromNumerics(Matrix4x4.CreateRotationY(angle));

    public static Matrix RotationZ(float angle) => FromNumerics(Matrix4x4.CreateRotationZ(angle));

    public static Matrix LookAtLH(Vector3 eye, Vector3 target, Vector3 up) {
        var zAxis = Normalize(target - eye);
        var xAxis = Normalize(Cross(up, zAxis));
        var yAxis = Cross(zAxis, xAxis);
        return CreateLookAt(eye, xAxis, yAxis, zAxis);
    }

    public static Matrix LookAtRH(Vector3 eye, Vector3 target, Vector3 up) {
        var zAxis = Normalize(eye - target);
        var xAxis = Normalize(Cross(up, zAxis));
        var yAxis = Cross(zAxis, xAxis);
        return CreateLookAt(eye, xAxis, yAxis, zAxis);
    }

    public static Matrix PerspectiveFovLH(float fieldOfView, float aspectRatio, float nearPlane, float farPlane) {
        var yScale = 1f / (float)Math.Tan(fieldOfView * 0.5f);
        var xScale = yScale / aspectRatio;
        return new Matrix(xScale,
                          0,
                          0,
                          0,
                          0,
                          yScale,
                          0,
                          0,
                          0,
                          0,
                          farPlane / (farPlane - nearPlane),
                          1,
                          0,
                          0,
                          -nearPlane * farPlane / (farPlane - nearPlane),
                          0);
    }

    public static Matrix PerspectiveFovRH(float fieldOfView, float aspectRatio, float nearPlane, float farPlane) {
        var yScale = 1f / (float)Math.Tan(fieldOfView * 0.5f);
        var xScale = yScale / aspectRatio;
        return new Matrix(xScale,
                          0,
                          0,
                          0,
                          0,
                          yScale,
                          0,
                          0,
                          0,
                          0,
                          farPlane / (nearPlane - farPlane),
                          -1,
                          0,
                          0,
                          nearPlane * farPlane / (nearPlane - farPlane),
                          0);
    }

    public static Matrix OrthoLH(float width, float height, float nearPlane, float farPlane) => new(2f / width,
        0,
        0,
        0,
        0,
        2f / height,
        0,
        0,
        0,
        0,
        1f / (farPlane - nearPlane),
        0,
        0,
        0,
        -nearPlane / (farPlane - nearPlane),
        1);

    public static Matrix OrthoRH(float width, float height, float nearPlane, float farPlane) => new(2f / width,
        0,
        0,
        0,
        0,
        2f / height,
        0,
        0,
        0,
        0,
        1f / (nearPlane - farPlane),
        0,
        0,
        0,
        nearPlane / (nearPlane - farPlane),
        1);

    public static bool Invert(Matrix value, out Matrix result) {
        var source = new Matrix4x4(value.M11,
                                   value.M12,
                                   value.M13,
                                   value.M14,
                                   value.M21,
                                   value.M22,
                                   value.M23,
                                   value.M24,
                                   value.M31,
                                   value.M32,
                                   value.M33,
                                   value.M34,
                                   value.M41,
                                   value.M42,
                                   value.M43,
                                   value.M44);

        if (!Matrix4x4.Invert(source, out var inverted)) {
            result = Matrix.Identity;
            return false;
        }

        result = new Matrix(inverted.M11,
                            inverted.M12,
                            inverted.M13,
                            inverted.M14,
                            inverted.M21,
                            inverted.M22,
                            inverted.M23,
                            inverted.M24,
                            inverted.M31,
                            inverted.M32,
                            inverted.M33,
                            inverted.M34,
                            inverted.M41,
                            inverted.M42,
                            inverted.M43,
                            inverted.M44);
        return true;
    }

    private static Matrix CreateLookAt(Vector3 eye, Vector3 xAxis, Vector3 yAxis, Vector3 zAxis) => new(xAxis.X,
        yAxis.X,
        zAxis.X,
        0,
        xAxis.Y,
        yAxis.Y,
        zAxis.Y,
        0,
        xAxis.Z,
        yAxis.Z,
        zAxis.Z,
        0,
        -Dot(xAxis, eye),
        -Dot(yAxis, eye),
        -Dot(zAxis, eye),
        1);

    private static Matrix FromNumerics(Matrix4x4 source) => new(source.M11,
        source.M12,
        source.M13,
        source.M14,
        source.M21,
        source.M22,
        source.M23,
        source.M24,
        source.M31,
        source.M32,
        source.M33,
        source.M34,
        source.M41,
        source.M42,
        source.M43,
        source.M44);
}

internal static class SilkNetMathExtensions {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float NextFloat(this Random random, float minimum, float maximum) => minimum + (float)random.NextDouble() * (maximum - minimum);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 NextVector3(this Random random, Vector3 minimum, Vector3 maximum) => new(random.NextFloat(minimum.X, maximum.X),
        random.NextFloat(minimum.Y, maximum.Y),
        random.NextFloat(minimum.Z, maximum.Z));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Normalize(this ref Vector2 value) {
        value = SilkMath.Normalize(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Normalize(this ref Vector3 value) {
        value = SilkMath.Normalize(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Normalize(this ref Vector4 value) {
        value = SilkMath.Normalize(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float LengthSquared(this Vector2 value) => value.LengthSquared;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float LengthSquared(this Vector3 value) => value.LengthSquared;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float LengthSquared(this Vector4 value) => value.LengthSquared;

    public static void Invert(this ref Matrix value) {
        SilkMath.Invert(value, out value);
    }

    public static bool Decompose(
        this Matrix value,
        out Vector3 scale,
        out Quaternion rotation,
        out Vector3 translation
    ) {
        var source = new Matrix4x4(value.M11,
                                   value.M12,
                                   value.M13,
                                   value.M14,
                                   value.M21,
                                   value.M22,
                                   value.M23,
                                   value.M24,
                                   value.M31,
                                   value.M32,
                                   value.M33,
                                   value.M34,
                                   value.M41,
                                   value.M42,
                                   value.M43,
                                   value.M44);

        var success = Matrix4x4.Decompose(source,
                                          out var numericsScale,
                                          out var numericsRotation,
                                          out var numericsTranslation);
        scale = new Vector3(numericsScale.X, numericsScale.Y, numericsScale.Z);
        rotation = new Quaternion(numericsRotation.X, numericsRotation.Y, numericsRotation.Z, numericsRotation.W);
        translation = new Vector3(numericsTranslation.X, numericsTranslation.Y, numericsTranslation.Z);
        return success;
    }
}

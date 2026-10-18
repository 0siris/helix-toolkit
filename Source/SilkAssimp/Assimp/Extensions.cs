using HelixToolkit.SharpDX.Core;
using SharpAssimp;
using System.Numerics;
using HelixToolkit.SharpDX.Core.Model;
using Metadata = HelixToolkit.SharpDX.Core.Model.Metadata;
using MetaDataType = HelixToolkit.SharpDX.Core.Model.MetaDataType;

namespace HelixToolkit.SharpDX.Core.Assimp;

/// <summary>
/// </summary>
public static class Extensions {
    /// <summary>
    ///     To the sharp dx matrix. Already transposed after this function
    /// </summary>
    /// <param name="m">The m.</param>
    /// <param name="isColumnMajor"></param>
    /// <returns></returns>
    public static Matrix ToSharpDXMatrix(this Matrix4x4 m, bool isColumnMajor) => isColumnMajor
        ? new Matrix(m.M11,
            m.M21,
            m.M31,
            m.M41,
            m.M12,
            m.M22,
            m.M32,
            m.M42,
            m.M13,
            m.M23,
            m.M33,
            m.M43,
            m.M14,
            m.M24,
            m.M34,
            m.M44)
        : new Matrix(m.M11,
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

    /// <summary>
    ///     To the assimp matrix. Already transposed after this function
    /// </summary>
    /// <param name="m">The m.</param>
    /// <param name="toColumnMajor"></param>
    /// <returns></returns>
    public static Matrix4x4 ToAssimpMatrix(this Matrix m, bool toColumnMajor) {
        var matrix = new Matrix4x4(m.M11,
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
        if (toColumnMajor) matrix = Matrix4x4.Transpose(matrix);
        return matrix;
    }

    /// <summary>
    ///     To the sharp dx vector3.
    /// </summary>
    /// <param name="v">The v.</param>
    /// <returns></returns>
    public static Vector3 ToSharpDXVector3(this System.Numerics.Vector3 v) => new(v.X, v.Y, v.Z);

    /// <summary>
    ///     To the assimp vector3d.
    /// </summary>
    /// <param name="v">The v.</param>
    /// <returns></returns>
    public static System.Numerics.Vector3 ToAssimpVector3D(this Vector3 v) => new(v.X, v.Y, v.Z);

    /// <summary>
    ///     To the sharp dx vector2.
    /// </summary>
    /// <param name="v">The v.</param>
    /// <returns></returns>
    public static Vector2 ToSharpDXVector2(this System.Numerics.Vector2 v) => new(v.X, v.Y);

    /// <summary>
    ///     To the assimp vector2d.
    /// </summary>
    /// <param name="v">The v.</param>
    /// <returns></returns>
    public static System.Numerics.Vector2 ToAssimpVector2D(this Vector2 v) => new(v.X, v.Y);

    /// <summary>
    ///     To the assimp vector3d.
    /// </summary>
    /// <param name="v">The v.</param>
    /// <returns></returns>
    public static System.Numerics.Vector3 ToAssimpVector3D(this Vector2 v) => new(v.X, v.Y, 0);

    /// <summary>
    ///     To the sharp dx vector2.
    /// </summary>
    /// <param name="v">The v.</param>
    /// <returns></returns>
    public static Vector2 ToSharpDXVector2(this System.Numerics.Vector3 v) => new(v.X, v.Y);

    /// <summary>
    ///     To the sharp dx color4.
    /// </summary>
    /// <param name="v">The v.</param>
    /// <returns></returns>
    public static Color4 ToSharpDXColor4(this Vector4 v) => new(v.X, v.Y, v.Z, v.W);

    /// <summary>
    ///     To the assimp color4d.
    /// </summary>
    /// <param name="v">The v.</param>
    /// <param name="alpha"></param>
    /// <returns></returns>
    public static Vector4 ToAssimpColor4D(this Color4 v, float alpha = 1f) => new(v.X, v.Y, v.Z, alpha);

    /// <summary>
    ///     To the sharp dx quaternion.
    /// </summary>
    /// <param name="q">The q.</param>
    /// <returns></returns>
    public static Quaternion ToSharpDXQuaternion(this System.Numerics.Quaternion q) => new(q.X, q.Y, q.Z, q.W);

    /// <summary>
    ///     To the assimp quaternion.
    /// </summary>
    /// <param name="q">The q.</param>
    /// <returns></returns>
    public static System.Numerics.Quaternion ToAssimpQuaternion(this Quaternion q) => new(q.X, q.Y, q.Z, q.W);

    /// <summary>
    ///     To the Helix UVTransform.
    /// </summary>
    /// <param name="transform">The transform.</param>
    /// <returns></returns>
    public static UVTransform ToHelixUvTransform(this SharpAssimp.UVTransform transform) => new(transform.Rotation,
        transform.Scaling.ToSharpDXVector2(),
        transform.Translation.ToSharpDXVector2());

    /// <summary>
    ///     To the type of the helix metadata.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException">Type {type} is not supported.</exception>
    public static MetaDataType ToHelixMetadataType(this global::SharpAssimp.MetaDataType type) {
        switch (type) {
            case global::SharpAssimp.MetaDataType.Bool:
                return MetaDataType.Bool;
            case global::SharpAssimp.MetaDataType.Double:
                return MetaDataType.Double;
            case global::SharpAssimp.MetaDataType.Float:
                return MetaDataType.Float;
            case global::SharpAssimp.MetaDataType.Int32:
                return MetaDataType.Int32;
            case global::SharpAssimp.MetaDataType.String:
                return MetaDataType.String;
            case global::SharpAssimp.MetaDataType.UInt64:
                return MetaDataType.UInt64;
            case global::SharpAssimp.MetaDataType.Vector3:
                return MetaDataType.Vector3D;
            default:
                throw new NotSupportedException($"Type {type} is not supported.");
        }
    }

    /// <summary>
    ///     To the type of the assimp metadata.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException">Type {type} is not supported.</exception>
    public static global::SharpAssimp.MetaDataType ToAssimpMetadataType(this MetaDataType type) {
        switch (type) {
            case MetaDataType.Bool:
                return global::SharpAssimp.MetaDataType.Bool;
            case MetaDataType.Double:
                return global::SharpAssimp.MetaDataType.Double;
            case MetaDataType.Float:
                return global::SharpAssimp.MetaDataType.Float;
            case MetaDataType.Int32:
                return global::SharpAssimp.MetaDataType.Int32;
            case MetaDataType.String:
                return global::SharpAssimp.MetaDataType.String;
            case MetaDataType.UInt64:
                return global::SharpAssimp.MetaDataType.UInt64;
            case MetaDataType.Vector3D:
                return global::SharpAssimp.MetaDataType.Vector3;
            default:
                throw new NotSupportedException($"Type {type} is not supported.");
        }
    }

    /// <summary>
    ///     To the helix metadata.
    /// </summary>
    /// <param name="m">The m.</param>
    /// <returns></returns>
    public static IEnumerable<KeyValuePair<string, Metadata.Entry>> ToHelixMetadata(
        this global::SharpAssimp.Metadata m
    ) {
        foreach (var d in m)
            yield return new KeyValuePair<string, Metadata.Entry>(d.Key,
                                                                  new Metadata.Entry(
                                                                      d.Value.DataType.ToHelixMetadataType(),
                                                                      d.Value.Data));
    }

    /// <summary>
    ///     To the assimp metadata.
    /// </summary>
    /// <param name="m">The m.</param>
    /// <returns></returns>
    public static IEnumerable<KeyValuePair<string, global::SharpAssimp.Metadata.Entry>> ToAssimpMetadata(
        this Metadata m
    ) {
        foreach (var d in m)
            yield return new KeyValuePair<string, global::SharpAssimp.Metadata.Entry>(d.Key,
                new global::SharpAssimp.Metadata.Entry(d.Value.DataType.ToAssimpMetadataType(), d.Value.Data));
    }
}

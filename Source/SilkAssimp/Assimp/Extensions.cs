using Assimp;
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
    public static Matrix ToSharpDXMatrix(this Matrix4x4 m, bool isColumnMajor) {
        return isColumnMajor
                   ? new Matrix(m.A1,
                                m.B1,
                                m.C1,
                                m.D1,
                                m.A2,
                                m.B2,
                                m.C2,
                                m.D2,
                                m.A3,
                                m.B3,
                                m.C3,
                                m.D3,
                                m.A4,
                                m.B4,
                                m.C4,
                                m.D4)
                   : new Matrix(m.A1,
                                m.A2,
                                m.A3,
                                m.A4,
                                m.B1,
                                m.B2,
                                m.B3,
                                m.B4,
                                m.C1,
                                m.C2,
                                m.C3,
                                m.C4,
                                m.D1,
                                m.D2,
                                m.D3,
                                m.D4);
    }

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
        if (toColumnMajor) matrix.Transpose();
        return matrix;
    }

    /// <summary>
    ///     To the sharp dx vector3.
    /// </summary>
    /// <param name="v">The v.</param>
    /// <returns></returns>
    public static Vector3 ToSharpDXVector3(this Vector3D v) {
        return new Vector3(v.X, v.Y, v.Z);
    }

    /// <summary>
    ///     To the assimp vector3d.
    /// </summary>
    /// <param name="v">The v.</param>
    /// <returns></returns>
    public static Vector3D ToAssimpVector3D(this Vector3 v) {
        return new Vector3D(v.X, v.Y, v.Z);
    }

    /// <summary>
    ///     To the sharp dx vector2.
    /// </summary>
    /// <param name="v">The v.</param>
    /// <returns></returns>
    public static Vector2 ToSharpDXVector2(this Vector2D v) {
        return new Vector2(v.X, v.Y);
    }

    /// <summary>
    ///     To the assimp vector2d.
    /// </summary>
    /// <param name="v">The v.</param>
    /// <returns></returns>
    public static Vector2D ToAssimpVector2D(this Vector2 v) {
        return new Vector2D(v.X, v.Y);
    }

    /// <summary>
    ///     To the assimp vector3d.
    /// </summary>
    /// <param name="v">The v.</param>
    /// <returns></returns>
    public static Vector3D ToAssimpVector3D(this Vector2 v) {
        return new Vector3D(v.X, v.Y, 0);
    }

    /// <summary>
    ///     To the sharp dx vector2.
    /// </summary>
    /// <param name="v">The v.</param>
    /// <returns></returns>
    public static Vector2 ToSharpDXVector2(this Vector3D v) {
        return new Vector2(v.X, v.Y);
    }

    /// <summary>
    ///     To the sharp dx color4.
    /// </summary>
    /// <param name="v">The v.</param>
    /// <returns></returns>
    public static Color4 ToSharpDXColor4(this Color4D v) {
        return new Color4(v.R, v.G, v.B, v.A);
    }

    /// <summary>
    ///     To the assimp color4d.
    /// </summary>
    /// <param name="v">The v.</param>
    /// <param name="alpha"></param>
    /// <returns></returns>
    public static Color4D ToAssimpColor4D(this Color4 v, float alpha = 1f) {
        return new Color4D(v.X, v.Y, v.Z, alpha);
    }

    /// <summary>
    ///     To the sharp dx quaternion.
    /// </summary>
    /// <param name="q">The q.</param>
    /// <returns></returns>
    public static Quaternion ToSharpDXQuaternion(this global::Assimp.Quaternion q) {
        return new Quaternion(q.X, q.Y, q.Z, q.W);
    }

    /// <summary>
    ///     To the assimp quaternion.
    /// </summary>
    /// <param name="q">The q.</param>
    /// <returns></returns>
    public static global::Assimp.Quaternion ToAssimpQuaternion(this Quaternion q) {
        return new global::Assimp.Quaternion(q.W, q.X, q.Y, q.Z);
    }

    /// <summary>
    ///     To the Helix UVTransform.
    /// </summary>
    /// <param name="transform">The transform.</param>
    /// <returns></returns>
    public static UVTransform ToHelixUVTransform(this global::Assimp.UVTransform transform) {
        return new UVTransform(transform.Rotation,
                               transform.Scaling.ToSharpDXVector2(),
                               transform.Translation.ToSharpDXVector2());
    }

    /// <summary>
    ///     To the type of the helix metadata.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException">Type {type} is not supported.</exception>
    public static MetaDataType ToHelixMetadataType(this global::Assimp.MetaDataType type) {
        switch (type) {
            case global::Assimp.MetaDataType.Bool:
                return MetaDataType.Bool;
            case global::Assimp.MetaDataType.Double:
                return MetaDataType.Double;
            case global::Assimp.MetaDataType.Float:
                return MetaDataType.Float;
            case global::Assimp.MetaDataType.Int32:
                return MetaDataType.Int32;
            case global::Assimp.MetaDataType.String:
                return MetaDataType.String;
            case global::Assimp.MetaDataType.UInt64:
                return MetaDataType.UInt64;
            case global::Assimp.MetaDataType.Vector3D:
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
    public static global::Assimp.MetaDataType ToAssimpMetadataType(this MetaDataType type) {
        switch (type) {
            case MetaDataType.Bool:
                return global::Assimp.MetaDataType.Bool;
            case MetaDataType.Double:
                return global::Assimp.MetaDataType.Double;
            case MetaDataType.Float:
                return global::Assimp.MetaDataType.Float;
            case MetaDataType.Int32:
                return global::Assimp.MetaDataType.Int32;
            case MetaDataType.String:
                return global::Assimp.MetaDataType.String;
            case MetaDataType.UInt64:
                return global::Assimp.MetaDataType.UInt64;
            case MetaDataType.Vector3D:
                return global::Assimp.MetaDataType.Vector3D;
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
        this global::Assimp.Metadata m
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
    public static IEnumerable<KeyValuePair<string, global::Assimp.Metadata.Entry>> ToAssimpMetadata(
        this Metadata m
    ) {
        foreach (var d in m)
            yield return new KeyValuePair<string, global::Assimp.Metadata.Entry>(d.Key,
                new global::Assimp.Metadata.Entry(d.Value.DataType.ToAssimpMetadataType(), d.Value.Data));
    }
}

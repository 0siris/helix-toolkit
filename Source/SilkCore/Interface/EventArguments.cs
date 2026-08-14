using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core;

/// <summary>
/// </summary>
/// <remarks>
///     Initializes a new instance of the <see cref="Texture2DArgs" /> class.
/// </remarks>
/// <param name="texture">The texture.</param>
public sealed class Texture2DArgs(ShaderResourceViewProxy texture) : EventArgs {
    /// <summary>
    ///     The texture
    /// </summary>
    public ShaderResourceViewProxy Texture => texture;

    /// <summary>
    ///     Performs an implicit conversion from <see cref="Texture2DArgs" /> to <see cref="ShaderResourceViewProxy" />.
    /// </summary>
    /// <param name="args">The arguments.</param>
    /// <returns>
    ///     The result of the conversion.
    /// </returns>
    public static implicit operator ShaderResourceViewProxy(Texture2DArgs args) 
        => args.Texture;
}

/// <summary>
/// </summary>
/// <remarks>
///     Initializes a new instance of the <see cref="OctreeArgs" /> class.
/// </remarks>
/// <param name="octree">The octree.</param>
public sealed class OctreeArgs(IOctreeBasic? octree) : EventArgs {
    /// <summary>
    ///     The octree
    /// </summary>
    public IOctreeBasic? Octree => octree;
}

/// <summary>
/// </summary>
public sealed class TransformArgs : EventArgs {
    /// <summary>
    ///     The transform
    /// </summary>
    public readonly Matrix Transform;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TransformArgs" /> class.
    /// </summary>
    /// <param name="transform">The transform.</param>
    public TransformArgs(Matrix transform) => Transform = transform;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TransformArgs" /> class.
    /// </summary>
    /// <param name="transform">The transform.</param>
    public TransformArgs(ref Matrix transform) => Transform = transform;

    /// <summary>
    ///     Performs an implicit conversion from <see cref="TransformArgs" /> to <see cref="Matrix" />.
    /// </summary>
    /// <param name="args">The arguments.</param>
    /// <returns>
    ///     The result of the conversion.
    /// </returns>
    public static implicit operator Matrix(TransformArgs args) => args.Transform;
}

/// <summary>
/// </summary>
public sealed class Transform2DArgs : EventArgs {
    /// <summary>
    ///     The transform
    /// </summary>
    public readonly Matrix3X2 Transform;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Transform2DArgs" /> class.
    /// </summary>
    /// <param name="transform">The transform.</param>
    public Transform2DArgs(Matrix3X2 transform) => Transform = transform;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Transform2DArgs" /> class.
    /// </summary>
    /// <param name="transform">The transform.</param>
    public Transform2DArgs(ref Matrix3X2 transform) => Transform = transform;

    /// <summary>
    ///     Performs an implicit conversion from <see cref="Transform2DArgs" /> to <see cref="Matrix3X2" />.
    /// </summary>
    /// <param name="args">The arguments.</param>
    /// <returns>
    ///     The result of the conversion.
    /// </returns>
    public static implicit operator Matrix3X2(Transform2DArgs args) => args.Transform;
}

/// <summary>
/// </summary>
/// <remarks>
///     Initializes a new instance of the <see cref="BoolArgs" /> class.
/// </remarks>
/// <param name="value">if set to <c>true</c> [value].</param>
public sealed class BoolArgs(bool value) : EventArgs {
    /// <summary>
    ///     The true arguments
    /// </summary>
    public static readonly BoolArgs TrueArgs = new(true);

    /// <summary>
    ///     The false arguments
    /// </summary>
    public static readonly BoolArgs FalseArgs = new(false);

    /// <summary>
    ///     The value
    /// </summary>
    public readonly bool Value = value;
}

/// <summary>
/// </summary>
/// <remarks>
///     Initializes a new instance of the <see cref="StringArgs" /> class.
/// </remarks>
/// <param name="value">The value.</param>
public sealed class StringArgs(string value) : EventArgs {
    /// <summary>
    ///     The value
    /// </summary>
    public string Value => value;
}

public sealed class FrameStatisticsArg(double avgValue, double avgFrequency) : EventArgs {
    public double AverageFrequency => avgFrequency;
    public double AverageValue => avgValue;
}

/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Interface;

namespace HelixToolkit.SharpDX.Core.Model;
/// <summary>
///     Vertex Normal Material
/// </summary>
public sealed class NormalMaterialCore : MaterialCore {
    public static readonly NormalMaterialCore Core = new();
}

/// <summary>
///     Vertex Color Material
/// </summary>
public sealed class ColorMaterialCore : MaterialCore {
    public static readonly ColorMaterialCore Core = new();
}

/// <summary>
///     Vertex Position Material
/// </summary>
public sealed class PositionMaterialCore : MaterialCore {
    public static readonly PositionMaterialCore Core = new();
}

/// <summary>
///     Vertex Normal Vector Material
/// </summary>
public sealed class NormalVectorMaterialCore : MaterialCore {
    public static readonly NormalVectorMaterialCore Core = new();
}

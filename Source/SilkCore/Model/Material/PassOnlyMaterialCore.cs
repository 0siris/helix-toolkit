/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Material.Variables;
using HelixToolkit.SharpDX.Core.ShaderManager;

namespace HelixToolkit.SharpDX.Core.Model.Material;
/// <summary>
///     Vertex Normal Material
/// </summary>
public sealed class NormalMaterialCore : MaterialCore {
    public static readonly NormalMaterialCore Core = new();

    public override MaterialVariable CreateMaterialVariables(
        IEffectsManager manager,
        IRenderTechnique technique
    )
        => new PassOnlyMaterialVariable(DefaultPassNames.Normals, technique);
}

/// <summary>
///     Vertex Color Material
/// </summary>
public sealed class ColorMaterialCore : MaterialCore {
    public static readonly ColorMaterialCore Core = new();

    public override MaterialVariable CreateMaterialVariables(
        IEffectsManager manager,
        IRenderTechnique technique
    )
        => new PassOnlyMaterialVariable(DefaultPassNames.Colors, technique);
}

/// <summary>
///     Vertex Position Material
/// </summary>
public sealed class PositionMaterialCore : MaterialCore {
    public static readonly PositionMaterialCore Core = new();

    public override MaterialVariable CreateMaterialVariables(
        IEffectsManager manager,
        IRenderTechnique technique
    )
        => new PassOnlyMaterialVariable(DefaultPassNames.Positions, technique);
}

/// <summary>
///     Vertex Normal Vector Material
/// </summary>
public sealed class NormalVectorMaterialCore : MaterialCore {
    public static readonly NormalVectorMaterialCore Core = new();

    public override MaterialVariable CreateMaterialVariables(
        IEffectsManager manager,
        IRenderTechnique technique
    )
        => new PassOnlyMaterialVariable(DefaultPassNames.NormalVector, technique);
}

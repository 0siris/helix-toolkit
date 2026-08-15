/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core.DefaultShaders;
/// <summary>
/// </summary>
public static class DefaultGsShaderByteCodes {
    /// <summary>
    /// </summary>
    public static string GsPoint { get; } = "gsPoint";

    /// <summary>
    /// </summary>
    public static string GsLine { get; } = "gsLine";

    /// <summary>
    ///     Gets the gs line arrow head.
    /// </summary>
    /// <value>
    ///     The gs line arrow head.
    /// </value>
    public static string GsLineArrowHead { get; } = "gsLineArrowHead";

    /// <summary>
    ///     Gets the gs line arrow tail.
    /// </summary>
    /// <value>
    ///     The gs line arrow tail.
    /// </value>
    public static string GsLineArrowHeadTail { get; } = "gsLineArrowHeadTail";

    /// <summary>
    /// </summary>
    public static string GsBillboard { get; } = "gsBillboard";

    /// <summary>
    /// </summary>
    public static string GsParticle { get; } = "gsParticle";

    /// <summary>
    ///     Gets the gs mesh normal vector.
    /// </summary>
    /// <value>
    ///     The gs mesh normal vector.
    /// </value>
    public static string GsMeshNormalVector { get; } = "gsMeshNormalVector";

    public static string GsMeshBoneSkinnedOut { get; } = "gsMeshSkinnedOut";
}


/// <summary>
///     Default Geometry Shaders
/// </summary>
public static class DefaultGsShaderDescriptions {
    /// <summary>
    /// </summary>
    public static readonly ShaderDescription GsPoint = new(nameof(GsPoint),
                                                           ShaderStage.Geometry,
                                                           new ShaderReflector(),
                                                           DefaultGsShaderByteCodes.GsPoint);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription GsLine = new(nameof(GsLine),
                                                          ShaderStage.Geometry,
                                                          new ShaderReflector(),
                                                          DefaultGsShaderByteCodes.GsLine);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription GsLineArrowHead = new(nameof(GsLineArrowHead),
                                                                   ShaderStage.Geometry,
                                                                   new ShaderReflector(),
                                                                   DefaultGsShaderByteCodes.GsLineArrowHead);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription GsLineArrowHeadTail = new(nameof(GsLineArrowHeadTail),
                                                                       ShaderStage.Geometry,
                                                                       new ShaderReflector(),
                                                                       DefaultGsShaderByteCodes
                                                                           .GsLineArrowHeadTail);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription GsBillboard = new(nameof(GsBillboard),
                                                               ShaderStage.Geometry,
                                                               new ShaderReflector(),
                                                               DefaultGsShaderByteCodes.GsBillboard);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription GsParticle = new(nameof(GsParticle),
                                                              ShaderStage.Geometry,
                                                              new ShaderReflector(),
                                                              DefaultGsShaderByteCodes.GsParticle);

    /// <summary>
    ///     The gs mesh normal vector
    /// </summary>
    public static readonly ShaderDescription GsMeshNormalVector = new(nameof(GsMeshNormalVector),
                                                                      ShaderStage.Geometry,
                                                                      new ShaderReflector(),
                                                                      DefaultGsShaderByteCodes
                                                                          .GsMeshNormalVector);

    /// <summary>
    ///     The gs mesh bone skinned out
    /// </summary>
    public static readonly ShaderDescription GsMeshBoneSkinnedOut = new(nameof(GsMeshBoneSkinnedOut),
        ShaderStage.Geometry,
        new ShaderReflector(),
        DefaultGsShaderByteCodes.GsMeshBoneSkinnedOut) {
        IsGsStreamOut = true,
        GssoElement = [
            new StreamOutputElement(0, "POSITION", 0, 0, 4, 0),
            new StreamOutputElement(0, "NORMAL", 0, 0, 3, 0),
            new StreamOutputElement(0, "TANGENT", 0, 0, 3, 0),
            new StreamOutputElement(0, "BINORMAL", 0, 0, 3, 0)
        ],
        GssoStrides = [
            DefaultVertex.SizeInBytes
        ]
    };
}

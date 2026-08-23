/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core.DefaultShaders;
/// <summary>
/// </summary>
public static class DefaultVsShaderByteCodes {
    /// <summary>
    /// </summary>
    public static string VsMeshDefault { get; } = "vsMeshDefault";

    /// <summary>
    ///     Gets the vs mesh batched.
    /// </summary>
    /// <value>
    ///     The vs mesh batched.
    /// </value>
    public static string VsMeshBatched { get; } = "vsMeshBatched";

    /// <summary>
    /// </summary>
    public static string VsMeshTessellation { get; } = "vsMeshTessellation";

    /// <summary>
    /// </summary>
    public static string VsMeshShadow { get; } = "vsMeshShadow";

    /// <summary>
    ///     Gets the vs mesh depth.
    /// </summary>
    /// <value>
    ///     The vs mesh depth.
    /// </value>
    public static string VsMeshDepth { get; } = "vsMeshDepth";

    /// <summary>
    /// </summary>
    public static string VsMeshBatchedShadow { get; } = "vsMeshBatchedShadow";

    /// <summary>
    ///     Gets the vs mesh batched ssao.
    /// </summary>
    /// <value>
    ///     The vs mesh batched ssao.
    /// </value>
    public static string VsMeshBatchedSsao { get; } = "vsMeshBatchedSSAO";

    /// <summary>
    /// </summary>
    public static string VsMeshInstancing { get; } = "vsMeshInstancing";

    public static string VsMeshSsao { get; } = "vsMeshSSAO";

    /// <summary>
    /// </summary>
    public static string VsMeshInstancingTessellation { get; } = "vsMeshInstancingTessellation";

    public static string VsMeshBoneSkinningBasic { get; } = "vsBoneSkinningBasic";

    /// <summary>
    /// </summary>
    public static string VsPoint { get; } = "vsPoint";

    /// <summary>
    /// </summary>
    public static string VsPointShadow { get; } = "vsPointShadow";

    /// <summary>
    /// </summary>
    public static string VsBillboard { get; } = "vsBillboard";

    /// <summary>
    /// </summary>
    public static string VsBillboardInstancing { get; } = "vsBillboardInstancing";

    /// <summary>
    /// </summary>
    public static string VsMeshClipPlane { get; } = "vsMeshClipPlane";

    /// <summary>
    /// </summary>
    public static string VsMeshClipPlaneQuad { get; } = "vsMeshClipPlaneQuad";

    /// <summary>
    /// </summary>
    public static string VsParticle { get; } = "vsParticle";

    /// <summary>
    /// </summary>
    public static string VsSkybox { get; } = "vsSkybox";

    /// <summary>
    /// </summary>
    public static string VsMeshWireframe { get; } = "vsMeshWireframe";

    /// <summary>
    ///     Gets the vs mesh batched wireframe.
    /// </summary>
    /// <value>
    ///     The vs mesh batched wireframe.
    /// </value>
    public static string VsMeshBatchedWireframe { get; } = "vsMeshBatchedWireframe";

    /// <summary>
    /// </summary>
    /// <summary>
    /// </summary>
    public static string VsMeshOutlineP1 { get; } = "vsMeshOutlinePass1";


    /// <summary>
    /// </summary>
    public static string VsMeshOutlineScreenQuad { get; } = "vsMeshOutlineScreenQuad";

    /// <summary>
    ///     Gets the vs plane grid.
    /// </summary>
    /// <value>
    ///     The vs plane grid.
    /// </value>
    public static string VsPlaneGrid { get; } = "vsPlaneGrid";

    public static string VsScreenQuad { get; } = "vsScreenQuad";

    public static string VsSprite2D { get; } = "vsSprite";

    public static string VsVolume3D { get; } = "vsVolume";

    public static string Vsssao { get; } = "vsSSAO";
#if !WINDOWS_UWP
    /// <summary>
    /// </summary>
    public static string VsScreenDup { get; } = "vsScreenDup";

    /// <summary>
    /// </summary>
    public static string VsScreenDupCursor { get; } = "vsScreenDupCursor";
#endif
}


/// <summary>
/// </summary>
public static class DefaultInputLayout {
    /// <summary>
    /// </summary>
    public static readonly InputElement[] VsInput = [
        new("POSITION", 0, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 0),
        new("NORMAL", 0, Format.FormatR32G32B32Float, InputElement.AppendAligned, 0),
        new("TANGENT", 0, Format.FormatR32G32B32Float, InputElement.AppendAligned, 0),
        new("BINORMAL", 0, Format.FormatR32G32B32Float, InputElement.AppendAligned, 0),
        new("TEXCOORD", 0, Format.FormatR32G32Float, InputElement.AppendAligned, 1),
        new("COLOR", 0, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 2),
        //INSTANCING: die 4 texcoords sind die matrix, die mit jedem buffer reinwandern
        new("TEXCOORD",
                         1,
                         Format.FormatR32G32B32A32Float,
                         InputElement.AppendAligned,
                         3,
                         InputClassification.PerInstanceData,
                         1),
        new("TEXCOORD",
                         2,
                         Format.FormatR32G32B32A32Float,
                         InputElement.AppendAligned,
                         3,
                         InputClassification.PerInstanceData,
                         1),
        new("TEXCOORD",
                         3,
                         Format.FormatR32G32B32A32Float,
                         InputElement.AppendAligned,
                         3,
                         InputClassification.PerInstanceData,
                         1),
        new("TEXCOORD",
                         4,
                         Format.FormatR32G32B32A32Float,
                         InputElement.AppendAligned,
                         3,
                         InputClassification.PerInstanceData,
                         1)
    ];

    /// <summary>
    ///     Gets the input layout retained by the optimized mesh depth vertex shader.
    /// </summary>
    public static readonly InputElement[] VsInputDepth = [
        new("POSITION", 0, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 0),
        new("TEXCOORD", 1, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 3,
            InputClassification.PerInstanceData, 1),
        new("TEXCOORD", 2, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 3,
            InputClassification.PerInstanceData, 1),
        new("TEXCOORD", 3, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 3,
            InputClassification.PerInstanceData, 1),
        new("TEXCOORD", 4, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 3,
            InputClassification.PerInstanceData, 1)
    ];

    public static InputElement[] VsMeshBatchedInput = [
        new("POSITION", 0, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 0),
        new("NORMAL", 0, Format.FormatR32G32B32Float, InputElement.AppendAligned, 0),
        new("TANGENT", 0, Format.FormatR32G32B32Float, InputElement.AppendAligned, 0),
        new("BINORMAL", 0, Format.FormatR32G32B32Float, InputElement.AppendAligned, 0),
        new("TEXCOORD", 0, Format.FormatR32G32Float, InputElement.AppendAligned, 0),
        new("COLOR", 0, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 0),
        new("COLOR", 1, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 0)
    ];

    /// <summary>
    /// </summary>
    public static readonly InputElement[] VsInputInstancing = [
        new("POSITION", 0, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 0),
        new("NORMAL", 0, Format.FormatR32G32B32Float, InputElement.AppendAligned, 0),
        new("TANGENT", 0, Format.FormatR32G32B32Float, InputElement.AppendAligned, 0),
        new("BINORMAL", 0, Format.FormatR32G32B32Float, InputElement.AppendAligned, 0),
        new("TEXCOORD", 0, Format.FormatR32G32Float, InputElement.AppendAligned, 1),
        new("COLOR", 0, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 2),
        //INSTANCING: die 4 texcoords sind die matrix, die mit jedem buffer reinwandern
        new("TEXCOORD",
                         1,
                         Format.FormatR32G32B32A32Float,
                         InputElement.AppendAligned,
                         3,
                         InputClassification.PerInstanceData,
                         1),
        new("TEXCOORD",
                         2,
                         Format.FormatR32G32B32A32Float,
                         InputElement.AppendAligned,
                         3,
                         InputClassification.PerInstanceData,
                         1),
        new("TEXCOORD",
                         3,
                         Format.FormatR32G32B32A32Float,
                         InputElement.AppendAligned,
                         3,
                         InputClassification.PerInstanceData,
                         1),
        new("TEXCOORD",
                         4,
                         Format.FormatR32G32B32A32Float,
                         InputElement.AppendAligned,
                         3,
                         InputClassification.PerInstanceData,
                         1),
        new("COLOR",
                         1,
                         Format.FormatR32G32B32A32Float,
                         InputElement.AppendAligned,
                         4,
                         InputClassification.PerInstanceData,
                         1),
        new("COLOR",
                         2,
                         Format.FormatR32G32B32A32Float,
                         InputElement.AppendAligned,
                         4,
                         InputClassification.PerInstanceData,
                         1),
        new("TEXCOORD",
                         5,
                         Format.FormatR32G32Float,
                         InputElement.AppendAligned,
                         4,
                         InputClassification.PerInstanceData,
                         1)
    ];

    /// <summary>
    ///     Gets the vs input bone skinned basic.
    /// </summary>
    /// <value>
    ///     The vs input bone skinned basic.
    /// </value>
    public static readonly InputElement[] VsInputBoneSkinnedBasic = [
        new("POSITION", 0, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 0),
        new("NORMAL", 0, Format.FormatR32G32B32Float, InputElement.AppendAligned, 0),
        new("TANGENT", 0, Format.FormatR32G32B32Float, InputElement.AppendAligned, 0),
        new("BINORMAL", 0, Format.FormatR32G32B32Float, InputElement.AppendAligned, 0),
        new("BONEIDS", 0, Format.FormatR32G32B32A32Sint, InputElement.AppendAligned, 1),
        new("BONEWEIGHTS", 0, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 1)
    ];

    /// <summary>
    /// </summary>
    public static readonly InputElement[] VsInputPoint = [
        new("POSITION", 0, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 0),
        new("COLOR", 0, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 0),
        //INSTANCING: die 4 texcoords sind die matrix, die mit jedem buffer reinwandern
        new("TEXCOORD",
                         0,
                         Format.FormatR32G32B32A32Float,
                         InputElement.AppendAligned,
                         1,
                         InputClassification.PerInstanceData,
                         1),
        new("TEXCOORD",
                         1,
                         Format.FormatR32G32B32A32Float,
                         InputElement.AppendAligned,
                         1,
                         InputClassification.PerInstanceData,
                         1),
        new("TEXCOORD",
                         2,
                         Format.FormatR32G32B32A32Float,
                         InputElement.AppendAligned,
                         1,
                         InputClassification.PerInstanceData,
                         1),
        new("TEXCOORD",
                         3,
                         Format.FormatR32G32B32A32Float,
                         InputElement.AppendAligned,
                         1,
                         InputClassification.PerInstanceData,
                         1)
    ];

    /// <summary>
    /// </summary>
    public static readonly InputElement[] VsInputBillboard = [
        new("POSITION", 0, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 0),
        new("COLOR", 0, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 0),
        new("COLOR", 1, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 0),
        new("TEXCOORD", 0, Format.FormatR32G32Float, InputElement.AppendAligned, 0),
        new("TEXCOORD", 1, Format.FormatR32G32Float, InputElement.AppendAligned, 0),
        new("TEXCOORD", 2, Format.FormatR32G32Float, InputElement.AppendAligned, 0),
        new("TEXCOORD", 3, Format.FormatR32G32Float, InputElement.AppendAligned, 0),
        new("TEXCOORD", 4, Format.FormatR32G32Float, InputElement.AppendAligned, 0),
        new("TEXCOORD", 5, Format.FormatR32G32Float, InputElement.AppendAligned, 0),
        //INSTANCING: die 4 texcoords sind die matrix, die mit jedem buffer reinwandern
        new("TEXCOORD",
                         6,
                         Format.FormatR32G32B32A32Float,
                         InputElement.AppendAligned,
                         1,
                         InputClassification.PerInstanceData,
                         1),
        new("TEXCOORD",
                         7,
                         Format.FormatR32G32B32A32Float,
                         InputElement.AppendAligned,
                         1,
                         InputClassification.PerInstanceData,
                         1),
        new("TEXCOORD",
                         8,
                         Format.FormatR32G32B32A32Float,
                         InputElement.AppendAligned,
                         1,
                         InputClassification.PerInstanceData,
                         1),
        new("TEXCOORD",
                         9,
                         Format.FormatR32G32B32A32Float,
                         InputElement.AppendAligned,
                         1,
                         InputClassification.PerInstanceData,
                         1)
    ];

    /// <summary>
    ///     Gets the vs input billboard instancing.
    /// </summary>
    /// <value>
    ///     The vs input billboard instancing.
    /// </value>
    public static readonly InputElement[] VsInputBillboardInstancing = [
        new("POSITION", 0, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 0),
        new("COLOR", 0, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 0),
        new("COLOR", 1, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 0),
        new("TEXCOORD", 0, Format.FormatR32G32Float, InputElement.AppendAligned, 0),
        new("TEXCOORD", 1, Format.FormatR32G32Float, InputElement.AppendAligned, 0),
        new("TEXCOORD", 2, Format.FormatR32G32Float, InputElement.AppendAligned, 0),
        new("TEXCOORD", 3, Format.FormatR32G32Float, InputElement.AppendAligned, 0),
        new("TEXCOORD", 4, Format.FormatR32G32Float, InputElement.AppendAligned, 0),
        new("TEXCOORD", 5, Format.FormatR32G32Float, InputElement.AppendAligned, 0),
        //INSTANCING: die 4 texcoords sind die matrix, die mit jedem buffer reinwandern
        new("TEXCOORD",
                         6,
                         Format.FormatR32G32B32A32Float,
                         InputElement.AppendAligned,
                         1,
                         InputClassification.PerInstanceData,
                         1),
        new("TEXCOORD",
                         7,
                         Format.FormatR32G32B32A32Float,
                         InputElement.AppendAligned,
                         1,
                         InputClassification.PerInstanceData,
                         1),
        new("TEXCOORD",
                         8,
                         Format.FormatR32G32B32A32Float,
                         InputElement.AppendAligned,
                         1,
                         InputClassification.PerInstanceData,
                         1),
        new("TEXCOORD",
                         9,
                         Format.FormatR32G32B32A32Float,
                         InputElement.AppendAligned,
                         1,
                         InputClassification.PerInstanceData,
                         1),
        new("COLOR",
                         2,
                         Format.FormatR32G32B32A32Float,
                         InputElement.AppendAligned,
                         2,
                         InputClassification.PerInstanceData,
                         1),
        new("TEXCOORD",
                         10,
                         Format.FormatR32G32Float,
                         InputElement.AppendAligned,
                         2,
                         InputClassification.PerInstanceData,
                         1),
        new("TEXCOORD",
                         11,
                         Format.FormatR32G32Float,
                         InputElement.AppendAligned,
                         2,
                         InputClassification.PerInstanceData,
                         1)
    ];

    /// <summary>
    ///     Gets the vs input particle.
    /// </summary>
    /// <value>
    ///     The vs input particle.
    /// </value>
    public static readonly InputElement[] VsInputParticle = [
        new("TEXCOORD",
                         1,
                         Format.FormatR32G32B32A32Float,
                         InputElement.AppendAligned,
                         0,
                         InputClassification.PerInstanceData,
                         1),
        new("TEXCOORD",
                         2,
                         Format.FormatR32G32B32A32Float,
                         InputElement.AppendAligned,
                         0,
                         InputClassification.PerInstanceData,
                         1),
        new("TEXCOORD",
                         3,
                         Format.FormatR32G32B32A32Float,
                         InputElement.AppendAligned,
                         0,
                         InputClassification.PerInstanceData,
                         1),
        new("TEXCOORD",
                         4,
                         Format.FormatR32G32B32A32Float,
                         InputElement.AppendAligned,
                         0,
                         InputClassification.PerInstanceData,
                         1)
    ];

    /// <summary>
    ///     Gets the vs input skybox.
    /// </summary>
    /// <value>
    ///     The vs input skybox.
    /// </value>
    public static readonly InputElement[] VsInputSkybox = [
        new("SV_POSITION", 0, Format.FormatR32G32B32Float, InputElement.AppendAligned, 0)
    ];

    /// <summary>
    ///     Gets the vs input sprite 2d.
    /// </summary>
    /// <value>
    ///     The vs input sprite 2d.
    /// </value>
    public static readonly InputElement[] VsInputSprite2D = [
        new("POSITION", 0, Format.FormatR32G32Float, InputElement.AppendAligned, 0),
        new("TEXCOORD", 0, Format.FormatR32G32Float, InputElement.AppendAligned, 0),
        new("COLOR", 0, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 0)
    ];

    /// <summary>
    ///     Gets the vs input volume3d.
    /// </summary>
    /// <value>
    ///     The vs input volume3d.
    /// </value>
    public static readonly InputElement[] VsInputVolume3D = [
        new("SV_POSITION", 0, Format.FormatR32G32B32Float, InputElement.AppendAligned, 0)
    ];
}

/// <summary>
/// </summary>
public static class DefaultVsShaderDescriptions {
    /// <summary>
    /// </summary>
    public static readonly ShaderDescription VsMeshDefault = new(nameof(VsMeshDefault),
                                                                 ShaderStage.Vertex,
                                                                 new ShaderReflector(),
                                                                 DefaultVsShaderByteCodes.VsMeshDefault);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription VsMeshBatched = new(nameof(VsMeshBatched),
                                                                 ShaderStage.Vertex,
                                                                 new ShaderReflector(),
                                                                 DefaultVsShaderByteCodes.VsMeshBatched);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription VsMeshTessellation = new(nameof(VsMeshTessellation),
                                                                      ShaderStage.Vertex,
                                                                      new ShaderReflector(),
                                                                      DefaultVsShaderByteCodes
                                                                          .VsMeshTessellation);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription VsMeshShadow = new(nameof(VsMeshShadow),
                                                                ShaderStage.Vertex,
                                                                new ShaderReflector(),
                                                                DefaultVsShaderByteCodes.VsMeshShadow);

    /// <summary>
    ///     The vs mesh ssao
    /// </summary>
    public static readonly ShaderDescription VsMeshSsao = new(nameof(VsMeshSsao),
                                                              ShaderStage.Vertex,
                                                              new ShaderReflector(),
                                                              DefaultVsShaderByteCodes.VsMeshSsao);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription VsMeshBatchedShadow = new(nameof(VsMeshBatchedShadow),
                                                                       ShaderStage.Vertex,
                                                                       new ShaderReflector(),
                                                                       DefaultVsShaderByteCodes
                                                                           .VsMeshBatchedShadow);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription VsMeshBatchedSsao = new(nameof(VsMeshBatchedSsao),
                                                                     ShaderStage.Vertex,
                                                                     new ShaderReflector(),
                                                                     DefaultVsShaderByteCodes
                                                                         .VsMeshBatchedSsao);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription VsMeshInstancing = new(nameof(VsMeshInstancing),
                                                                    ShaderStage.Vertex,
                                                                    new ShaderReflector(),
                                                                    DefaultVsShaderByteCodes.VsMeshInstancing);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription VsMeshInstancingTessellation = new(
        nameof(VsMeshInstancingTessellation),
        ShaderStage.Vertex,
        new ShaderReflector(),
        DefaultVsShaderByteCodes.VsMeshInstancingTessellation);

    /// <summary>
    ///     The vs mesh bone skinned basic
    /// </summary>
    public static readonly ShaderDescription VsMeshBoneSkinnedBasic = new(nameof(VsMeshBoneSkinnedBasic),
        ShaderStage.Vertex,
        new ShaderReflector(),
        DefaultVsShaderByteCodes.VsMeshBoneSkinningBasic);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription VsPoint = new(nameof(VsPoint),
                                                           ShaderStage.Vertex,
                                                           new ShaderReflector(),
                                                           DefaultVsShaderByteCodes.VsPoint);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription VsPointShadow = new(nameof(VsPointShadow),
                                                                 ShaderStage.Vertex,
                                                                 new ShaderReflector(),
                                                                 DefaultVsShaderByteCodes.VsPointShadow);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription VsBillboardText = new(nameof(VsBillboardText),
                                                                   ShaderStage.Vertex,
                                                                   new ShaderReflector(),
                                                                   DefaultVsShaderByteCodes.VsBillboard);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription VsBillboardInstancing = new(nameof(VsBillboardInstancing),
        ShaderStage.Vertex,
        new ShaderReflector(),
        DefaultVsShaderByteCodes.VsBillboardInstancing);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription VsMeshClipPlane = new(nameof(VsMeshClipPlane),
                                                                   ShaderStage.Vertex,
                                                                   new ShaderReflector(),
                                                                   DefaultVsShaderByteCodes.VsMeshClipPlane);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription VsFullScreenQuad = new(nameof(VsFullScreenQuad),
                                                                    ShaderStage.Vertex,
                                                                    new ShaderReflector(),
                                                                    DefaultVsShaderByteCodes
                                                                        .VsMeshClipPlaneQuad);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription VsParticle = new(nameof(VsParticle),
                                                              ShaderStage.Vertex,
                                                              new ShaderReflector(),
                                                              DefaultVsShaderByteCodes.VsParticle);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription VsSkybox = new(nameof(VsSkybox),
                                                            ShaderStage.Vertex,
                                                            new ShaderReflector(),
                                                            DefaultVsShaderByteCodes.VsSkybox);

    /// <summary>
    ///     The vs mesh wireframe
    /// </summary>
    public static readonly ShaderDescription VsMeshWireframe = new(nameof(VsMeshWireframe),
                                                                   ShaderStage.Vertex,
                                                                   new ShaderReflector(),
                                                                   DefaultVsShaderByteCodes.VsMeshWireframe);

    /// <summary>
    ///     The vs mesh depth
    /// </summary>
    public static readonly ShaderDescription VsMeshDepth = new(nameof(VsMeshDepth),
                                                               ShaderStage.Vertex,
                                                               new ShaderReflector(),
                                                               DefaultVsShaderByteCodes.VsMeshDepth);

    /// <summary>
    ///     The vs mesh batched wireframe
    /// </summary>
    public static readonly ShaderDescription VsMeshBatchedWireframe = new(nameof(VsMeshBatchedWireframe),
        ShaderStage.Vertex,
        new ShaderReflector(),
        DefaultVsShaderByteCodes.VsMeshBatchedWireframe);

    /// <summary>
    ///     The vs mesh outline pass1
    /// </summary>
    public static readonly ShaderDescription VsMeshOutlinePass1 = new(nameof(VsMeshOutlinePass1),
                                                                      ShaderStage.Vertex,
                                                                      new ShaderReflector(),
                                                                      DefaultVsShaderByteCodes.VsMeshOutlineP1);

    /// <summary>
    ///     The vs mesh outline pass1
    /// </summary>
    public static readonly ShaderDescription VsMeshOutlineScreenQuad = new(nameof(VsMeshOutlineScreenQuad),
        ShaderStage.Vertex,
        new ShaderReflector(),
        DefaultVsShaderByteCodes.VsMeshOutlineScreenQuad);

    /// <summary>
    ///     The vs plane grid
    /// </summary>
    public static readonly ShaderDescription VsPlaneGrid = new(nameof(VsPlaneGrid),
                                                               ShaderStage.Vertex,
                                                               new ShaderReflector(),
                                                               DefaultVsShaderByteCodes.VsPlaneGrid);

    /// <summary>
    ///     The vs screen quad
    /// </summary>
    public static readonly ShaderDescription VsScreenQuad = new(nameof(VsScreenQuad),
                                                                ShaderStage.Vertex,
                                                                new ShaderReflector(),
                                                                DefaultVsShaderByteCodes.VsScreenQuad);

    /// <summary>
    ///     The vs sprite
    /// </summary>
    public static readonly ShaderDescription VsSprite2D = new(nameof(VsSprite2D),
                                                              ShaderStage.Vertex,
                                                              new ShaderReflector(),
                                                              DefaultVsShaderByteCodes.VsSprite2D);

    /// <summary>
    ///     The vs volume3d
    /// </summary>
    public static readonly ShaderDescription VsVolume3D = new(nameof(VsVolume3D),
                                                              ShaderStage.Vertex,
                                                              new ShaderReflector(),
                                                              DefaultVsShaderByteCodes.VsVolume3D);

    /// <summary>
    ///     The vsssao
    /// </summary>
    public static readonly ShaderDescription Vsssao = new(nameof(Vsssao),
                                                          ShaderStage.Vertex,
                                                          new ShaderReflector(),
                                                          DefaultVsShaderByteCodes.Vsssao);

#if !WINDOWS_UWP
    /// <summary>
    ///     The vs screen dup
    /// </summary>
    public static readonly ShaderDescription VsScreenDup = new(nameof(VsScreenDup),
                                                               ShaderStage.Vertex,
                                                               new ShaderReflector(),
                                                               DefaultVsShaderByteCodes.VsScreenDup);

    /// <summary>
    ///     The vs screen dup mouse cursor
    /// </summary>
    public static readonly ShaderDescription VsScreenDupCursor = new(nameof(VsScreenDupCursor),
                                                                     ShaderStage.Vertex,
                                                                     new ShaderReflector(),
                                                                     DefaultVsShaderByteCodes
                                                                         .VsScreenDupCursor);

#endif
}

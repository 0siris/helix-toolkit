/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core.DefaultShaders;
public static class DefaultComputeShaders {
    /// <summary>
    /// </summary>
    public static string CsParticleInsert { get; } = "csParticleInsert";

    /// <summary>
    /// </summary>
    public static string CsParticleUpdate { get; } = "csParticleUpdate";
}


/// <summary>Registers native compute kernels consumed by the existing render-host resource owners.</summary>
public static class DefaultComputeShaderDescriptions {
    /// The active source generic selected-triangle-pair kernel.
    public static readonly ShaderDescription CsMeshIntersect = new(nameof(CsMeshIntersect), ShaderStage.Compute,
        new ShaderReflector(), "csMeshIntersect");
    /// The active source kernel consuming actual flat render indices and float4 position sidecars.
    public static readonly ShaderDescription CsMeshIntersectSharedRenderBuffer = new(nameof(CsMeshIntersectSharedRenderBuffer),
        ShaderStage.Compute, new ShaderReflector(), "csMeshIntersectSharedRenderBuffer");
    /// <summary>
    /// </summary>
    public static readonly ShaderDescription CsParticleInsert = new(nameof(CsParticleInsert),
                                                                    ShaderStage.Compute,
                                                                    new ShaderReflector(),
                                                                    DefaultComputeShaders.CsParticleInsert);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription CsParticleUpdate = new(nameof(CsParticleUpdate),
                                                                    ShaderStage.Compute,
                                                                    new ShaderReflector(),
                                                                    DefaultComputeShaders.CsParticleUpdate);
}

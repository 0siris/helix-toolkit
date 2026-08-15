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


public static class DefaultComputeShaderDescriptions {
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

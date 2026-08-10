/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


namespace HelixToolkit.SharpDX.Core.Shaders;
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

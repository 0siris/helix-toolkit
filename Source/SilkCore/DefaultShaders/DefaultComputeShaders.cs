/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


namespace HelixToolkit.SharpDX.Core.Shaders;
public static class DefaultComputeShaders {
    /// <summary>
    /// </summary>
    public static string CSParticleInsert { get; } = "csParticleInsert";

    /// <summary>
    /// </summary>
    public static string CSParticleUpdate { get; } = "csParticleUpdate";
}


public static class DefaultComputeShaderDescriptions {
    /// <summary>
    /// </summary>
    public static readonly ShaderDescription CSParticleInsert = new(nameof(CSParticleInsert),
                                                                    ShaderStage.Compute,
                                                                    new ShaderReflector(),
                                                                    DefaultComputeShaders.CSParticleInsert);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription CSParticleUpdate = new(nameof(CSParticleUpdate),
                                                                    ShaderStage.Compute,
                                                                    new ShaderReflector(),
                                                                    DefaultComputeShaders.CSParticleUpdate);
}

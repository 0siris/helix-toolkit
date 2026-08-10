/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


namespace HelixToolkit.SharpDX.Core.Shaders;
public static class DefaultHullShaders {
    /// <summary>
    /// </summary>
    public static string HsMeshTessellation { get; } = "hsMeshTriTessellation";
}

public static class DefaultHullShaderDescriptions {
    public static readonly ShaderDescription HsMeshTessellation = new(nameof(HsMeshTessellation),
                                                                      ShaderStage.Hull,
                                                                      new ShaderReflector(),
                                                                      DefaultHullShaders.HsMeshTessellation);
}

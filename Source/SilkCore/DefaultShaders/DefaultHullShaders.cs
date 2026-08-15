/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core.DefaultShaders;
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

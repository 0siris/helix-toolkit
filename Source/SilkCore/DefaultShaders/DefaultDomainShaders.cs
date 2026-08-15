/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core.DefaultShaders;
/// <summary>
/// </summary>
public static class DefaultDomainShaders {
    /// <summary>
    /// </summary>
    public static string DsMeshTessellation { get; } = "dsMeshTriTessellation";
}

public static class DefaultDomainShaderDescriptions {
    public static readonly ShaderDescription DsMeshTessellation = new(nameof(DsMeshTessellation),
                                                                      ShaderStage.Domain,
                                                                      new ShaderReflector(),
                                                                      DefaultDomainShaders.DsMeshTessellation);
}

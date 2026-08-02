/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


namespace HelixToolkit.SharpDX.Core {
    namespace Shaders {
        public static class DefaultHullShaders {
            /// <summary>
            /// </summary>
            public static string HSMeshTessellation { get; } = "hsMeshTriTessellation";
        }

        public static class DefaultHullShaderDescriptions {
            public static readonly ShaderDescription HSMeshTessellation = new(nameof(HSMeshTessellation),
                                                                              ShaderStage.Hull,
                                                                              new ShaderReflector(),
                                                                              DefaultHullShaders.HSMeshTessellation);
        }
    }
}

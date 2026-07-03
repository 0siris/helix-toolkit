/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


namespace HelixToolkit.SharpDX.Core {
    namespace Shaders {
        /// <summary>
        /// </summary>
        public static class DefaultDomainShaders {
            /// <summary>
            /// </summary>
            public static string DSMeshTessellation { get; } = "dsMeshTriTessellation";
        }

        public static class DefaultDomainShaderDescriptions {
            public static readonly ShaderDescription DSMeshTessellation = new(nameof(DSMeshTessellation),
                                                                              ShaderStage.Domain,
                                                                              new ShaderReflector(),
                                                                              DefaultDomainShaders.DSMeshTessellation);
        }
    }
}

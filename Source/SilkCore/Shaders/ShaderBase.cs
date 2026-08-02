/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core {
    namespace Shaders {
        /// <summary>
        /// </summary>
        public abstract class ShaderBase : DisposeObject {
            /// <summary>
            /// </summary>
            /// <param name="name"></param>
            /// <param name="type"></param>
            /// <param name="isNull"></param>
            public ShaderBase(string name, ShaderStage type, bool isNull = false) {
                ShaderType = type;
                ShaderStageIndex = type.ToIndex();
                Name = name;
                IsNULL = isNull;
            }

            /// <summary>
            /// </summary>
            public MappingProxy<ConstantBufferProxy> ConstantBufferMapping { get; } = new();

            /// <summary>
            /// </summary>
            public MappingProxy<TextureMapping> ShaderResourceViewMapping { get; } = new();

            /// <summary>
            /// </summary>
            public MappingProxy<UAVMapping> UnorderedAccessViewMapping { get; } = new();

            /// <summary>
            /// </summary>
            public MappingProxy<SamplerMapping> SamplerMapping { get; } = new();

            internal virtual IShaderHandle NativeShader => null;

            /// <summary>
            ///     Gets the type of the shader.
            /// </summary>
            /// <value>
            ///     The type of the shader.
            /// </value>
            public ShaderStage ShaderType { get; private set; }

            /// <summary>
            ///     Gets the index of the shader stage.
            /// </summary>
            /// <value>
            ///     The index of the shader stage.
            /// </value>
            public int ShaderStageIndex { get; private set; }

            /// <summary>
            ///     If is null shader
            /// </summary>
            public bool IsNULL { get; protected set; }

            /// <summary>
            ///     Shader Name
            /// </summary>
            public string Name { get; private set; }

            protected override void OnDispose(bool disposeManagedResources) {
                ConstantBufferMapping.Dispose();
                ShaderResourceViewMapping.Dispose();
                UnorderedAccessViewMapping.Dispose();
                SamplerMapping.Dispose();
                base.OnDispose(disposeManagedResources);
            }
        }
    }
}

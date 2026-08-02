/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.CompilerServices;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render;

namespace HelixToolkit.SharpDX.Core {
    namespace Shaders {
        /// <summary>
        /// </summary>
        public sealed class HullShader : ShaderBase {
            public static readonly HullShader NullHullShader = new("NULL");
            public static readonly HullShaderType Type;
            private HullShaderHandle shader;

            /// <summary>
            ///     Vertex Shader
            /// </summary>
            /// <param name="device"></param>
            /// <param name="name"></param>
            /// <param name="byteCode"></param>
            internal HullShader(SilkD3DDevice device, string name, byte[] byteCode)
                : base(name, ShaderStage.Hull) {
                shader = device.CreateHullShader(byteCode);
            }

            private HullShader(string name)
                : base(name, ShaderStage.Hull, true) { }

            internal HullShaderHandle Shader => shader;
            internal override IShaderHandle NativeShader => shader;

            /// <summary>
            ///     Binds shader to pipeline
            /// </summary>
            /// <param name="context">The context.</param>
            /// <param name="bindConstantBuffer"></param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Bind(DeviceContextProxy context, bool bindConstantBuffer = true) {
                context.SetShader(this);
            }

            protected override void OnDispose(bool disposeManagedResources) {
                RemoveAndDispose(ref shader);
                base.OnDispose(disposeManagedResources);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator HullShaderType(HullShader s) {
                return Type;
            }
        }
    }
}

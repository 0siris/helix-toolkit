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
        public sealed class GeometryShader : ShaderBase {
            public static readonly GeometryShader NullGeometryShader = new("NULL");
            public static readonly GeometryShaderType Type;
            private GeometryShaderHandle shader;

            /// <summary>
            ///     Initializes a new instance of the <see cref="GeometryShader" /> class.
            /// </summary>
            /// <param name="device">The device.</param>
            /// <param name="name">The name.</param>
            /// <param name="byteCode">The byte code.</param>
            internal GeometryShader(SilkD3DDevice device, string name, byte[] byteCode) : base(name,
                ShaderStage.Geometry) {
                shader = device.CreateGeometryShader(byteCode);
            }

            /// <summary>
            ///     Initializes a new instance of the <see cref="GeometryShader" /> class. This is used for stream out geometry shader
            /// </summary>
            /// <param name="device">The device.</param>
            /// <param name="name">The name.</param>
            /// <param name="byteCode">The byte code.</param>
            /// <param name="streamOutputElements">The stream output elements.</param>
            /// <param name="bufferStrides">The buffer strides.</param>
            /// <param name="rasterizedStream">The rasterized stream.</param>
            internal GeometryShader(
                SilkD3DDevice device,
                string name,
                byte[] byteCode,
                StreamOutputElement[] streamOutputElements,
                int[] bufferStrides,
                int rasterizedStream = -1
            )
                : base(name, ShaderStage.Geometry) {
                shader = device.CreateGeometryShader(byteCode, streamOutputElements, bufferStrides, rasterizedStream);
            }

            private GeometryShader(string name)
                : base(name, ShaderStage.Geometry, true) { }

            internal GeometryShaderHandle Shader => shader;
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
            public static implicit operator GeometryShaderType(GeometryShader s) {
                return Type;
            }
        }
    }
}

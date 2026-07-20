/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.CompilerServices;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core {
    namespace Model {
        /// <summary>
        ///     Default Light Model
        /// </summary>
        public sealed class LightsBufferModel : ILightsBufferProxy<LightStruct> {
            public const int SizeInBytes = LightStruct.SizeInBytes * Constants.MaxLights + 4 * 4 * 2;

            /// <summary>
            ///     Gets or sets the environment map mip levels.
            /// </summary>
            /// <value>
            ///     The environment map mip levels.
            /// </value>
            internal int EnvironmentMapMipLevels = 0;

            /// <summary>
            ///     Gets or sets a value indicating whether the scene has environment map.
            /// </summary>
            /// <value>
            ///     <c>true</c> if the scene has environment map; otherwise, <c>false</c>.
            /// </value>
            internal bool HasEnvironmentMap = false;

            public Color4 AmbientLight { get; set; } = new(0, 0, 0, 1);
            public int LightCount { get; private set; }

            public int BufferSize => SizeInBytes;

            public LightStruct[] Lights { get; } = new LightStruct[Constants.MaxLights];

            public void IncrementLightCount() {
                ++LightCount;
            }

            public void ResetLightCount() {
                LightCount = 0;
                AmbientLight = new Color4(0, 0, 0, 1);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void UploadToBuffer(IBufferProxy buffer, DeviceContextProxy context) {
                if (buffer.StructureSize != SizeInBytes) {
#if DEBUG
                    throw new ArgumentException("Buffer type or size do not match the model requirement");
#endif
                    return;
                }

                if (buffer is ConstantBufferProxy constantBuffer) {
                    constantBuffer.UploadDataToBuffer(context, Upload);
                    return;
                }

                var dataBox = context.MapSubresource(buffer.Buffer, 0, MapMode.WriteDiscard, MapFlags.None);
                if (dataBox.IsEmpty) return;
                Upload(dataBox);
                context.UnmapSubresource(buffer.Buffer, 0);
            }

            private void Upload(DataBox dataBox) {
                var ptr = UnsafeHelper.Write(dataBox.DataPointer, Lights, 0, Lights.Length);
                ptr = UnsafeHelper.Write(ptr, AmbientLight);
                ptr = UnsafeHelper.Write(ptr, LightCount);
                ptr = UnsafeHelper.Write(ptr, HasEnvironmentMap ? 1 : 0);
                UnsafeHelper.Write(ptr, EnvironmentMapMipLevels);
            }
        }
    }
}

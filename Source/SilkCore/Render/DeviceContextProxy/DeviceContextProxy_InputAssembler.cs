using System.Runtime.CompilerServices;
using HelixToolkit.SharpDX.Core.Shaders;
using Silk.NET.Core.Native;

namespace HelixToolkit.SharpDX.Core {
    namespace Render {
        public partial class DeviceContextProxy {
            private InputLayoutProxy currInputLayout;

            public PrimitiveTopology PrimitiveTopology {
                get => (PrimitiveTopology)nativeDeviceContext.PrimitiveTopology;
                set => nativeDeviceContext.PrimitiveTopology = (D3DPrimitiveTopology)value;
            }

            public InputLayoutProxy InputLayout {
                get => currInputLayout;
                set {
                    if (currInputLayout == value) return;

                    currInputLayout = value;
                    nativeDeviceContext.SetInputLayout(value?.Layout);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetIndexBuffer(Buffer? buffer, Format format, int offset) {
                NativeContext.SetIndexBuffer(buffer, format, offset);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetVertexBuffers(int slot, VertexBufferBinding binding) {
                NativeContext.SetVertexBuffer(slot, binding);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetVertexBuffers(int startSlot, VertexBufferBinding[] bindings) {
                NativeContext.SetVertexBuffers(startSlot, bindings);
            }
        }
    }
}

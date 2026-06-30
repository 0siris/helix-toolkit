using System.Runtime.CompilerServices;
using Silk.NET.Core.Native;

#if !NETFX_CORE
namespace HelixToolkit.Wpf.SharpDX
#else
#if CORE
namespace HelixToolkit.SharpDX.Core
#else
namespace HelixToolkit.UWP
#endif
#endif
{
    namespace Render
    {
        using Shaders;

        public partial class DeviceContextProxy
        {
            public PrimitiveTopology PrimitiveTopology
            {
                set
                {
                    nativeDeviceContext.PrimitiveTopology = (D3DPrimitiveTopology)value;
                }
                get
                {
                    return (PrimitiveTopology)nativeDeviceContext.PrimitiveTopology;
                }
            }

            private InputLayoutProxy currInputLayout;

            public InputLayoutProxy InputLayout
            {
                set
                {
                    if (currInputLayout == value)
                    {
                        return;
                    }

                    currInputLayout = value;
                    nativeDeviceContext.SetInputLayout(value?.Layout);
                }
                get
                {
                    return currInputLayout;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetIndexBuffer(Buffer buffer, Format format, int offset)
            {
                NativeContext.SetIndexBuffer(buffer, format, offset);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetVertexBuffers(int slot, VertexBufferBinding binding)
            {
                NativeContext.SetVertexBuffer(slot, binding);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetVertexBuffers(int startSlot, VertexBufferBinding[] bindings)
            {
                NativeContext.SetVertexBuffers(startSlot, bindings);
            }
        }
    }
}

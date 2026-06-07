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
            public D3DPrimitiveTopology PrimitiveTopology
            {
                set
                {
                    nativeDeviceContext.PrimitiveTopology = value;
                }
                get
                {
                    return nativeDeviceContext.PrimitiveTopology;
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
                    throw new System.NotSupportedException("Input layouts require the native input layout wrapper migration.");
                }
                get
                {
                    return currInputLayout;
                }
            }
        }
    }
}

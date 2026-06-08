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

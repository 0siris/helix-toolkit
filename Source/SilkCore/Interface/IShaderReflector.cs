using System.Collections.Generic;

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
    namespace Shaders
    {
        public interface IShaderReflector
        {
            FeatureLevel FeatureLevel
            {
                get;
            }

            void Parse(byte[] byteCode, ShaderStage stage);

            Dictionary<string, ConstantBufferMapping> ConstantBufferMappings
            {
                get;
            }

            Dictionary<string, TextureMapping> TextureMappings
            {
                get;
            }

            Dictionary<string, UAVMapping> UAVMappings
            {
                get;
            }

            Dictionary<string, SamplerMapping> SamplerMappings
            {
                get;
            }
        }
    }
}

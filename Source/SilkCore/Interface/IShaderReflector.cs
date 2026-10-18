using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core.Shaders;
public interface IShaderReflector {
    FeatureLevel FeatureLevel { get; }

    Dictionary<string, ConstantBufferMapping> ConstantBufferMappings { get; }

    Dictionary<string, TextureMapping> TextureMappings { get; }

    Dictionary<string, UAVMapping> UavMappings { get; }

    Dictionary<string, SamplerMapping> SamplerMappings { get; }

    void Parse(byte[] byteCode, ShaderStage stage);
}

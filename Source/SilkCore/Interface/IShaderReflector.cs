using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core.Interface;
public interface IShaderReflector {
    FeatureLevel FeatureLevel { get; }

    Dictionary<string, ConstantBufferMapping> ConstantBufferMappings { get; }

    Dictionary<string, TextureMapping> TextureMappings { get; }

    Dictionary<string, UavMapping> UavMappings { get; }

    Dictionary<string, SamplerMapping> SamplerMappings { get; }

    void Parse(byte[] byteCode, ShaderStage stage);
}

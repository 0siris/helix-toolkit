namespace HelixToolkit.SharpDX.Core.Shaders;
public interface IShaderReflector {
    FeatureLevel FeatureLevel { get; }

    Dictionary<string, ConstantBufferMapping> ConstantBufferMappings { get; }

    Dictionary<string, TextureMapping> TextureMappings { get; }

    Dictionary<string, UavMapping> UavMappings { get; }

    Dictionary<string, SamplerMapping> SamplerMappings { get; }

    void Parse(byte[] byteCode, ShaderStage stage);
}

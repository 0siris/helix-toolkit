using System.Reflection;

namespace HelixToolkit.SharpDX.Core.Helper;
/// <summary>
/// </summary>
public interface IShaderByteCodeReader {
    byte[] Read(string name);
}

/// <summary>
///     Used to read HelixToolkit internal default shader byte codes
/// </summary>
public sealed class HelixToolkitByteCodeReader : IShaderByteCodeReader {
    public byte[] Read(string name) {
        var assembly = typeof(UWPShaderBytePool).GetTypeInfo().Assembly;
        return ReadResource(assembly, $"SilkCore.Resources.{name}.cso", $"{name}.cso");
    }

    public byte[] ReadDxil(string stage, string name, string entryPoint = "main") {
        var assembly = typeof(UWPShaderBytePool).GetTypeInfo().Assembly;
        return ReadResource(assembly,
                            $"SilkCore.Resources.DX12.{stage}.{name}.{entryPoint}.dxil",
                            $"{stage}\\{name}.{entryPoint}.dxil");
    }

    private static byte[] ReadResource(Assembly assembly, string resourceName, string fileName) {
        var shaderStream = assembly.GetManifestResourceStream(resourceName);
        if (shaderStream == null)
            throw new FileNotFoundException($"Shader byte code was not found: {resourceName}", fileName);
        using var memory = new MemoryStream();
        shaderStream.CopyTo(memory);
        return memory.ToArray();
    }
}

/// <summary>
///     Used to read shader bytecode
/// </summary>
public static class UWPShaderBytePool {
    public static Dictionary<string, byte[]> Dict = [];
    internal static readonly HelixToolkitByteCodeReader InternalByteCodeReader = new();

    public static byte[] Read(string name, IShaderByteCodeReader? reader = null) {
        lock (Dict) {
            if (!Dict.TryGetValue(name, out var byteCode))
                lock (Dict) {
                    if (!Dict.TryGetValue(name, out byteCode)) {
                        if (reader == null)
                            byteCode = InternalByteCodeReader.Read(name);
                        else
                            byteCode = reader.Read(name);
                        Dict.Add(name, byteCode);
                    }
                }

            return byteCode;
        }
    }

    public static byte[] ReadDxil(string stage, string name, string entryPoint = "main") {
        var key = $"DX12/{stage}/{name}/{entryPoint}";
        lock (Dict) {
            if (!Dict.TryGetValue(key, out var byteCode)) {
                byteCode = InternalByteCodeReader.ReadDxil(stage, name, entryPoint);
                Dict.Add(key, byteCode);
            }

            return byteCode;
        }
    }
}

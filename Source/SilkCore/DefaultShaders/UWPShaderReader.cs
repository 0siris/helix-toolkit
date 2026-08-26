using System.Reflection;

namespace HelixToolkit.SharpDX.Core.DefaultShaders;
/// <summary>
/// </summary>
public interface IShaderByteCodeReader {
    /// <summary>
    ///     Reads one Shader Model 6 DXIL module.
    /// </summary>
    /// <param name="stage">The two-letter shader stage.</param>
    /// <param name="name">The shader name.</param>
    /// <param name="entryPoint">The shader entry point.</param>
    /// <returns>The DXIL bytecode.</returns>
    byte[] ReadDxil(string stage, string name, string entryPoint = "main");
}

/// <summary>
///     Used to read HelixToolkit internal default shader byte codes
/// </summary>
public sealed class HelixToolkitByteCodeReader : IShaderByteCodeReader {
    /// <inheritdoc />
    public byte[] ReadDxil(string stage, string name, string entryPoint = "main") {
        var assembly = typeof(UwpShaderBytePool).GetTypeInfo().Assembly;
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
public static class UwpShaderBytePool {
    public static Dictionary<string, byte[]> Dict = [];
    internal static readonly HelixToolkitByteCodeReader InternalByteCodeReader = new();

    /// <summary>
    ///     Reads and caches one embedded Shader Model 6 DXIL module.
    /// </summary>
    /// <param name="stage">The two-letter shader stage.</param>
    /// <param name="name">The shader name.</param>
    /// <param name="entryPoint">The shader entry point.</param>
    /// <returns>The DXIL bytecode.</returns>
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

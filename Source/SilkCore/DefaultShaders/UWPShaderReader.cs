using System.Collections.Generic;
using System.IO;
using System.Reflection;
namespace HelixToolkit.SharpDX.Core
{
    namespace Helper
    {
        /// <summary>
        /// 
        /// </summary>
        public interface IShaderByteCodeReader
        {
            byte[] Read(string name);
        }

        /// <summary>
        /// Used to read HelixToolkit internal default shader byte codes
        /// </summary>
        public sealed class HelixToolkitByteCodeReader : IShaderByteCodeReader
        {
            public byte[] Read(string name)
            {
#if CORE
                var assembly = typeof(UWPShaderBytePool).GetTypeInfo().Assembly;
                Stream shaderStream = assembly.GetManifestResourceStream($"SilkCore.Resources.{name}.cso");
                if (shaderStream == null)
                {
                    throw new FileNotFoundException($"Shader byte code was not found: {name}", $"{name}.cso");
                }
                using (var memory = new MemoryStream())
                {
                    shaderStream.CopyTo(memory);
                    return memory.ToArray();
                }
#else
#if NETFX_CORE
                var filePath = Windows.ApplicationModel.Package.Current.InstalledLocation.Path + $"\\HelixToolkit.UWP\\Resources\\{name}.cso";
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Shader byte code was not found: {name}", filePath);
                }
                var byteCode = global::SharpDX.IO.NativeFile.ReadAllBytes(filePath);
                if(byteCode == null)
                {
                    throw new FileNotFoundException($"Shader byte code was not found: {name}", filePath);
                }
                return byteCode;
#else
                var byteCode = Properties.Resources.ResourceManager.GetObject(name) as byte[];
                if (byteCode == null)
                {
                    throw new FileNotFoundException($"Shader byte code was not found: {name}", $"{name}.cso");
                }
                return byteCode;
#endif
#endif
            }
        }
        /// <summary>
        /// Used to read shader bytecode
        /// </summary>
        public static class UWPShaderBytePool
        {
            public static Dictionary<string, byte[]> Dict = new Dictionary<string, byte[]>();
            internal static readonly IShaderByteCodeReader InternalByteCodeReader = new HelixToolkitByteCodeReader();
            public static byte[] Read(string name, IShaderByteCodeReader reader = null)
            {
                lock (Dict)
                {
                    if (!Dict.TryGetValue(name, out var byteCode))
                    {
                        lock (Dict)
                        {
                            if (!Dict.TryGetValue(name, out byteCode))
                            {
                                if (reader == null)
                                {
                                    byteCode = InternalByteCodeReader.Read(name);
                                }
                                else
                                {
                                    byteCode = reader.Read(name);
                                }
                                Dict.Add(name, byteCode);
                            }
                        }
                    }
                    return byteCode;
                }
            }
        }
    }
}

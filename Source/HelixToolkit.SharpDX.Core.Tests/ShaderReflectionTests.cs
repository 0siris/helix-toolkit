using HelixToolkit.SharpDX.Core.Helper;
using HelixToolkit.SharpDX.Core.Shaders;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

namespace HelixToolkit.SharpDX.Core.Tests
{
    [TestFixture]
    public class ShaderReflectionTests
    {
        [Test]
        public void ReflectsAllEmbeddedShaders()
        {
            var assembly = typeof(DefaultEffectsManager).Assembly;
            var resources = assembly.GetManifestResourceNames()
                .Where(x => x.EndsWith(".cso", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            Assert.That(resources, Is.Not.Empty);
            foreach (var resource in resources)
            {
                using var stream = assembly.GetManifestResourceStream(resource);
                using var memory = new MemoryStream();
                stream.CopyTo(memory);
                var byteCode = memory.ToArray();
                var reflector = new ShaderReflector();

                Assert.DoesNotThrow(
                    () => reflector.Parse(byteCode, GetStage(resource)),
                    resource);
                Assert.That(reflector.FeatureLevel, Is.Not.EqualTo(FeatureLevel.Level_DEFAULT), resource);
            }
        }

        [Test]
        public void MapsKnownMeshShaderResources()
        {
            var reflector = new ShaderReflector();
            reflector.Parse(UWPShaderBytePool.Read(DefaultPSShaderByteCodes.PSMeshBinnPhong), ShaderStage.Pixel);

            Assert.That(reflector.ConstantBufferMappings, Contains.Key(DefaultBufferNames.ModelCB));
            Assert.That(reflector.ConstantBufferMappings[DefaultBufferNames.ModelCB].Description.StructSize, Is.GreaterThan(0));
            Assert.That(reflector.ConstantBufferMappings[DefaultBufferNames.ModelCB].Description.Variables, Is.Not.Empty);
            Assert.That(reflector.TextureMappings, Contains.Key(DefaultBufferNames.DiffuseMapTB));
            Assert.That(reflector.SamplerMappings, Contains.Key(DefaultSamplerStateNames.SurfaceSampler));
        }

        [Test]
        public void ReportsMissingAndInvalidByteCode()
        {
            Assert.Throws<FileNotFoundException>(() => new HelixToolkitByteCodeReader().Read("missing"));
            Assert.Throws<ArgumentException>(() => new ShaderReflector().Parse(Array.Empty<byte>(), ShaderStage.Vertex));
            Assert.Throws<InvalidDataException>(() => new ShaderReflector().Parse(new byte[] { 1, 2, 3, 4 }, ShaderStage.Vertex));
        }

        private static ShaderStage GetStage(string resource)
        {
            var marker = ".Resources.";
            var name = resource.Substring(resource.IndexOf(marker, StringComparison.Ordinal) + marker.Length);
            if (name.StartsWith("vs", StringComparison.OrdinalIgnoreCase)) return ShaderStage.Vertex;
            if (name.StartsWith("ps", StringComparison.OrdinalIgnoreCase)) return ShaderStage.Pixel;
            if (name.StartsWith("gs", StringComparison.OrdinalIgnoreCase)) return ShaderStage.Geometry;
            if (name.StartsWith("hs", StringComparison.OrdinalIgnoreCase)) return ShaderStage.Hull;
            if (name.StartsWith("ds", StringComparison.OrdinalIgnoreCase)) return ShaderStage.Domain;
            if (name.StartsWith("cs", StringComparison.OrdinalIgnoreCase)) return ShaderStage.Compute;
            throw new InvalidDataException($"Unknown shader stage: {resource}");
        }
    }
}

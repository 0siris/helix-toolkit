using System.Runtime.InteropServices;
using System.Text;
using HelixToolkit.SharpDX.Core.Geometry;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.SharpDX.Toolkit.Graphics;
using Silk.NET.Maths;
using DxgiFormat = Silk.NET.DXGI.Format;

namespace SilkCore.Tests;

public class TextureAndResourceTests {
    /// <summary>
    ///     Verifies CPU images retain automatic mip-chain validation after removal of D3D11 texture wrappers.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void CpuImagesCalculateMipChainsWithoutD3D11Textures() {
        using var image1D = Image.New1D(8, true, PixelFormat.R8.UNorm);
        using var image2D = Image.New2D(8, 4, true, PixelFormat.R8.UNorm);
        using var image3D = Image.New3D(8, 8, 4, true, PixelFormat.R8.UNorm);

        Assert.Equal(4, image1D.Description.MipLevels);
        Assert.Equal(4, image2D.Description.MipLevels);
        Assert.Equal(4, image3D.Description.MipLevels);
        Assert.Throws<InvalidOperationException>(() => Image.New1D(8, 5, PixelFormat.R8.UNorm));
        Assert.Throws<InvalidOperationException>(() => Image.New3D(6, 6, 6, true, PixelFormat.R8.UNorm));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DdsLoaderReadsBc1Pixels() {
        var expected = new byte[] {1, 2, 3, 4, 5, 6, 7, 8};
        using var stream = CreateDds(expected);
        using var image = Image.Load(stream)
                          ?? throw new InvalidOperationException("The DDS image could not be loaded.");
        var actual = new byte[expected.Length];
        Marshal.Copy(image.DataPointer, actual, 0, actual.Length);

        Assert.Equal(4, image.Description.Width);
        Assert.Equal(4, image.Description.Height);
        Assert.Equal((int) DxgiFormat.FormatBC1Unorm, (int) image.Description.Format);
        Assert.Equal(expected, actual);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void CompressedTextureRetainsStreamContract() {
        using var stream = new MemoryStream([1, 2, 3]);
        var texture = new TextureInfo(stream, false);

        Assert.Same(stream, texture.Texture);
        Assert.Equal(TextureDataType.Stream, texture.DataType);
        Assert.True(texture.IsCompressed);
        Assert.False(texture.GenerateMipMaps);
        Assert.Equal(0, texture.Dimension);
    }

    [Theory]
    [InlineData(1, 1, 0, 0)]
    [InlineData(2, 2, 3, 0)]
    [InlineData(3, 2, 3, 4)]
    [Trait("Category", "Unit")]
    public void RawByteTexturesReportDimensions(int dimension, int width, int height, int depth) {
        var data = new byte[Math.Max(1, width * Math.Max(1, height) * Math.Max(1, depth))];
        var texture = dimension switch {
            1 => new TextureInfo(data, DxgiFormat.FormatR8Unorm, width),
            2 => new TextureInfo(data, DxgiFormat.FormatR8Unorm, width, height),
            _ => new TextureInfo(data, DxgiFormat.FormatR8Unorm, width, height, depth)
        };

        Assert.Equal(TextureDataType.ByteArray, texture.DataType);
        Assert.Equal(dimension, texture.Dimension);
        Assert.Equal(width, texture.Width);
        Assert.Equal(height, texture.Height);
        Assert.Equal(depth, texture.Depth);
        Assert.Same(data, texture.TextureRaw);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ColorTextureUsesFloatRgbaFormat() {
        var colors = new[] {new Vector4D<float>(1, 0, 0, 1), new Vector4D<float>(0, 1, 0, 1)};

        var texture = new TextureInfo(colors, 2, 1);

        Assert.Equal(TextureDataType.Color4, texture.DataType);
        Assert.Equal(DxgiFormat.FormatR32G32B32A32Float, texture.PixelFormat);
        Assert.Same(colors, texture.Color4Array);
    }

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(1, 1, 0)]
    [Trait("Category", "Unit")]
    public void TextureRejectsZeroDimensions(int width, int height, int depth) {
        var bytes = new byte[4];

        Assert.ThrowsAny<ArgumentException>(() =>
            new TextureInfo(bytes,
                DxgiFormat.FormatR8Unorm,
                width,
                height,
                depth));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void RequiredEmbeddedResourcesArePresent() {
        var names = typeof(MeshBuilder).Assembly.GetManifestResourceNames();

        Assert.Contains("SilkCore.Resources.arial.dds", names);
        Assert.Contains("SilkCore.Resources.arial.fnt", names);
        Assert.Contains("SilkCore.Resources.DX12.shader-manifest.txt", names);
        Assert.Contains(names, name => name.EndsWith(".dxil", StringComparison.Ordinal));
        Assert.DoesNotContain(names, name => name.EndsWith(".cso", StringComparison.Ordinal));
        Assert.DoesNotContain(names, name => name.Contains('\\'));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void EveryEmbeddedShaderCanBeRead() {
        var assembly = typeof(MeshBuilder).Assembly;
        var names = assembly.GetManifestResourceNames();
        var dxilNames = names.Where(name => name.EndsWith(".dxil", StringComparison.Ordinal))
            .ToArray();

        Assert.Equal(103, dxilNames.Length);
        Assert.All(dxilNames,
            name => {
                using var stream = assembly.GetManifestResourceStream(name)
                                   ?? throw new InvalidOperationException($"Missing shader resource: {name}");
                Assert.True(stream.Length > 0, name);
            });
    }

    /// <summary>
    ///     Provides every explicit DXC declaration from the embedded build manifest.
    /// </summary>
    /// <returns>The source, entry point, profile, and embedded relative path.</returns>
    public static IEnumerable<object[]> DxcShaderDeclarations() => ReadDxcManifest()
        .Select(line => line.Split('|'))
        .Select(parts => new object[] { parts[0], parts[1], parts[2], parts[3] });

    /// <summary>
    ///     Verifies every explicit shader uses a matching SM6.0 profile and resolves to non-empty embedded DXIL.
    /// </summary>
    /// <param name="source">The HLSL source path.</param>
    /// <param name="entryPoint">The shader entry point.</param>
    /// <param name="profile">The explicit DXC profile.</param>
    /// <param name="relativeOutput">The generated relative DXIL path.</param>
    [Theory]
    [MemberData(nameof(DxcShaderDeclarations))]
    [Trait("Category", "Unit")]
    public void DxcDeclarationMatchesEmbeddedDxil(
        string source,
        string entryPoint,
        string profile,
        string relativeOutput
    ) {
        var stage = source[..2].ToLowerInvariant();
        Assert.EndsWith(".hlsl", source, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(entryPoint));
        Assert.Equal($"{stage}_6_0", profile);
        Assert.StartsWith(source[..2] + "\\", relativeOutput, StringComparison.Ordinal);
        Assert.EndsWith($".{entryPoint}.dxil", relativeOutput, StringComparison.Ordinal);

        var resourceName = "SilkCore.Resources.DX12." + relativeOutput.Replace('\\', '.');
        using var stream = typeof(MeshBuilder).Assembly.GetManifestResourceStream(resourceName);
        Assert.NotNull(stream);
        Assert.True(stream.Length > 0, resourceName);
    }

    /// <summary>
    ///     Verifies manifest identities and output paths are unique and cover every embedded DXIL resource.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void DxcManifestIsUniqueAndComplete() {
        var entries = ReadDxcManifest().Select(line => line.Split('|')).ToArray();
        var identities = entries.Select(parts => $"{parts[0]}|{parts[1]}").ToArray();
        var outputs = entries.Select(parts => parts[3]).ToArray();
        var embeddedDxilCount = typeof(MeshBuilder).Assembly.GetManifestResourceNames()
            .Count(name => name.EndsWith(".dxil", StringComparison.Ordinal));

        Assert.Equal(103, entries.Length);
        Assert.Equal(entries.Length, identities.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(entries.Length, outputs.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(entries.Length, embeddedDxilCount);
    }

    /// <summary>
    ///     Reads the embedded explicit-DXC build manifest.
    /// </summary>
    /// <returns>The non-empty manifest lines.</returns>
    private static string[] ReadDxcManifest() {
        var assembly = typeof(MeshBuilder).Assembly;
        using var stream = assembly.GetManifestResourceStream("SilkCore.Resources.DX12.shader-manifest.txt")
                           ?? throw new InvalidOperationException("The embedded DXC shader manifest is missing.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd()
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static MemoryStream CreateDds(byte[] pixels) {
        var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, true)) {
            writer.Write(0x20534444);
            writer.Write(124);
            writer.Write(0x00081007);
            writer.Write(4);
            writer.Write(4);
            writer.Write(8);
            writer.Write(0);
            writer.Write(0);
            for (var i = 0; i < 11; i++) writer.Write(0);
            writer.Write(32);
            writer.Write(4);
            writer.Write(0x31545844);
            for (var i = 0; i < 5; i++) writer.Write(0);
            writer.Write(0x1000);
            for (var i = 0; i < 4; i++) writer.Write(0);
            writer.Write(pixels);
        }

        stream.Position = 0;
        return stream;
    }
}

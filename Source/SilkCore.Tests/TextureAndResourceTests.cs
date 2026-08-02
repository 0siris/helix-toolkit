using System.Runtime.InteropServices;
using System.Text;
using HelixToolkit.SharpDX.Core;
using SharpDX.Toolkit.Graphics;
using Silk.NET.Maths;
using DxgiFormat = Silk.NET.DXGI.Format;

namespace SilkCore.Tests;
public class TextureAndResourceTests {
    [Fact]
    [Trait("Category", "Unit")]
    public void DdsLoaderReadsBc1Pixels() {
        var expected = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        using var stream = CreateDds(expected);
        using var image = Image.Load(stream);
        var actual = new byte[expected.Length];
        Marshal.Copy(image.DataPointer, actual, 0, actual.Length);

        Assert.Equal(4, image.Description.Width);
        Assert.Equal(4, image.Description.Height);
        Assert.Equal((int)DxgiFormat.FormatBC1Unorm, (int)image.Description.Format);
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
        var colors = new[] { new Vector4D<float>(1, 0, 0, 1), new Vector4D<float>(0, 1, 0, 1) };

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
        Assert.Contains(names, name => name.EndsWith(".cso", StringComparison.Ordinal));
        Assert.DoesNotContain(names, name => name.Contains('\\'));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void EveryEmbeddedShaderCanBeRead() {
        var assembly = typeof(MeshBuilder).Assembly;
        var names = assembly.GetManifestResourceNames();
        var csoNames = names.Where(name => name.EndsWith(".cso", StringComparison.Ordinal)).ToArray();
        var dxilNames = names.Where(name => name.EndsWith(".dxil", StringComparison.Ordinal)).ToArray();

        Assert.NotEmpty(csoNames);
        Assert.NotEmpty(dxilNames);
        Assert.All(csoNames.Concat(dxilNames),
                   name => {
                       using var stream = assembly.GetManifestResourceStream(name);
                       Assert.NotNull(stream);
                       Assert.True(stream.Length > 0, name);
                   });
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

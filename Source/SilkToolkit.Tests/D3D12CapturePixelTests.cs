using HelixToolkit.Wpf.SharpDX.Controls;
using Silk.NET.DXGI;
using Xunit;

namespace SilkToolkit.Tests;

/// <summary>Checks explicit readback normalization independently of presentation and native row layout.</summary>
public sealed class D3D12CapturePixelTests {
    /// <summary>Normalizes only the channels and alpha representation actually declared by the source.</summary>
    /// <param name="format">The actual native resource format.</param>
    /// <param name="premultiplied">Whether RGB stores coverage-multiplied values.</param>
    /// <param name="first">The source's first channel.</param>
    /// <param name="green">The source green channel.</param>
    /// <param name="third">The source's third channel.</param>
    /// <param name="alpha">The source coverage.</param>
    /// <param name="blue">The expected straight blue channel.</param>
    /// <param name="expectedGreen">The expected straight green channel.</param>
    /// <param name="red">The expected straight red channel.</param>
    [Theory]
    [InlineData(Format.FormatR8G8B8A8Unorm, false, 10, 20, 30, 40, 30, 20, 10)]
    [InlineData(Format.FormatB8G8R8A8Unorm, false, 10, 20, 30, 40, 10, 20, 30)]
    [InlineData(Format.FormatB8G8R8A8Unorm, true, 16, 32, 64, 128, 32, 64, 128)]
    [InlineData(Format.FormatR8G8B8A8Unorm, true, 64, 32, 16, 128, 32, 64, 128)]
    [InlineData(Format.FormatB8G8R8A8Unorm, true, 1, 0, 1, 1, 255, 0, 255)]
    [InlineData(Format.FormatB8G8R8A8Unorm, true, 40, 80, 120, 0, 0, 0, 0)]
    [InlineData(Format.FormatB8G8R8A8Unorm, true, 30, 60, 90, 255, 30, 60, 90)]
    public void CapturePreservesItsDeclaredPixelContract(Format format, bool premultiplied,
        int first, int green, int third, int alpha, int blue, int expectedGreen, int red) {
        byte[] padded = [99, 99, (byte)first, (byte)green, (byte)third, (byte)alpha, 77, 77];
        var frame = D3D12PresentationSurface.CreateCapturedFrame(padded, 2, 6, 1, 1, format, premultiplied);
        Assert.Equal(new byte[] { (byte)blue, (byte)expectedGreen, (byte)red, (byte)alpha }, frame.Pixels);
        Assert.Equal((uint)1, frame.Width);
        Assert.Equal((uint)1, frame.Height);
        Assert.Equal((byte)99, padded[0]);
        Assert.Equal((byte)first, padded[2]);
    }
}

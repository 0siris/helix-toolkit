using HelixToolkit.Wpf.SharpDX.Render;
using HelixToolkit.Wpf.SharpDX.Utilities;
using NUnit.Framework;
using System.IO;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace HelixToolkit.Wpf.SharpDX.Tests.Core2D
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class D2DAndCaptureTests
    {
        [Test]
        public void DrawsTextAndEncodesBackBuffer()
        {
            using var effectsManager = new DefaultEffectsManager();
            using var buffer = new DX11Texture2DRenderBufferProxy(effectsManager);
            var backBuffer = buffer.Initialize(128, 64, MSAALevel.Disable);
            var context = effectsManager.DeviceContext2D;
            context.Target = buffer.D2DTarget.D2DTarget;

            using (var format = new TextFormat(effectsManager.DirectWriteFactory, "Arial", FontWeight.Bold, FontStyle.Normal, 24))
            using (var layout = new TextLayout(effectsManager.DirectWriteFactory, "Silk.NET", format, 128, 64))
            using (var brush = new SolidColorBrush(context, new Color4(1, 1, 1, 1)))
            {
                context.BeginDraw();
                context.Clear(new Color4(0, 0, 0, 0));
                context.DrawTextLayout(new Vector2(4, 4), layout, brush);
                context.EndDraw();
            }

            effectsManager.NativeDeviceResources.ImmediateContext.Flush();
            using var stream = new MemoryStream();
            Assert.That(ScreenCapture.SaveWICTextureToBitmapStream(
                effectsManager,
                (Texture2D)backBuffer.Resource,
                stream), Is.True);

            var decoder = new BmpBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            var frame = decoder.Frames[0];
            Assert.That(frame.PixelWidth, Is.EqualTo(128));
            Assert.That(frame.PixelHeight, Is.EqualTo(64));

            BitmapSource source = frame.Format == PixelFormats.Bgra32
                ? frame
                : new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0);
            var pixels = new byte[source.PixelWidth * source.PixelHeight * 4];
            source.CopyPixels(pixels, source.PixelWidth * 4, 0);
            Assert.That(pixels, Has.Some.Not.EqualTo((byte)0));
        }

        [Test]
        public void CreatesEncodedTextAndGradientBitmaps()
        {
            using var effectsManager = new DefaultEffectsManager();
            var width = 0f;
            var height = 0f;
            using var text = "Phase 7".ToBitmapStream(
                20,
                new Color4(1, 1, 1, 1),
                new Color4(0, 0, 0, 0),
                "Arial",
                FontWeight.Bold,
                FontStyle.Normal,
                new Vector4(4),
                ref width,
                ref height,
                false,
                effectsManager);
            AssertEncodedPixels(text, (int)width, (int)height);

            using var gradient = BitmapExtensions.CreateLinearGradientBitmapStream(
                effectsManager,
                32,
                16,
                Direct2DImageFormat.Png,
                new Vector2(0, 0),
                new Vector2(32, 0),
                new[]
                {
                    new GradientStop { Position = 0, Color = new Color4(1, 0, 0, 1) },
                    new GradientStop { Position = 1, Color = new Color4(0, 0, 1, 1) }
                });
            AssertEncodedPixels(gradient, 32, 16);
        }

        private static void AssertEncodedPixels(Stream stream, int expectedWidth, int expectedHeight)
        {
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            var frame = decoder.Frames[0];
            Assert.That(frame.PixelWidth, Is.EqualTo(expectedWidth));
            Assert.That(frame.PixelHeight, Is.EqualTo(expectedHeight));
            BitmapSource source = frame.Format == PixelFormats.Bgra32
                ? frame
                : new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0);
            var pixels = new byte[source.PixelWidth * source.PixelHeight * 4];
            source.CopyPixels(pixels, source.PixelWidth * 4, 0);
            Assert.That(pixels, Has.Some.Not.EqualTo((byte)0));
        }
    }
}

using HelixToolkit.Wpf.SharpDX.Render;
using HelixToolkit.Wpf.SharpDX.Utilities;
using NUnit.Framework;
using System.IO;
using System.Threading;
using System.Windows.Media.Imaging;

namespace HelixToolkit.Wpf.SharpDX.Tests.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class DX11ImageSourceTests
    {
        [Test]
        public void OpensSharedTextureAsD3D9BackBuffer()
        {
            using var effectsManager = new DefaultEffectsManager();
            using var buffer = new DX11Texture2DRenderBufferProxy(effectsManager);
            using var imageSource = new DX11ImageSource();

            var backBuffer = buffer.Initialize(40, 30, MSAALevel.Disable);
            for (var i = 0; i < 3; ++i)
            {
                imageSource.SetRenderTargetDX11((Texture2D)backBuffer.Resource);

                Assert.That(imageSource.PixelWidth, Is.EqualTo(40));
                Assert.That(imageSource.PixelHeight, Is.EqualTo(30));
                Assert.That(imageSource.IsDeviceStateOk(), Is.True);
                Assert.DoesNotThrow(imageSource.InvalidateD3DImage);

                imageSource.SetRenderTargetDX11(null);
            }

            using var stream = new MemoryStream();
            Assert.That(ScreenCapture.SaveWICTextureToBitmapStream(
                effectsManager,
                (Texture2D)backBuffer.Resource,
                stream), Is.True);
            var frame = new BmpBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad).Frames[0];
            Assert.That(frame.PixelWidth, Is.EqualTo(40));
            Assert.That(frame.PixelHeight, Is.EqualTo(30));
        }
    }
}

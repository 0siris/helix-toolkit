using HelixToolkit.Wpf.SharpDX.Render;
using HelixToolkit.Wpf.SharpDX.Utilities;
using NUnit.Framework;
using System.IO;
using System.Threading;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace HelixToolkit.Wpf.SharpDX.Tests.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class SwapChainRenderBufferTests
    {
        [Test]
        public void CreatesResizesAndPresentsNativeSwapChain()
        {
            using var window = new HwndSource(new HwndSourceParameters("SwapChainRenderBufferTests")
            {
                Width = 32,
                Height = 24
            });
            using var effectsManager = new DefaultEffectsManager();
            using var buffer = new DX11SwapChainRenderBufferProxy(window.Handle, effectsManager);

            var backBuffer = buffer.Initialize(32, 24, MSAALevel.Disable);

            Assert.That(backBuffer.Resource.NativePointer, Is.Not.EqualTo(System.IntPtr.Zero));
            Assert.That(buffer.Present(), Is.True);

            for (var i = 1; i <= 3; ++i)
            {
                var width = 32 + i * 16;
                var height = 24 + i * 12;
                backBuffer = buffer.Resize(width, height);

                Assert.That(backBuffer.Resource.NativePointer, Is.Not.EqualTo(System.IntPtr.Zero));
                Assert.That(buffer.SwapChain.Description1.Width, Is.EqualTo(width));
                Assert.That(buffer.SwapChain.Description1.Height, Is.EqualTo(height));
                Assert.That(buffer.Present(), Is.True);
            }

            using var stream = new MemoryStream();
            Assert.That(ScreenCapture.SaveWICTextureToBitmapStream(
                effectsManager,
                (Texture2D)backBuffer.Resource,
                stream), Is.True);
            var frame = new BmpBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad).Frames[0];
            Assert.That(frame.PixelWidth, Is.EqualTo(80));
            Assert.That(frame.PixelHeight, Is.EqualTo(60));
        }
    }
}

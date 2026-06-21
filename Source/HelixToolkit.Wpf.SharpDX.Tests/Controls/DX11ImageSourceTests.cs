using HelixToolkit.Wpf.SharpDX.Render;
using NUnit.Framework;
using System.Threading;

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
        }
    }
}

using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Utilities;
using Xunit;
using Color4 = Silk.NET.Maths.Vector4D<float>;

namespace SilkToolkit.Tests;
[Collection(WpfCollection.Name)]
public sealed class WarpRenderTests {
    [Fact]
    [Trait("Category", "Warp")]
    public Task ClearsAndCapturesOffscreenTarget() {
        return StaThread.RunAsync(() => {
            using var effectsManager = CreateWarpEffectsManager();
            using var buffer = new DX11Texture2DRenderBufferProxy(effectsManager);
            var backBuffer = buffer.Initialize(64, 32, MSAALevel.Disable);
            var context = effectsManager.DeviceContext2D;
            context.Target = buffer.D2DTarget.D2DTarget;

            context.BeginDraw();
            context.Clear(new Color4(0.25f, 0.5f, 0.75f, 1));
            context.EndDraw();
            effectsManager.NativeDeviceResources.ImmediateContext.Flush();

            using var stream = new MemoryStream();
            Assert.True(ScreenCapture.SaveWICTextureToBitmapStream(effectsManager,
                                                                   (Texture2D)backBuffer.Resource,
                                                                   stream));

            var frame = new BmpBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad).Frames[0];
            Assert.Equal(64, frame.PixelWidth);
            Assert.Equal(32, frame.PixelHeight);
            BitmapSource source = frame.Format == PixelFormats.Bgra32
                                      ? frame
                                      : new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0);
            var pixel = new byte[4];
            source.CopyPixels(new Int32Rect(0, 0, 1, 1), pixel, 4, 0);
            Assert.Contains(pixel, value => value != 0);
        });
    }

    [Fact]
    [Trait("Category", "Warp")]
    [Trait("Category", "Wpf")]
    public Task ResizesPresentsAndCapturesHwndSwapChain() {
        return StaThread.RunAsync(() => {
            using var window = new HwndSource(new HwndSourceParameters(nameof(WarpRenderTests)) {
                Width = 32,
                Height = 24,
                WindowStyle = unchecked((int)0x80000000)
            });
            using var effectsManager = CreateWarpEffectsManager();
            using var buffer = new DX11SwapChainRenderBufferProxy(window.Handle, effectsManager);

            var backBuffer = buffer.Initialize(32, 24, MSAALevel.Disable);
            Assert.NotEqual(IntPtr.Zero, backBuffer.Resource.NativePointer);
            Assert.True(buffer.Present());

            backBuffer = buffer.Resize(80, 60);
            Assert.Equal(80, buffer.SwapChain.Description1.Width);
            Assert.Equal(60, buffer.SwapChain.Description1.Height);
            Assert.True(buffer.Present());

            using var stream = new MemoryStream();
            Assert.True(ScreenCapture.SaveWICTextureToBitmapStream(effectsManager,
                                                                   (Texture2D)backBuffer.Resource,
                                                                   stream));
            var frame = new BmpBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad).Frames[0];
            Assert.Equal(80, frame.PixelWidth);
            Assert.Equal(60, frame.PixelHeight);
        });
    }

    private static DefaultEffectsManager CreateWarpEffectsManager() {
        return new DefaultEffectsManager(new EffectsManagerConfiguration {
            EnableSoftwareRendering = true
        });
    }
}

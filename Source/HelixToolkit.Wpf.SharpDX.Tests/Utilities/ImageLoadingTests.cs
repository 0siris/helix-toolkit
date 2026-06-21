extern alias WpfAssembly;

using NUnit.Framework;
using System.IO;
using System.Runtime.InteropServices;
using Image = WpfAssembly::SharpDX.Toolkit.Graphics.Image;

namespace HelixToolkit.Wpf.SharpDX.Tests.Utilities
{
    [TestFixture]
    public class ImageLoadingTests
    {
        [Test]
        public void LoadDecodesBmpToBgra32()
        {
            using (var stream = CreateBitmap())
            using (var image = Image.Load(stream))
            {
                Assert.That(image, Is.Not.Null);
                Assert.That(image.Description.Width, Is.EqualTo(1));
                Assert.That(image.Description.Height, Is.EqualTo(1));
                Assert.That((int)image.Description.Format, Is.EqualTo((int)Silk.NET.DXGI.Format.FormatB8G8R8A8Unorm));
                Assert.That(Marshal.ReadInt32(image.DataPointer), Is.EqualTo(unchecked((int)0xff0a141e)));
            }
        }

        private static MemoryStream CreateBitmap()
        {
            var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
            {
                writer.Write((byte)'B');
                writer.Write((byte)'M');
                writer.Write(58);
                writer.Write(0);
                writer.Write(54);
                writer.Write(40);
                writer.Write(1);
                writer.Write(-1);
                writer.Write((short)1);
                writer.Write((short)32);
                writer.Write(0);
                writer.Write(4);
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);
                writer.Write((byte)30);
                writer.Write((byte)20);
                writer.Write((byte)10);
                writer.Write((byte)255);
            }
            stream.Position = 0;
            return stream;
        }
    }
}

using NUnit.Framework;
using SharpDX.Toolkit.Graphics;
using System.IO;
using System.Runtime.InteropServices;

namespace HelixToolkit.SharpDX.Core.Tests
{
    [TestFixture]
    public class ImageLoadingTests
    {
        [Test]
        public void LoadReadsDxt1Dds()
        {
            var expected = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            using (var stream = CreateDds(expected))
            using (var image = Image.Load(stream))
            {
                var actual = new byte[expected.Length];
                Marshal.Copy(image.DataPointer, actual, 0, actual.Length);

                Assert.That(image.Description.Width, Is.EqualTo(4));
                Assert.That(image.Description.Height, Is.EqualTo(4));
                Assert.That((int)image.Description.Format, Is.EqualTo((int)Silk.NET.DXGI.Format.FormatBC1Unorm));
                Assert.That(actual, Is.EqualTo(expected));
            }
        }

        private static MemoryStream CreateDds(byte[] pixels)
        {
            var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
            {
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
}

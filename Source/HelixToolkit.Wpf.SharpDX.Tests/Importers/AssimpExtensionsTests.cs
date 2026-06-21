extern alias WpfAssimpAssembly;

using Assimp;
using NUnit.Framework;
using AssimpExtensions = WpfAssimpAssembly::HelixToolkit.Wpf.SharpDX.Assimp.Extensions;

namespace HelixToolkit.Wpf.SharpDX.Tests.Importers
{
    [TestFixture]
    public class AssimpExtensionsTests
    {
        [Test]
        public void ConvertsMatrixAndColorToSilkTypes()
        {
            var matrix = new Matrix4x4(
                1, 2, 3, 4,
                5, 6, 7, 8,
                9, 10, 11, 12,
                13, 14, 15, 16);

            var converted = AssimpExtensions.ToSharpDXMatrix(matrix, true);
            var color = AssimpExtensions.ToAssimpColor4D(new Color4(0.1f, 0.2f, 0.3f, 0.4f), 0.4f);

            Assert.That(converted.M12, Is.EqualTo(5));
            Assert.That(converted.M21, Is.EqualTo(2));
            Assert.That(color.A, Is.EqualTo(0.4f));
        }
    }
}

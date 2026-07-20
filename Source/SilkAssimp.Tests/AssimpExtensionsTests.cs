using Assimp;
using HelixToolkit.SharpDX.Core.Assimp;
using Silk.NET.Maths;
using Xunit;

namespace SilkAssimp.Tests {
    public class AssimpExtensionsTests {
        [Fact]
        [Trait("Category", "Unit")]
        public void MatrixAndColorConversionsPreserveComponents() {
            var matrix = new Matrix4x4(1,
                                       2,
                                       3,
                                       4,
                                       5,
                                       6,
                                       7,
                                       8,
                                       9,
                                       10,
                                       11,
                                       12,
                                       13,
                                       14,
                                       15,
                                       16);

            var converted = matrix.ToSharpDXMatrix(true);
            var color = new Vector4D<float>(0.1f, 0.2f, 0.3f, 0.4f).ToAssimpColor4D(0.4f);

            Assert.Equal(5, converted.M12);
            Assert.Equal(2, converted.M21);
            Assert.Equal(0.4f, color.A);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void VectorAndQuaternionConversionsRoundTrip() {
            var vector = new Vector3D<float>(1.5f, -2, 3.25f);
            var quaternion = new Quaternion<float>(0.1f, 0.2f, 0.3f, 0.9f);

            Assert.Equal(vector, vector.ToAssimpVector3D().ToSharpDXVector3());
            Assert.Equal(quaternion, quaternion.ToAssimpQuaternion().ToSharpDXQuaternion());
        }
    }
}

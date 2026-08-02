using HelixToolkit.SharpDX.Core;
using Silk.NET.Maths;

namespace SilkCore.Tests;
public class MeshBuilderTests {
    [Fact]
    [Trait("Category", "Unit")]
    public void TriangleProducesExpectedNormalsAndIndices() {
        var builder = new MeshBuilder();

        builder.AddTriangle(new Vector3D<float>(0, 0, 0),
                            new Vector3D<float>(1, 0, 0),
                            new Vector3D<float>(1, 1, 0));

        Assert.Equal(3, builder.Positions.Count);
        Assert.Equal([0, 1, 2], builder.TriangleIndices);
        Assert.All(builder.Normals, normal => Assert.Equal(new Vector3D<float>(0, 0, 1), normal));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void NormalsCanBeComputedForPolygon() {
        var builder = new MeshBuilder(false);
        builder.AddPolygon([
            new Vector3D<float>(0, 0, 0),
            new Vector3D<float>(7, 0, 0),
            new Vector3D<float>(7, 0, 7)
        ]);

        Assert.False(builder.HasNormals);

        builder.ComputeNormalsAndTangents(MeshFaces.Default);

        Assert.True(builder.HasNormals);
        Assert.Equal(3, builder.Normals.Count);
    }

    [Theory]
    [InlineData(BoxFaces.PositiveX, 4, 6)]
    [InlineData(BoxFaces.All, 24, 36)]
    [Trait("Category", "Unit")]
    public void BoxFacesControlGeneratedGeometry(BoxFaces faces, int vertices, int indices) {
        var builder = new MeshBuilder();

        builder.AddBox(new Vector3D<float>(0), 2, 4, 6, faces);

        Assert.Equal(vertices, builder.Positions.Count);
        Assert.Equal(indices, builder.TriangleIndices.Count);
        Assert.Equal(vertices, builder.Normals.Count);
        Assert.Equal(vertices, builder.TextureCoordinates.Count);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AppendPreservesBothMeshes() {
        var first = new MeshBuilder();
        first.AddTriangle(new Vector3D<float>(0, 0, 0), new Vector3D<float>(1, 0, 0), new Vector3D<float>(0, 1, 0));
        var second = new MeshBuilder();
        second.AddTriangle(new Vector3D<float>(0, 0, 1),
                           new Vector3D<float>(1, 0, 1),
                           new Vector3D<float>(0, 1, 1));

        first.Append(second);

        Assert.Equal(6, first.Positions.Count);
        Assert.Equal([0, 1, 2, 3, 4, 5], first.TriangleIndices);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ToMeshUpdatesBounds() {
        var builder = new MeshBuilder();
        builder.AddBox(new Vector3D<float>(2, 3, 4), 2, 4, 6);

        var mesh = builder.ToMesh();

        Assert.Equal(new Vector3D<float>(1, 1, 1), mesh.Bound.Minimum);
        Assert.Equal(new Vector3D<float>(3, 5, 7), mesh.Bound.Maximum);
        Assert.Equal(24, mesh.Positions.Count);
        Assert.Equal(36, mesh.Indices.Count);
    }
}

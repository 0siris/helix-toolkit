using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Extensions;
using HelixToolkit.SharpDX.Core.Geometry;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using Silk.NET.Maths;

namespace SilkCore.Tests;

public class GeometryContractTests {
    [Fact]
    [Trait("Category", "Unit")]
    public void PolygonTriangulatesConvexContour() {
        var polygon = new Polygon {
            Points = [
                new Vector2D<float>(0, 0), new Vector2D<float>(2, 0), new Vector2D<float>(2, 2),
                new Vector2D<float>(0, 2)
            ]
        };

        var triangles = polygon.Triangulate()
                        ?? throw new InvalidOperationException("Triangulation returned no result.");

        Assert.Equal(6, triangles.Count);
        Assert.All(triangles, index => Assert.InRange(index, 0, 3));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Polygon3DNormalAndFlattenPreservePlanarShape() {
        var polygon = new Polygon3D([
            new Vector3D<float>(0, 0, 2),
            new Vector3D<float>(2, 0, 2),
            new Vector3D<float>(2, 2, 2),
            new Vector3D<float>(0, 2, 2)
        ]);

        var normal = polygon.GetNormal();
        var flattened = polygon.Flatten();

        Assert.True(polygon.IsPlanar());
        Assert.Equal(1f, normal.Length, 5);
        Assert.Equal(4, flattened.Points.Count);
        var triangles = flattened.Triangulate()
                        ?? throw new InvalidOperationException("Triangulation returned no result.");
        Assert.Equal(6, triangles.Count);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Polygon3DRejectsTooFewPoints() {
        var polygon = new Polygon3D([new Vector3D<float>(0), new Vector3D<float>(1)]);

        Assert.Throws<InvalidOperationException>(() => polygon.GetNormal());
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void BoundingBoxFromPointsPadsDegenerateAxis() {
        var box = BoundingBoxExtensions.FromPoints([
            new Vector3D<float>(-2, 1, 4),
            new Vector3D<float>(2, 1, 8)
        ]);

        Assert.Equal(new Vector3D<float>(-2.1f, 0.9f, 3.9f), box.Minimum);
        Assert.Equal(new Vector3D<float>(2.1f, 1.1f, 8.1f), box.Maximum);
        Assert.Equal(new Vector3D<float>(0, 1, 6), box.Center());
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void BoundingSphereContainsPointsAndIntersectsRay() {
        var sphere = BoundingSphereExtensions.FromPoints([
            new Vector3D<float>(-2, 0, 0),
            new Vector3D<float>(2, 0, 0)
        ]);
        var hit = new Ray(new Vector3D<float>(-4, 0, 0), Vector3D<float>.UnitX);
        var miss = new Ray(new Vector3D<float>(-4, 3, 0), Vector3D<float>.UnitX);

        Assert.Equal(new Vector3D<float>(0), sphere.Center);
        Assert.Equal(2f, sphere.Radius);
        Assert.True(sphere.Intersects(ref hit));
        Assert.False(sphere.Intersects(ref miss));
        Assert.Equal(ContainmentType.Contains,
            BoundingSphereExtensions.Contains(sphere, new Vector3D<float>(1, 0, 0)));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void EmptyMeshCanBeClearedRepeatedly() {
        var mesh = new MeshGeometry3D();

        mesh.ClearAllGeometryData();
        mesh.ClearAllGeometryData();

        Assert.Null(mesh.Positions);
        Assert.Null(mesh.Indices);
        Assert.True(mesh.OctreeDirty);
    }
}
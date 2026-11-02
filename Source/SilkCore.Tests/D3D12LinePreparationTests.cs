using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Native;
using Silk.NET.Maths;

namespace SilkCore.Tests;

/// <summary>Checks line preparation without a device, command queue or viewport.</summary>
public sealed class D3D12LinePreparationTests {
    /// <summary>Zero, one and two markers retain the builder's actual segment topology.</summary>
    /// <param name="pointCount">The number of annotation markers.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void OpenAnnotationUsesOnlyExistingSegments(int pointCount) {
        var builder = new LineBuilder();
        var points = Enumerable.Range(0, pointCount).Select(index => new Vector3D<float>(index, 0, 0)).ToArray();
        builder.Add(false, points);
        using var model = new DefaultLineGeometryBufferModel { Geometry = builder.ToLineGeometry3D() };

        var (vertices, indices) = SilkD3D12PointLineBuffers.Prepare(model);

        Assert.Equal(pointCount < 2 ? 0 : 2, vertices.Length);
        Assert.Equal(pointCount < 2 ? [] : new[] { 0, 1 }, indices);
    }

    /// <summary>A cleared line cannot fall back to a non-indexed draw using leftover positions.</summary>
    [Fact]
    public void PopulatedEmptyAndRepopulatedGeometryPreservesIndices() {
        var geometry = new LineGeometry3D {
            Positions = new Vector3Collection([Vector3D<float>.Zero, Vector3D<float>.UnitX,
                Vector3D<float>.UnitY, -Vector3D<float>.UnitX]),
            Indices = new IntCollection([0, 1, 2, 3])
        };
        using var model = new DefaultLineGeometryBufferModel { Geometry = geometry };
        var cross = SilkD3D12PointLineBuffers.Prepare(model);
        geometry.Indices = [];
        var empty = SilkD3D12PointLineBuffers.Prepare(model);
        geometry.Indices = new IntCollection([3, 1]);
        var restored = SilkD3D12PointLineBuffers.Prepare(model);

        Assert.Equal(new[] { 0, 1, 2, 3 }, cross.Indices);
        Assert.Empty(empty.Vertices);
        Assert.Empty(empty.Indices);
        Assert.Equal(4, restored.Vertices.Length);
        Assert.Equal(new[] { 3, 1 }, restored.Indices);
    }

    /// <summary>Absent index and position collections describe an empty temporary line.</summary>
    [Fact]
    public void DefaultGeometryHasNoSegments() {
        using var model = new DefaultLineGeometryBufferModel { Geometry = new LineGeometry3D() };
        var prepared = SilkD3D12PointLineBuffers.Prepare(model);
        Assert.Empty(prepared.Vertices);
        Assert.Empty(prepared.Indices);
    }

    /// <summary>Actual invalid indices still fail instead of becoming empty or implicit lines.</summary>
    /// <param name="indices">The malformed endpoint indices.</param>
    [Theory]
    [InlineData(new[] { 0, 2 })]
    [InlineData(new[] { -1, 0 })]
    [InlineData(new[] { 0 })]
    public void RejectsInvalidEndpointIndices(int[] indices) {
        using var model = new DefaultLineGeometryBufferModel {
            Geometry = new LineGeometry3D {
                Positions = new Vector3Collection([Vector3D<float>.Zero, Vector3D<float>.UnitX]),
                Indices = new IntCollection(indices)
            }
        };
        Assert.Throws<ArgumentException>(() => SilkD3D12PointLineBuffers.Prepare(model));
    }

    /// <summary>Indices without positions remain invalid, as does a missing geometry model.</summary>
    [Fact]
    public void RejectsMissingGeometryAndIndexedGeometryWithoutPositions() {
        using var model = new DefaultLineGeometryBufferModel();
        Assert.Throws<InvalidOperationException>(() => SilkD3D12PointLineBuffers.Prepare(model));
        model.Geometry = new LineGeometry3D { Indices = new IntCollection([0, 1]) };
        Assert.Throws<InvalidOperationException>(() => SilkD3D12PointLineBuffers.Prepare(model));
    }
}

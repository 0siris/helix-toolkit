using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model.Scene;
using Silk.NET.Maths;

namespace SilkCore.Tests;

public class SceneNodeTransformTests {
    [Fact]
    [Trait("Category", "Unit")]
    public void NestedTransformAppliesChildBeforeParent() {
        using var parent = new GroupNode();
        using var child = new GroupNode();
        parent.ModelMatrix = SilkMath.RotationZ(MathF.PI / 2);
        child.ModelMatrix = SilkMath.Translation(new Vector3D<float>(1, 0, 0));
        Assert.True(parent.AddChildNode(child));

        parent.ComputeTransformMatrix();
        child.ComputeTransformMatrix();

        // Row-vector convention: world = local * parent, so the +X offset rotated by +90 degrees
        // about Z lands on +Y.
        var world = child.TotalModelMatrix;
        Assert.Equal(0, world.M41, 5);
        Assert.Equal(1, world.M42, 5);
        Assert.Equal(0, world.M43, 5);
    }
}

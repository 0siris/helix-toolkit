using HelixToolkit.SharpDX.Core.Model.Scene2D;
using Xunit;

namespace HelixToolkit.SharpDX.Core.Model.Scene2D.Abstract;

/// <summary>Exercises actual attached nodes without replacing their invalidation or attachment behavior.</summary>
public sealed class SceneNode2DInvalidationTests {
    /// <summary>Verifies attached invalidation returns and marks both the node and its existing parent dirty.</summary>
    /// <param name="route">The public invalidation entry point to exercise.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void AttachedInvalidationPreservesAncestorDirtyStateTest(int route) {
        using var parent = new PanelNode2D();
        using var child = new PathNode2D();
        var added = parent.AddChildNode(child);
        parent.Attach(1.5f);
        parent.IsVisualDirty = false;
        child.IsVisualDirty = false;

        switch (route) {
            case 0: child.InvalidateVisual(); break;
            case 1: child.InvalidateRender(); break;
            case 2: child.InvalidateMeasure(); break;
            case 3: child.InvalidateArrange(); break;
            case 4: child.InvalidateTransform(); break;
            case 5: child.InvalidateAll(); break;
        }

        Assert.True(added);
        Assert.True(parent.IsAttached);
        Assert.True(child.IsAttached);
        Assert.True(parent.IsVisualDirty);
        Assert.True(child.IsVisualDirty);
        Assert.Same(parent, child.Parent);
        Assert.Equal(1.5f, child.DpiScale);
        parent.Detach();
    }
}

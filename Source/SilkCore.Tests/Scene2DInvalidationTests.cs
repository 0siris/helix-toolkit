using HelixToolkit.SharpDX.Core.Model.Scene2D;

namespace SilkCore.Tests;

/// <summary>Verifies attached 2D invalidation remains bounded and propagates repaint state.</summary>
public class Scene2DInvalidationTests {
    /// <summary>Exercises the startup attachment path and both invalidation entry points without a device.</summary>
    /// <param name="visual">Whether to use layout visual invalidation rather than direct render invalidation.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Unit")]
    public void AttachedChildInvalidatesAncestorsWithoutRecursion(bool visual) {
        using var root = new PanelNode2D();
        using var parent = new PanelNode2D();
        using var child = new PanelNode2D();
        root.AddChildNode(parent);
        root.Attach();
        parent.AddChildNode(child);
        Assert.True(child.IsAttached);
        root.IsVisualDirty = false;
        parent.IsVisualDirty = false;
        child.IsVisualDirty = false;

        if (visual) child.InvalidateVisual();
        else child.InvalidateRender();

        Assert.True(child.IsVisualDirty);
        Assert.True(parent.IsVisualDirty);
        Assert.True(root.IsVisualDirty);
        child.InvalidateAll();
        root.Detach();
        Assert.False(child.IsAttached);
    }
}

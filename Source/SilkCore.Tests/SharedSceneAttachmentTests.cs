using HelixToolkit.SharpDX.Core.Model.Scene;

namespace SilkCore.Tests;

/// <summary>Checks native attachment ownership independently of presentation, device and manager ownership.</summary>
public sealed class SharedSceneAttachmentTests {
    /// <summary>Notifies graph observers after removing null-manager native leases without detaching their cores.</summary>
    /// <param name="clear">Whether to clear the hierarchy instead of removing its single child.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NullManagerLeasedHierarchyMutationInvalidates(bool clear) {
        using var root = new GroupNode();
        using var child = new GroupNode();
        root.AddChildNode(child);
        var owner = new object();
        root.RetainNativeAttachment(owner, null);
        child.RetainNativeAttachment(owner, null);
        Assert.True(root.Attach());
        Assert.True(child.Attach());
        Assert.False(root.IsAttached);
        Assert.False(child.IsAttached);
        var notifications = 0;
        EventHandler<InvalidateTypes> invalidated = (_, kind) => {
            Assert.Equal(InvalidateTypes.SceneGraph, kind);
            notifications++;
        };
        root.Invalidated += invalidated;

        if (clear) root.Clear();
        else Assert.True(root.RemoveChildNode(child));

        Assert.Equal(1, notifications);
        Assert.Empty(root.Items);
        Assert.Null(child.Parent);
        Assert.True(child.RenderCore.IsAttached);
        Assert.False(root.RemoveChildNode(child));
        Assert.Equal(1, notifications);
        root.Invalidated -= invalidated;
        child.ReleaseNativeAttachment(owner);
        root.ReleaseNativeAttachment(owner);
        Assert.False(child.RenderCore.IsAttached);
        Assert.False(root.RenderCore.IsAttached);
    }

    /// <summary>Retains lease capacity and root notification traversal after warming actual core attachment.</summary>
    [Fact]
    public void WarmLeaseRefreshAndAncestorInvalidationDoNotAllocate() {
        Assert.Equal("false", AppContext.GetData("System.GC.Concurrent")?.ToString(), ignoreCase: true);
        using var root = new GroupNode();
        using var child = new GroupNode();
        root.AddChildNode(child);
        var notifications = 0;
        root.Invalidated += (_, _) => notifications++;
        var first = new object();
        var second = new object();
        child.RetainNativeAttachment(first, null);
        Assert.True(child.Attach());
        for (var index = 0; index < 64; index++) {
            child.RetainNativeAttachment(second, null);
            child.ClearNativeRenderHost(second);
            child.InvalidateRender();
            child.ReleaseNativeAttachment(second);
        }

        notifications = 0;
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 1_000; index++) {
            child.RetainNativeAttachment(second, null);
            child.ClearNativeRenderHost(second);
            child.InvalidateRender();
            child.ReleaseNativeAttachment(second);
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, allocated);
        Assert.Equal(1_000, notifications);
        Assert.True(child.RenderCore.IsAttached);
        Assert.False(child.HasOtherNativeAttachments(first));
        child.ReleaseNativeAttachment(first);
        Assert.False(child.RenderCore.IsAttached);
    }

    /// <summary>Retains a real render core through either release order, including consumers without viewport hosts.</summary>
    /// <param name="releaseLatest">Whether the most recently registered consumer releases first.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NullHostConsumersDetachOnlyAtLastRelease(bool releaseLatest) {
        using var node = new GroupNode();
        var first = new object();
        var second = new object();
        node.RetainNativeAttachment(first, null);
        Assert.True(node.Attach());
        node.RetainNativeAttachment(second, null);
        node.RetainNativeAttachment(second, null);
        var leaving = releaseLatest ? second : first;
        var surviving = releaseLatest ? first : second;
        node.ReleaseNativeAttachment(leaving);
        node.ReleaseNativeAttachment(leaving);
        Assert.True(node.RenderCore.IsAttached);
        Assert.False(node.IsDisposed);
        node.ReleaseNativeAttachment(surviving);
        Assert.False(node.RenderCore.IsAttached);
        Assert.Null(node.RenderHost);
        Assert.False(node.IsDisposed);
    }

    /// <summary>Preserves active descendant bindings when an enclosing attachment owner releases its own hierarchy.</summary>
    [Fact]
    public void HierarchyDetachDefersLeasedDescendants() {
        using var root = new GroupNode();
        using var child = new GroupNode();
        root.AddChildNode(child);
        var owner = new object();
        child.RetainNativeAttachment(owner, null);
        Assert.True(child.Attach());
        root.Clear();
        Assert.True(child.RenderCore.IsAttached);
        Assert.Null(child.Parent);
        child.ReleaseNativeAttachment(owner);
        Assert.False(child.RenderCore.IsAttached);
    }

    /// <summary>Retains root observers through a temporary native generation release and reattachment.</summary>
    [Fact]
    public void RootInvalidationSubscriptionSurvivesTemporaryDetach() {
        using var root = new GroupNode();
        using var child = new GroupNode();
        root.AddChildNode(child);
        var notifications = 0;
        root.Invalidated += (_, _) => notifications++;
        Assert.True(root.Attach());
        root.Detach();
        notifications = 0;
        child.InvalidateRender();
        Assert.Equal(1, notifications);
        Assert.True(root.Attach());
        notifications = 0;
        child.InvalidateRender();
        Assert.Equal(1, notifications);
    }
}

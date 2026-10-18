using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Controls;
using Xunit;

namespace SilkToolkit.Tests;

/// <summary>Verifies viewport lifetime notification without a window, template, device or ownership override.</summary>
[Collection(WpfCollection.Name)]
public sealed class ViewportDisposalTests {
    /// <summary>Notifies once and completes ownership-sensitive cleanup even when a listener throws or reenters.</summary>
    /// <param name="belongsToParent">Whether the parent window owns the scene resources.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public Task DisposalNotifiesEveryListenerAndPreservesCleanup(bool belongsToParent) => StaThread.RunAsync(() => {
        using var viewport = new Viewport3DX { BelongsToParentWindow = belongsToParent };
        using var item = new SceneNodeGroupModel3D();
        _ = item.SceneNode;
        viewport.Items.Add(item);
        var notifications = 0;
        var failure = new InvalidOperationException("Listener failed");
        viewport.Disposing += (_, _) => {
            Assert.True(viewport.IsDisposed);
            viewport.Dispose();
            throw failure;
        };
        viewport.Disposing += (_, _) => notifications++;
        var actual = Assert.Throws<InvalidOperationException>(() => viewport.Dispose());
        Assert.Same(failure, actual);
        Assert.Equal(1, notifications);
        Assert.True(viewport.IsDisposed);
        Assert.Equal(!belongsToParent, item.SceneNode.IsDisposed);
        Assert.Equal(belongsToParent ? 1 : 0, viewport.Items.Count);
        viewport.Dispose();
        Assert.Equal(1, notifications);
    });
}

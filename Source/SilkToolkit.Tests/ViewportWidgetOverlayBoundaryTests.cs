using System.Windows;
using System.Windows.Controls;
using MeshDesktop.UI.Controls;
using Xunit;

namespace SilkToolkit.Tests;

/// <summary>
///     Verifies the shared overlay's managed layout and lifetime without opening a window or popup.
/// </summary>
[Collection(WpfCollection.Name)]
public sealed class ViewportWidgetOverlayBoundaryTests
{
    /// <summary>
    ///     Verifies bounded layout, logical data inheritance and repeatable cleanup before native presentation.
    /// </summary>
    /// <returns>The completion of the headless STA check.</returns>
    [Fact]
    [Trait("Category", "Wpf")]
    public Task LayoutInheritanceAndDisposeRemainHeadlessTest() => StaThread.RunAsync(() =>
    {
        var state = new object();
        var widget = new Border { Width = 80, Height = 32 };
        var owner = new Window { DataContext = state };
        using var overlay = new ViewportWidgetOverlay(owner, widget);
        owner.Content = overlay;
        try
        {
            var availableSize = new Size(120, 60);
            var widgetSize = new Size(80, 32);
            overlay.Measure(availableSize);
            overlay.Arrange(new Rect(widgetSize));
            overlay.RefreshPlacement();
            var logicalChildren = LogicalTreeHelper.GetChildren(overlay).Cast<object>().ToArray();

            Assert.Equal(widgetSize, overlay.DesiredSize);
            Assert.Equal(widgetSize, overlay.RenderSize);
            Assert.Equal(widgetSize, widget.RenderSize);
            Assert.Single(logicalChildren);
            Assert.Same(state, widget.DataContext);
            Assert.False(overlay.IsOpen);
            Assert.True(overlay.ScreenBounds.IsEmpty);
            Assert.True(overlay.VisibleScreenBounds.IsEmpty);
            Assert.Null(PresentationSource.FromVisual(widget));

            overlay.Dispose();
            overlay.Dispose();
            overlay.RefreshPlacement();
            var remainingChildren = LogicalTreeHelper.GetChildren(overlay).Cast<object>().ToArray();

            Assert.Empty(remainingChildren);
            Assert.Null(widget.Parent);
            Assert.False(overlay.IsOpen);
            Assert.Null(PresentationSource.FromVisual(widget));
        }
        finally
        {
            owner.Close();
        }
    });
}

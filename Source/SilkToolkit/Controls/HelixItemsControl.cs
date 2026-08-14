using System.Windows;
using System.Windows.Controls;

namespace HelixToolkit.Wpf.SharpDX.Controls;
public class HelixItemsControl : ItemsControl {
    public HelixItemsControl() {
        Focusable = false;
        Visibility = Visibility.Collapsed;

        IsHitTestVisible = false;
        DefaultStyleKey = typeof(HelixItemsControl);
    }

    protected override Size ArrangeOverride(Size finalSize) => new();

    protected override Size MeasureOverride(Size availableSize) => new();
}

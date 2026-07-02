using System.Windows;
using System.Windows.Controls;

namespace HelixToolkit.Wpf.SharpDX
{
    namespace Controls
    {
        public class HelixItemsControl : ItemsControl
        {
            public HelixItemsControl()
            {
                Focusable = false;
                Visibility = Visibility.Collapsed;

                IsHitTestVisible = false;
                DefaultStyleKey = typeof(HelixItemsControl);
            }

            protected override Size ArrangeOverride(Size finalSize)
            {
                return new Size();
            }

            protected override Size MeasureOverride(Size availableSize)
            {
                return new Size();
            }
        }
    }
}
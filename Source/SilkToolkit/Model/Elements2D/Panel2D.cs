using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using HelixToolkit.SharpDX.Core.Model.Scene2D;
using WpfBrush = System.Windows.Media.Brush;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace HelixToolkit.Wpf.SharpDX {
    namespace Elements2D {
        [ContentProperty("Children")]
        public class Panel2D : Element2D {
            public static readonly DependencyProperty BackgroundProperty =
                DependencyProperty.Register("Background",
                                            typeof(WpfBrush),
                                            typeof(Panel2D),
                                            new PropertyMetadata(new WpfSolidColorBrush(Colors.Transparent)));

            public Panel2D() {
                Children.CollectionChanged += Items_CollectionChanged;
                EnableBitmapCache = false;
            }

            public WpfBrush Background {
                get => (WpfBrush) GetValue(BackgroundProperty);
                set => SetValue(BackgroundProperty, value);
            }

            public ObservableCollection<Element2D> Children { get; } = new();

            private void Items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
                if (e.OldItems != null) DetachChildren(e.OldItems);

                if (e.Action == NotifyCollectionChangedAction.Reset) {
                    foreach (var item in SceneNode.Items) RemoveLogicalChild(item.WrapperSource);
                    (SceneNode as PanelNode2D).Clear();
                    if (IsAttached) AttachChildren(sender as IEnumerable);
                } else if (e.NewItems != null && IsAttached) {
                    AttachChildren(e.NewItems);
                }

                InvalidateRender();
            }

            protected void AttachChildren(IEnumerable children) {
                var s = SceneNode as PanelNode2D;
                foreach (Element2D c in children) {
                    if (c.Parent == null) AddLogicalChild(c);
                    s.AddChildNode(c);
                }
            }

            protected void DetachChildren(IEnumerable children) {
                var s = SceneNode as PanelNode2D;
                foreach (Element2D c in children) {
                    if (c.Parent == this) RemoveLogicalChild(c);
                    s.RemoveChildNode(c);
                }
            }

            protected override void OnAttached() {
                base.OnAttached();
                AttachChildren(Children);
            }

            protected override void OnDetached() {
                DetachChildren(Children);
                base.OnDetached();
            }

            protected override SceneNode2D OnCreateSceneNode() {
                return new PanelNode2D();
            }
        }
    }
}

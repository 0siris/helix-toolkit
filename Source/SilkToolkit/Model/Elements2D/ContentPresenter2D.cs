using HelixToolkit.Wpf.SharpDX.Core2D;
using System.ComponentModel;
using System.Windows;
using System.Windows.Markup;
using HelixToolkit.SharpDX.Core.Model.Scene2D;

namespace HelixToolkit.Wpf.SharpDX.Elements2D;
[ContentProperty("Content")]
public class ContentPresenter2D : global::HelixToolkit.Wpf.SharpDX.Elements2D.Element2D {
    public static readonly DependencyProperty Content2DProperty = DependencyProperty.Register("Content",
        typeof(Element2DCore),
        typeof(ContentPresenter2D),
        new PropertyMetadata(null,
                             (d, e) => {
                                  var model = (ContentPresenter2D)d;
                                  var node = (PresenterNode2D)model.SceneNode;
                                 if (e.OldValue is global::HelixToolkit.Wpf.SharpDX.Elements2D.Element2D old) {
                                     model.RemoveLogicalChild(old);
                                     node.Content = null;
                                 }

                                 if (e.NewValue is global::HelixToolkit.Wpf.SharpDX.Elements2D.Element2D newElement) {
                                     model.AddLogicalChild(newElement);
                                     node.Content = newElement;
                                 }
                             }));

    [Bindable(true)]
    public global::HelixToolkit.Wpf.SharpDX.Elements2D.Element2D? Content2D {
        get => (global::HelixToolkit.Wpf.SharpDX.Elements2D.Element2D?)GetValue(Content2DProperty);
        set => SetValue(Content2DProperty, value);
    }

    protected override SceneNode2D OnCreateSceneNode() => new PresenterNode2D();
}

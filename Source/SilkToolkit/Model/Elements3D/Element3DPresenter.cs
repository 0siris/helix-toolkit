using System.Windows;
using System.Windows.Markup;
using HelixToolkit.SharpDX.Core.Model.Scene;

namespace HelixToolkit.Wpf.SharpDX;

[ContentProperty("Content")]
public class Element3DPresenter : Element3D {
    /// <summary>
    ///     The content property
    /// </summary>
    public static readonly DependencyProperty ContentProperty =
        DependencyProperty.Register("Content",
                                    typeof(Element3D),
                                    typeof(Element3DPresenter),
                                    new PropertyMetadata(null,
                                                         (d, e) => {
                                                              var model = (Element3DPresenter)d;
                                                             if (e.OldValue != null) {
                                                                 model.RemoveLogicalChild(e.OldValue);
                                                                 if (e.OldValue is Element3D ele)
                                                                      ((GroupNode)model.SceneNode).RemoveChildNode(
                                                                         ele.SceneNode);
                                                             }

                                                             if (e.NewValue != null) {
                                                                 model.AddLogicalChild(e.NewValue);
                                                                 if (e.NewValue is Element3D ele)
                                                                      ((GroupNode)model.SceneNode).AddChildNode(
                                                                         ele.SceneNode);
                                                             }
                                                         }));

    public Element3DPresenter() {
        Loaded += Element3DPresenter_Loaded;
    }

    /// <summary>
    ///     Gets or sets the content.
    /// </summary>
    /// <value>
    ///     The content.
    /// </value>
    public Element3D? Content {
        get => (Element3D?)GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    protected override SceneNode OnCreateSceneNode() => new GroupNode();

    private void Element3DPresenter_Loaded(object? sender, RoutedEventArgs e) {
        if (Content != null) {
            RemoveLogicalChild(Content);
            AddLogicalChild(Content);
        }
    }
}

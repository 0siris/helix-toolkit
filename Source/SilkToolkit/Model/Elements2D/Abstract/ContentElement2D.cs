using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model.Scene2D;
using HelixToolkit.Wpf.SharpDX.Core2D;
using HelixToolkit.Wpf.SharpDX.Extensions;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using VerticalAlignment = System.Windows.VerticalAlignment;
using WpfBrush = System.Windows.Media.Brush;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace HelixToolkit.Wpf.SharpDX.Elements2D;
[ContentProperty("Content2D")]
public abstract class ContentElement2D : Element2D {
    public static readonly DependencyProperty Content2DProperty = DependencyProperty.Register("Content2D",
        typeof(object),
        typeof(ContentElement2D),
        new PropertyMetadata(null,
                             (d, e) => {
                                 if (!(d is ContentElement2D model))
                                     return;
                                 if (!(model.SceneNode is ContentNode2D node))
                                     return;
                                 if (e.OldValue is Element2D old) {
                                     model.RemoveLogicalChild(old);
                                     node.Content = null;
                                 }

                                 if (e.NewValue is Element2D newElement) {
                                     model.AddLogicalChild(newElement);
                                     node.Content = newElement;
                                     model.SetupBindings(newElement);
                                 }

                                 model.InvalidateMeasure();
                             },
                             (_, e) => e is Element2D ? e : new TextModel2D { Text = e?.ToString() ?? string.Empty }));

    public static readonly DependencyProperty BackgroundProperty
        = DependencyProperty.Register("Background",
                                      typeof(WpfBrush),
                                      typeof(ContentElement2D),
                                      new PropertyMetadata(new WpfSolidColorBrush(Colors.Transparent),
                                                           (d, _) => {
                                                               if (d is ContentElement2D model) {
                                                                   model.backgroundChanged = true;
                                                                   model.InvalidateRender();
                                                               }
                                                           }));

    public static readonly DependencyProperty ForegroundProperty
        = DependencyProperty.Register("Foreground",
                                      typeof(WpfBrush),
                                      typeof(ContentElement2D),
                                      new PropertyMetadata(new WpfSolidColorBrush(Colors.Black)));

    public static readonly DependencyProperty HorizontalContentAlignmentProperty =
        DependencyProperty.Register("HorizontalContentAlignment",
                                    typeof(HorizontalAlignment),
                                    typeof(ContentElement2D),
                                    new PropertyMetadata(HorizontalAlignment.Center,
                                                         (d, e) => {
                                                             if (d is Element2DCore { SceneNode: ContentNode2D node })
                                                                 node.HorizontalContentAlignment =
                                                                     ((HorizontalAlignment)e.NewValue)
                                                                     .ToD2DHorizontalAlignment();
                                                         }));

    public static readonly DependencyProperty VerticalContentAlignmentProperty =
        DependencyProperty.Register("VerticalContentAlignment",
                                    typeof(VerticalAlignment),
                                    typeof(ContentElement2D),
                                    new PropertyMetadata(VerticalAlignment.Center,
                                                         (d, e) => {
                                                             if (d is Element2DCore { SceneNode: ContentNode2D node })
                                                                 node.VerticalContentAlignment =
                                                                     ((VerticalAlignment)e.NewValue)
                                                                     .ToD2DVerticalAlignment();
                                                         }));

    private bool backgroundChanged = true;

    [Bindable(true)]
    public object? Content2D {
        get => GetValue(Content2DProperty);
        set => SetValue(Content2DProperty, value);
    }

    public WpfBrush Background {
        get => (WpfBrush)GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public WpfBrush Foreground {
        get => (WpfBrush)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public HorizontalAlignment HorizontalContentAlignment {
        get => (HorizontalAlignment)GetValue(HorizontalContentAlignmentProperty);
        set => SetValue(HorizontalContentAlignmentProperty, value);
    }


    public VerticalAlignment VerticalContentAlignment {
        get => (VerticalAlignment)GetValue(VerticalContentAlignmentProperty);
        set => SetValue(VerticalContentAlignmentProperty, value);
    }

    protected override void OnUpdate(RenderContext2D context) {
        base.OnUpdate(context);
        if (backgroundChanged) {
            if (SceneNode is ContentNode2D node)
                node.Background = Background.ToD2DBrush(context.DeviceContext);
            backgroundChanged = false;
        }
    }

    protected override void OnAttached() {
        backgroundChanged = true;
        base.OnAttached();
    }

    protected void SetupBindings(Element2D content) {
        if (content is TextModel2D) {
            var binding = new Binding(nameof(Foreground)) {
                Source = this,
                Mode = BindingMode.OneWay,
                Path = new PropertyPath(nameof(Foreground))
            };
            BindingOperations.SetBinding(content, TextModel2D.ForegroundProperty, binding);
        }
    }
}

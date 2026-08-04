using System.IO;
using System.Windows;
using HelixToolkit.SharpDX.Core.Model.Scene2D;
using HelixToolkit.Wpf.SharpDX.Core2D;

namespace HelixToolkit.Wpf.SharpDX.Elements2D;
/// <summary>
/// </summary>
/// <seealso cref="HelixToolkit.Wpf.SharpDX.Elements2D.Element2D" />
public class ImageModel2D : Element2D {
    /// <summary>
    ///     The image stream property
    /// </summary>
    public static readonly DependencyProperty ImageStreamProperty =
        DependencyProperty.Register("ImageStream",
                                    typeof(Stream),
                                    typeof(ImageModel2D),
                                    new PropertyMetadata(null,
                                                         (d, e) => {
                                                             ((d as Element2DCore).SceneNode as ImageNode2D)
                                                                 .ImageStream = e.NewValue as Stream;
                                                         }));

    /// <summary>
    ///     The opacity property
    /// </summary>
    public static readonly DependencyProperty OpacityProperty =
        DependencyProperty.Register("Opacity",
                                    typeof(double),
                                    typeof(ImageModel2D),
                                    new PropertyMetadata(1.0,
                                                         (d, e) => {
                                                             ((d as Element2DCore).SceneNode as ImageNode2D)
                                                                 .Opacity = (float)(double)e.NewValue;
                                                         }));

    /// <summary>
    ///     Gets or sets the image stream.
    /// </summary>
    /// <value>
    ///     The image stream.
    /// </value>
    public Stream ImageStream {
        get => (Stream)GetValue(ImageStreamProperty);
        set => SetValue(ImageStreamProperty, value);
    }


    /// <summary>
    ///     Gets or sets the opacity.
    /// </summary>
    /// <value>
    ///     The opacity.
    /// </value>
    public double Opacity {
        get => (double)GetValue(OpacityProperty);
        set => SetValue(OpacityProperty, value);
    }


    protected override SceneNode2D OnCreateSceneNode() {
        return new ImageNode2D();
    }
}

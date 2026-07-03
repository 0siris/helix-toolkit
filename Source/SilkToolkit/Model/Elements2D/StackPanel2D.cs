using System.Windows;
using HelixToolkit.SharpDX.Core.Model.Scene2D;
using HelixToolkit.Wpf.SharpDX.Extensions;

namespace HelixToolkit.Wpf.SharpDX {
    using Orientation = System.Windows.Controls.Orientation;

    namespace Elements2D {
        public class StackPanel2D : Panel2D {
            /// <summary>
            ///     The orientation property
            /// </summary>
            public static readonly DependencyProperty OrientationProperty =
                DependencyProperty.Register("Orientation",
                                            typeof(Orientation),
                                            typeof(StackPanel2D),
                                            new PropertyMetadata(Orientation.Horizontal,
                                                                 (d, e) => {
                                                                     ((d as Element2D).SceneNode as StackPanelNode2D)
                                                                         .Orientation =
                                                                         ((Orientation) e.NewValue).ToD2DOrientation();
                                                                 }));

            /// <summary>
            ///     Gets or sets the orientation.
            /// </summary>
            /// <value>
            ///     The orientation.
            /// </value>
            public Orientation Orientation {
                get => (Orientation) GetValue(OrientationProperty);
                set => SetValue(OrientationProperty, value);
            }


            protected override SceneNode2D OnCreateSceneNode() {
                return new StackPanelNode2D();
            }
        }
    }
}

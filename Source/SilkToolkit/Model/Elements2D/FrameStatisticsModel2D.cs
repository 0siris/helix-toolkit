using System.Windows;
using System.Windows.Media;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model.Scene2D;
using HelixToolkit.Wpf.SharpDX.Extensions;
using WpfBrush = System.Windows.Media.Brush;
using WpfColor = System.Windows.Media.Color;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace HelixToolkit.Wpf.SharpDX {
    namespace Elements2D {
        public class FrameStatisticsModel2D : Element2D {
            public static readonly DependencyProperty ForegroundProperty
                = DependencyProperty.Register("Foreground",
                                              typeof(WpfBrush),
                                              typeof(FrameStatisticsModel2D),
                                              new PropertyMetadata(new WpfSolidColorBrush(Colors.Black),
                                                                   (d, e) => {
                                                                       var model = d as FrameStatisticsModel2D;
                                                                       model.foregroundChanged = true;
                                                                   }));

            public static readonly DependencyProperty BackgroundProperty
                = DependencyProperty.Register("Background",
                                              typeof(WpfBrush),
                                              typeof(FrameStatisticsModel2D),
                                              new PropertyMetadata(
                                                  new WpfSolidColorBrush(WpfColor.FromArgb(64, 32, 32, 32)),
                                                  (d, e) => {
                                                      var model = d as FrameStatisticsModel2D;
                                                      model.backgroundChanged = true;
                                                  }));

            private bool backgroundChanged = true;

            private bool foregroundChanged = true;

            public WpfBrush Foreground {
                get => (WpfBrush) GetValue(ForegroundProperty);
                set => SetValue(ForegroundProperty, value);
            }

            public WpfBrush Background {
                get => (WpfBrush) GetValue(BackgroundProperty);
                set => SetValue(BackgroundProperty, value);
            }

            protected override void OnAttached() {
                base.OnAttached();
                foregroundChanged = backgroundChanged = true;
            }

            protected override SceneNode2D OnCreateSceneNode() {
                return new FrameStatisticsNode2D();
            }

            protected override void OnUpdate(RenderContext2D context) {
                base.OnUpdate(context);
                if (foregroundChanged) {
                    (SceneNode as FrameStatisticsNode2D).Foreground =
                        Foreground != null ? Foreground.ToD2DBrush(context.DeviceContext) : null;
                    foregroundChanged = false;
                }

                if (backgroundChanged) {
                    (SceneNode as FrameStatisticsNode2D).Background =
                        Background != null ? Background.ToD2DBrush(context.DeviceContext) : null;
                    backgroundChanged = false;
                }
            }
        }
    }
}

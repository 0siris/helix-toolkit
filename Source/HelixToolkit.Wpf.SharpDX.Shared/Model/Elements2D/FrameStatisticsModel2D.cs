using System.Windows;
using System.Windows.Media;
using WpfBrush = System.Windows.Media.Brush;
using WpfColor = System.Windows.Media.Color;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;

#if COREWPF
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model.Scene2D;
#endif
namespace HelixToolkit.Wpf.SharpDX
{
    using Extensions;
#if !COREWPF
    using Model.Scene2D;
#endif
    namespace Elements2D
    {
        public class FrameStatisticsModel2D : Element2D
        {
            public static readonly DependencyProperty ForegroundProperty
                = DependencyProperty.Register("Foreground", typeof(WpfBrush), typeof(FrameStatisticsModel2D),
            new PropertyMetadata(new WpfSolidColorBrush(Colors.Black), (d, e) =>
            {
                var model = (d as FrameStatisticsModel2D);
                model.foregroundChanged = true;
            }));

            public WpfBrush Foreground
            {
                set
                {
                    SetValue(ForegroundProperty, value);
                }
                get
                {
                    return (WpfBrush)GetValue(ForegroundProperty);
                }
            }

            public static readonly DependencyProperty BackgroundProperty
                = DependencyProperty.Register("Background", typeof(WpfBrush), typeof(FrameStatisticsModel2D),
                    new PropertyMetadata(new WpfSolidColorBrush(WpfColor.FromArgb(64, 32, 32, 32)), (d, e) =>
                    {
                        var model = (d as FrameStatisticsModel2D);
                        model.backgroundChanged = true;
                    }));

            public WpfBrush Background
            {
                set
                {
                    SetValue(BackgroundProperty, value);
                }
                get
                {
                    return (WpfBrush)GetValue(BackgroundProperty);
                }
            }

            private bool foregroundChanged = true;
            private bool backgroundChanged = true;

            protected override void OnAttached()
            {
                base.OnAttached();
                foregroundChanged = backgroundChanged = true;
            }

            protected override SceneNode2D OnCreateSceneNode()
            {
                return new FrameStatisticsNode2D();
            }

            protected override void OnUpdate(RenderContext2D context)
            {
                base.OnUpdate(context);
                if (foregroundChanged)
                {
                    (SceneNode as FrameStatisticsNode2D).Foreground = Foreground != null ? Foreground.ToD2DBrush(context.DeviceContext) : null;
                    foregroundChanged = false;
                }
                if (backgroundChanged)
                {
                    (SceneNode as FrameStatisticsNode2D).Background = Background != null ? Background.ToD2DBrush(context.DeviceContext) : null;
                    backgroundChanged = false;
                }
            }
        }
    }
}

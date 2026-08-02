using System.Windows;
using System.Windows.Media;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model.Scene2D;
using HelixToolkit.Wpf.SharpDX.Core2D;
using HelixToolkit.Wpf.SharpDX.Extensions;
using WpfBrush = System.Windows.Media.Brush;
using WpfDashStyle = System.Windows.Media.DashStyle;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace HelixToolkit.Wpf.SharpDX {
    using Thickness = System.Windows.Thickness;

    namespace Elements2D {
        public class Border2D : ContentElement2D {
            public static readonly DependencyProperty CornerRadiusProperty =
                DependencyProperty.Register("CornerRadius",
                                            typeof(double),
                                            typeof(Border2D),
                                            new PropertyMetadata(0.0,
                                                                 (d, e) => {
                                                                     ((d as Element2DCore).SceneNode as BorderNode2D)
                                                                         .CornerRadius = (float)(double)e.NewValue;
                                                                 }));

            public static readonly DependencyProperty PaddingProperty =
                DependencyProperty.Register("Padding",
                                            typeof(Thickness),
                                            typeof(Border2D),
                                            new PropertyMetadata(new Thickness(0, 0, 0, 0),
                                                                 (d, e) => {
                                                                     ((d as Element2DCore).SceneNode as BorderNode2D)
                                                                         .Padding =
                                                                         ((Thickness)e.NewValue).ToD2DThickness();
                                                                 }));

            private bool strokeChanged = true;

            public double CornerRadius {
                get => (double)GetValue(CornerRadiusProperty);
                set => SetValue(CornerRadiusProperty, value);
            }

            public Thickness Padding {
                get => (Thickness)GetValue(PaddingProperty);
                set => SetValue(PaddingProperty, value);
            }

            protected override SceneNode2D OnCreateSceneNode() {
                return new BorderNode2D();
            }

            protected override void OnAttached() {
                strokeChanged = true;
                base.OnAttached();
            }

            protected override void OnUpdate(RenderContext2D context) {
                base.OnUpdate(context);
                if (strokeChanged) {
                    (SceneNode as BorderNode2D).BorderBrush = BorderBrush.ToD2DBrush(context.DeviceContext);
                    strokeChanged = false;
                }
            }

            protected override void AssignDefaultValuesToSceneNode(SceneNode2D node) {
                base.AssignDefaultValuesToSceneNode(node);
                var c = node as BorderNode2D;
                c.CornerRadius = (float)CornerRadius;
                c.Padding = Padding.ToD2DThickness();
                c.StrokeDashCap = StrokeDashCap.ToD2DCapStyle();
                c.StrokeDashOffset = (float)StrokeDashOffset;
                c.StrokeDashStyle = StrokeDashStyle.ToD2DDashStyle();
                c.StrokeEndLineCap = StrokeEndLineCap.ToD2DCapStyle();
                c.StrokeLineJoin = StrokeLineJoin.ToD2DLineJoin();
                c.StrokeMiterLimit = (float)StrokeMiterLimit;
                c.StrokeStartLineCap = StrokeStartLineCap.ToD2DCapStyle();
                c.BorderThickness = BorderThickness.ToD2DThickness();
            }

            #region Stroke properties

            public static DependencyProperty BorderBrushProperty
                = DependencyProperty.Register("BorderBrush",
                                              typeof(WpfBrush),
                                              typeof(Border2D),
                                              new PropertyMetadata(new WpfSolidColorBrush(Colors.Black),
                                                                   (d, e) => {
                                                                       (d as Border2D).strokeChanged = true;
                                                                   }));

            public WpfBrush BorderBrush {
                get => (WpfBrush)GetValue(BorderBrushProperty);
                set => SetValue(BorderBrushProperty, value);
            }

            public static DependencyProperty StrokeDashCapProperty
                = DependencyProperty.Register("StrokeDashCap",
                                              typeof(PenLineCap),
                                              typeof(Border2D),
                                              new PropertyMetadata(PenLineCap.Flat,
                                                                   (d, e) => {
                                                                       ((d as Element2DCore).SceneNode as BorderNode2D)
                                                                           .StrokeDashCap =
                                                                           ((PenLineCap)e.NewValue).ToD2DCapStyle();
                                                                   }));

            public PenLineCap StrokeDashCap {
                get => (PenLineCap)GetValue(StrokeDashCapProperty);
                set => SetValue(StrokeDashCapProperty, value);
            }

            public static DependencyProperty StrokeStartLineCapProperty
                = DependencyProperty.Register("StrokeStartLineCap",
                                              typeof(PenLineCap),
                                              typeof(Border2D),
                                              new PropertyMetadata(PenLineCap.Flat,
                                                                   (d, e) => {
                                                                       ((d as Element2DCore).SceneNode as BorderNode2D)
                                                                           .StrokeStartLineCap =
                                                                           ((PenLineCap)e.NewValue).ToD2DCapStyle();
                                                                   }));

            public PenLineCap StrokeStartLineCap {
                get => (PenLineCap)GetValue(StrokeStartLineCapProperty);
                set => SetValue(StrokeStartLineCapProperty, value);
            }

            public static DependencyProperty StrokeEndLineCapProperty
                = DependencyProperty.Register("StrokeEndLineCap",
                                              typeof(PenLineCap),
                                              typeof(Border2D),
                                              new PropertyMetadata(PenLineCap.Flat,
                                                                   (d, e) => {
                                                                       ((d as Element2DCore).SceneNode as BorderNode2D)
                                                                           .StrokeEndLineCap =
                                                                           ((PenLineCap)e.NewValue).ToD2DCapStyle();
                                                                   }));

            public PenLineCap StrokeEndLineCap {
                get => (PenLineCap)GetValue(StrokeEndLineCapProperty);
                set => SetValue(StrokeEndLineCapProperty, value);
            }

            public static DependencyProperty StrokeDashStyleProperty
                = DependencyProperty.Register("StrokeDashStyle",
                                              typeof(WpfDashStyle),
                                              typeof(Border2D),
                                              new PropertyMetadata(DashStyles.Solid,
                                                                   (d, e) => {
                                                                       ((d as Element2DCore).SceneNode as BorderNode2D)
                                                                           .StrokeDashStyle =
                                                                           ((WpfDashStyle)e.NewValue).ToD2DDashStyle();
                                                                   }));

            public WpfDashStyle StrokeDashStyle {
                get => (WpfDashStyle)GetValue(StrokeDashStyleProperty);
                set => SetValue(StrokeDashStyleProperty, value);
            }

            public static DependencyProperty StrokeDashOffsetProperty
                = DependencyProperty.Register("StrokeDashOffset",
                                              typeof(double),
                                              typeof(Border2D),
                                              new PropertyMetadata(0.0,
                                                                   (d, e) => {
                                                                       ((d as Element2DCore).SceneNode as BorderNode2D)
                                                                           .StrokeDashOffset =
                                                                           (float)(double)e.NewValue;
                                                                   }));

            public double StrokeDashOffset {
                get => (double)GetValue(StrokeDashOffsetProperty);
                set => SetValue(StrokeDashOffsetProperty, value);
            }

            public static DependencyProperty StrokeLineJoinProperty
                = DependencyProperty.Register("StrokeLineJoin",
                                              typeof(PenLineJoin),
                                              typeof(Border2D),
                                              new PropertyMetadata(PenLineJoin.Miter,
                                                                   (d, e) => {
                                                                       ((d as Element2DCore).SceneNode as BorderNode2D)
                                                                           .StrokeLineJoin =
                                                                           ((PenLineJoin)e.NewValue).ToD2DLineJoin();
                                                                   }));


            public PenLineJoin StrokeLineJoin {
                get => (PenLineJoin)GetValue(StrokeLineJoinProperty);
                set => SetValue(StrokeLineJoinProperty, value);
            }

            public static DependencyProperty StrokeMiterLimitProperty
                = DependencyProperty.Register("StrokeMiterLimit",
                                              typeof(double),
                                              typeof(Border2D),
                                              new PropertyMetadata(1.0,
                                                                   (d, e) => {
                                                                       ((d as Element2DCore).SceneNode as BorderNode2D)
                                                                           .StrokeMiterLimit =
                                                                           (float)(double)e.NewValue;
                                                                   }));

            public double StrokeMiterLimit {
                get => (double)GetValue(StrokeMiterLimitProperty);
                set => SetValue(StrokeMiterLimitProperty, value);
            }

            public static DependencyProperty BorderThicknessProperty
                = DependencyProperty.Register("BorderThickness",
                                              typeof(Thickness),
                                              typeof(Border2D),
                                              new PropertyMetadata(new Thickness(0, 0, 0, 0),
                                                                   (d, e) => {
                                                                       ((d as Element2DCore).SceneNode as BorderNode2D)
                                                                           .BorderThickness =
                                                                           ((Thickness)e.NewValue).ToD2DThickness();
                                                                   }));

            public Thickness BorderThickness {
                get => (Thickness)GetValue(BorderThicknessProperty);
                set => SetValue(BorderThicknessProperty, value);
            }

            #endregion
        }
    }
}

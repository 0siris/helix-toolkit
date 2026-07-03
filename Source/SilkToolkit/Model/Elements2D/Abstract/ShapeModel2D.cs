using System.Linq;
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
    namespace Elements2D {
        public abstract class ShapeModel2D : Element2D {
            public static DependencyProperty FillProperty
                = DependencyProperty.Register("Fill",
                                              typeof(WpfBrush),
                                              typeof(ShapeModel2D),
                                              new PropertyMetadata(new WpfSolidColorBrush(Colors.Black),
                                                                   (d, e) => {
                                                                       (d as ShapeModel2D).fillChanged = true;
                                                                   }));

            private bool fillChanged = true;
            private bool strokeChanged = true;

            public WpfBrush Fill {
                get => (WpfBrush) GetValue(FillProperty);
                set => SetValue(FillProperty, value);
            }

            protected override void OnAttached() {
                fillChanged = true;
                strokeChanged = true;
            }

            protected override void OnUpdate(RenderContext2D context) {
                base.OnUpdate(context);
                if (fillChanged) {
                    (SceneNode as ShapeNode2D).Fill = Fill.ToD2DBrush(context.DeviceContext);
                    fillChanged = false;
                }

                if (strokeChanged) {
                    (SceneNode as ShapeNode2D).Stroke = Stroke.ToD2DBrush(context.DeviceContext);
                    strokeChanged = false;
                }
            }

            protected override void AssignDefaultValuesToSceneNode(SceneNode2D node) {
                base.AssignDefaultValuesToSceneNode(node);
                var c = node as ShapeNode2D;
                c.StrokeDashArray = StrokeDashArray == null
                                        ? new float[0]
                                        : StrokeDashArray.Select(x => (float) x).ToArray();
                c.StrokeDashCap = StrokeDashCap.ToD2DCapStyle();
                c.StrokeDashOffset = (float) StrokeDashOffset;
                c.StrokeEndLineCap = StrokeEndLineCap.ToD2DCapStyle();
                c.StrokeLineJoin = StrokeLineJoin.ToD2DLineJoin();
                c.StrokeMiterLimit = (float) StrokeMiterLimit;
                c.StrokeStartLineCap = StrokeStartLineCap.ToD2DCapStyle();
                c.StrokeThickness = (float) StrokeThickness;
                c.StrokeDashStyle = WpfDashStyle.ToD2DDashStyle();
            }

        #region Stroke properties

            public static DependencyProperty StrokeProperty
                = DependencyProperty.Register("Stroke",
                                              typeof(WpfBrush),
                                              typeof(ShapeModel2D),
                                              new PropertyMetadata(new WpfSolidColorBrush(Colors.Black),
                                                                   (d, e) => {
                                                                       (d as ShapeModel2D).strokeChanged = true;
                                                                   }));

            public WpfBrush Stroke {
                get => (WpfBrush) GetValue(StrokeProperty);
                set => SetValue(StrokeProperty, value);
            }

            public static DependencyProperty StrokeDashCapProperty
                = DependencyProperty.Register("StrokeDashCap",
                                              typeof(PenLineCap),
                                              typeof(ShapeModel2D),
                                              new PropertyMetadata(PenLineCap.Flat,
                                                                   (d, e) => {
                                                                       ((d as Element2DCore).SceneNode as ShapeNode2D)
                                                                           .StrokeDashCap =
                                                                           ((PenLineCap) e.NewValue).ToD2DCapStyle();
                                                                   }));

            public PenLineCap StrokeDashCap {
                get => (PenLineCap) GetValue(StrokeDashCapProperty);
                set => SetValue(StrokeDashCapProperty, value);
            }

            public static DependencyProperty StrokeStartLineCapProperty
                = DependencyProperty.Register("StrokeStartLineCap",
                                              typeof(PenLineCap),
                                              typeof(ShapeModel2D),
                                              new PropertyMetadata(PenLineCap.Flat,
                                                                   (d, e) => {
                                                                       ((d as Element2DCore).SceneNode as ShapeNode2D)
                                                                           .StrokeStartLineCap =
                                                                           ((PenLineCap) e.NewValue).ToD2DCapStyle();
                                                                   }));

            public PenLineCap StrokeStartLineCap {
                get => (PenLineCap) GetValue(StrokeStartLineCapProperty);
                set => SetValue(StrokeStartLineCapProperty, value);
            }

            public static DependencyProperty StrokeEndLineCapProperty
                = DependencyProperty.Register("StrokeEndLineCap",
                                              typeof(PenLineCap),
                                              typeof(ShapeModel2D),
                                              new PropertyMetadata(PenLineCap.Flat,
                                                                   (d, e) => {
                                                                       ((d as Element2DCore).SceneNode as ShapeNode2D)
                                                                           .StrokeEndLineCap =
                                                                           ((PenLineCap) e.NewValue).ToD2DCapStyle();
                                                                   }));

            public PenLineCap StrokeEndLineCap {
                get => (PenLineCap) GetValue(StrokeEndLineCapProperty);
                set => SetValue(StrokeEndLineCapProperty, value);
            }

            public static DependencyProperty StrokeDashArrayProperty
                = DependencyProperty.Register("StrokeDashArray",
                                              typeof(DoubleCollection),
                                              typeof(ShapeModel2D),
                                              new PropertyMetadata(null,
                                                                   (d, e) => {
                                                                       ((d as Element2DCore).SceneNode as ShapeNode2D)
                                                                           .StrokeDashArray = e.NewValue == null
                                                                               ? new float[0]
                                                                               : (e.NewValue as DoubleCollection)
                                                                               .Select(x => (float) x).ToArray();
                                                                   }));

            public DoubleCollection StrokeDashArray {
                get => (DoubleCollection) GetValue(StrokeDashArrayProperty);
                set => SetValue(StrokeDashArrayProperty, value);
            }

            public static DependencyProperty StrokeDashOffsetProperty
                = DependencyProperty.Register("StrokeDashOffset",
                                              typeof(double),
                                              typeof(ShapeModel2D),
                                              new PropertyMetadata(0.0,
                                                                   (d, e) => {
                                                                       ((d as Element2DCore).SceneNode as ShapeNode2D)
                                                                           .StrokeDashOffset =
                                                                           (float) (double) e.NewValue;
                                                                   }));

            public double StrokeDashOffset {
                get => (double) GetValue(StrokeDashOffsetProperty);
                set => SetValue(StrokeDashOffsetProperty, value);
            }

            public static DependencyProperty StrokeLineJoinProperty
                = DependencyProperty.Register("StrokeLineJoin",
                                              typeof(PenLineJoin),
                                              typeof(ShapeModel2D),
                                              new PropertyMetadata(PenLineJoin.Bevel,
                                                                   (d, e) => {
                                                                       ((d as Element2DCore).SceneNode as ShapeNode2D)
                                                                           .StrokeLineJoin =
                                                                           ((PenLineJoin) e.NewValue).ToD2DLineJoin();
                                                                   }));


            public PenLineJoin StrokeLineJoin {
                get => (PenLineJoin) GetValue(StrokeLineJoinProperty);
                set => SetValue(StrokeLineJoinProperty, value);
            }

            public static DependencyProperty StrokeMiterLimitProperty
                = DependencyProperty.Register("StrokeMiterLimit",
                                              typeof(double),
                                              typeof(ShapeModel2D),
                                              new PropertyMetadata(1.0,
                                                                   (d, e) => {
                                                                       ((d as Element2DCore).SceneNode as ShapeNode2D)
                                                                           .StrokeMiterLimit =
                                                                           (float) (double) e.NewValue;
                                                                   }));

            public double StrokeMiterLimit {
                get => (double) GetValue(StrokeMiterLimitProperty);
                set => SetValue(StrokeMiterLimitProperty, value);
            }

            public static DependencyProperty StrokeThicknessProperty
                = DependencyProperty.Register("StrokeThickness",
                                              typeof(double),
                                              typeof(ShapeModel2D),
                                              new PropertyMetadata(1.0,
                                                                   (d, e) => {
                                                                       ((d as Element2DCore).SceneNode as ShapeNode2D)
                                                                           .StrokeThickness =
                                                                           (float) (double) e.NewValue;
                                                                   }));

            public double StrokeThickness {
                get => (double) GetValue(StrokeThicknessProperty);
                set => SetValue(StrokeThicknessProperty, value);
            }


            public WpfDashStyle WpfDashStyle {
                get => (WpfDashStyle) GetValue(DashStyleProperty);
                set => SetValue(DashStyleProperty, value);
            }

            public static readonly DependencyProperty DashStyleProperty =
                DependencyProperty.Register("WpfDashStyle",
                                            typeof(WpfDashStyle),
                                            typeof(ShapeModel2D),
                                            new PropertyMetadata(DashStyles.Solid,
                                                                 (d, e) => {
                                                                     ((d as Element2DCore).SceneNode as ShapeNode2D)
                                                                         .StrokeDashStyle =
                                                                         (e.NewValue as WpfDashStyle).ToD2DDashStyle();
                                                                 }));

        #endregion
        }
    }
}

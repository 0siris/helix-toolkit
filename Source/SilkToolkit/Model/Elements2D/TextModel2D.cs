using System;
using System.Windows;
using System.Windows.Markup;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model.Scene2D;
using HelixToolkit.Wpf.SharpDX.Core2D;
using HelixToolkit.Wpf.SharpDX.Extensions;
using Media = System.Windows.Media;
using WpfFlowDirection = System.Windows.FlowDirection;
using WpfFontStyle = System.Windows.FontStyle;
using WpfFontWeight = System.Windows.FontWeight;
using WpfTextAlignment = System.Windows.TextAlignment;

#pragma warning disable CS8601, CS8602, CS8604 // WPF dependency-property callbacks provide the owning element and scene node.

namespace HelixToolkit.Wpf.SharpDX {
    namespace Elements2D {
        [ContentProperty("Text")]
        public class TextModel2D : Element2D, ITextBlock {
            public static readonly string DefaultFont = "Arial";

            public static readonly DependencyProperty TextProperty
                = DependencyProperty.Register("Text",
                                              typeof(string),
                                              typeof(TextModel2D),
                                              new PropertyMetadata("Text",
                                                                   (d, e) => {
                                                                       ((d as Element2DCore).SceneNode as TextNode2D)
                                                                           .Text =
                                                                           e.NewValue == null
                                                                               ? string.Empty
                                                                               : (string)e.NewValue;
                                                                   }));


            public static readonly DependencyProperty ForegroundProperty
                = DependencyProperty.Register("Foreground",
                                              typeof(Media.Brush),
                                              typeof(TextModel2D),
                                              new PropertyMetadata(new Media.SolidColorBrush(Media.Colors.Black),
                                                                   (d, e) => {
                                                                       var model = d as TextModel2D;
                                                                       model.foregroundChanged = true;
                                                                   }));

            public static readonly DependencyProperty BackgroundProperty
                = DependencyProperty.Register("Background",
                                              typeof(Media.Brush),
                                              typeof(TextModel2D),
                                              new PropertyMetadata(null,
                                                                   (d, e) => {
                                                                       var model = d as TextModel2D;
                                                                       model.backgroundChanged = true;
                                                                   }));

            public static readonly DependencyProperty FontSizeProperty
                = DependencyProperty.Register("FontSize",
                                              typeof(int),
                                              typeof(TextModel2D),
                                              new PropertyMetadata(12,
                                                                   (d, e) => {
                                                                       ((d as Element2DCore).SceneNode as TextNode2D)
                                                                           .FontSize = Math.Max(1, (int)e.NewValue);
                                                                   }));

            public static readonly DependencyProperty FontWeightProperty
                = DependencyProperty.Register("FontWeight",
                                              typeof(WpfFontWeight),
                                              typeof(TextModel2D),
                                              new PropertyMetadata(FontWeights.Normal,
                                                                   (d, e) => {
                                                                       ((d as Element2DCore).SceneNode as TextNode2D)
                                                                           .FontWeight =
                                                                           ((WpfFontWeight)e.NewValue)
                                                                           .ToDXFontWeight();
                                                                   }));

            public static readonly DependencyProperty FontStyleProperty
                = DependencyProperty.Register("FontStyle",
                                              typeof(WpfFontStyle),
                                              typeof(TextModel2D),
                                              new PropertyMetadata(FontStyles.Normal,
                                                                   (d, e) => {
                                                                       ((d as Element2DCore).SceneNode as TextNode2D)
                                                                           .FontStyle =
                                                                           ((WpfFontStyle)e.NewValue).ToDXFontStyle();
                                                                   }));

            /// <summary>
            ///     The text alignment property
            /// </summary>
            public static readonly DependencyProperty TextAlignmentProperty =
                DependencyProperty.Register("TextAlignment",
                                            typeof(WpfTextAlignment),
                                            typeof(TextModel2D),
                                            new PropertyMetadata(WpfTextAlignment.Left,
                                                                 (d, e) => {
                                                                     ((d as Element2DCore).SceneNode as TextNode2D)
                                                                         .TextAlignment =
                                                                         ((WpfTextAlignment)e.NewValue)
                                                                         .ToD2DTextAlignment();
                                                                 }));

            /// <summary>
            ///     The text alignment property
            /// </summary>
            public static readonly DependencyProperty FlowDirectionProperty =
                DependencyProperty.Register("FlowDirection",
                                            typeof(WpfFlowDirection),
                                            typeof(TextModel2D),
                                            new PropertyMetadata(WpfFlowDirection.LeftToRight,
                                                                 (d, e) => {
                                                                     ((d as Element2DCore).SceneNode as TextNode2D)
                                                                         .FlowDirection =
                                                                         ((WpfFlowDirection)e.NewValue).ToD2DFlowDir();
                                                                 }));

            /// <summary>
            ///     The font family property
            /// </summary>
            public static readonly DependencyProperty FontFamilyProperty =
                DependencyProperty.Register("FontFamily",
                                            typeof(string),
                                            typeof(TextModel2D),
                                            new PropertyMetadata(DefaultFont,
                                                                 (d, e) => {
                                                                     ((d as Element2DCore).SceneNode as TextNode2D)
                                                                         .FontFamily =
                                                                         e.NewValue == null
                                                                             ? "Arial"
                                                                             : (string)e.NewValue;
                                                                 }));

            private bool backgroundChanged = true;

            private bool foregroundChanged = true;

            public string Text {
                get => (string)GetValue(TextProperty);
                set => SetValue(TextProperty, value);
            }

            public int FontSize {
                get => (int)GetValue(FontSizeProperty);
                set => SetValue(FontSizeProperty, value);
            }

            public WpfFontWeight FontWeight {
                get => (WpfFontWeight)GetValue(FontWeightProperty);
                set => SetValue(FontWeightProperty, value);
            }

            public WpfFontStyle FontStyle {
                get => (WpfFontStyle)GetValue(FontStyleProperty);
                set => SetValue(FontStyleProperty, value);
            }


            /// <summary>
            ///     Gets or sets the text alignment.
            /// </summary>
            /// <value>
            ///     The text alignment.
            /// </value>
            public WpfTextAlignment TextAlignment {
                get => (WpfTextAlignment)GetValue(TextAlignmentProperty);
                set => SetValue(TextAlignmentProperty, value);
            }

            /// <summary>
            ///     Gets or sets the text alignment.
            /// </summary>
            /// <value>
            ///     The text alignment.
            /// </value>
            public WpfFlowDirection FlowDirection {
                get => (WpfFlowDirection)GetValue(FlowDirectionProperty);
                set => SetValue(FlowDirectionProperty, value);
            }


            /// <summary>
            ///     Gets or sets the font family.
            /// </summary>
            /// <value>
            ///     The font family.
            /// </value>
            public string FontFamily {
                get => (string)GetValue(FontFamilyProperty);
                set => SetValue(FontFamilyProperty, value);
            }

            public Media.Brush Foreground {
                get => (Media.Brush)GetValue(ForegroundProperty);
                set => SetValue(ForegroundProperty, value);
            }

            public Media.Brush Background {
                get => (Media.Brush)GetValue(BackgroundProperty);
                set => SetValue(BackgroundProperty, value);
            }

            protected override SceneNode2D OnCreateSceneNode() {
                return new TextNode2D();
            }

            protected override void OnAttached() {
                base.OnAttached();
                foregroundChanged = true;
                backgroundChanged = true;
            }

            protected override void OnUpdate(RenderContext2D context) {
                base.OnUpdate(context);
                if (foregroundChanged) {
                    (SceneNode as TextNode2D).Foreground =
                        Foreground?.ToD2DBrush(context.DeviceContext);
                    foregroundChanged = false;
                }

                if (backgroundChanged) {
                    (SceneNode as TextNode2D).Background =
                        Background?.ToD2DBrush(context.DeviceContext);
                    backgroundChanged = false;
                }
            }

            protected override void AssignDefaultValuesToSceneNode(SceneNode2D node) {
                var t = node as TextNode2D;
                t.Text = Text == null ? string.Empty : Text;
                t.FontFamily = FontFamily == null ? DefaultFont : FontFamily;
                t.FontWeight = FontWeight.ToDXFontWeight();
                t.FontStyle = FontStyle.ToDXFontStyle();
                t.FontSize = FontSize;
                t.TextAlignment = TextAlignment.ToD2DTextAlignment();
                t.FlowDirection = FlowDirection.ToD2DFlowDir();
            }
        }
    }
}

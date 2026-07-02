using System;
using System.Windows;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows.Markup;

using System.Linq;
using Media = System.Windows.Media;
using WpfFontStyle = System.Windows.FontStyle;
using WpfFontWeight = System.Windows.FontWeight;
using WpfFlowDirection = System.Windows.FlowDirection;
using WpfTextAlignment = System.Windows.TextAlignment;

using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model.Scene2D;

namespace HelixToolkit.Wpf.SharpDX
{
    using Core2D;
    using Utilities;
    using Extensions;

    namespace Elements2D
    {
        [ContentProperty("Text")]
        public class TextModel2D : Element2D, ITextBlock
        {
            public static readonly string DefaultFont = "Arial";

            public static readonly DependencyProperty TextProperty
                = DependencyProperty.Register("Text", typeof(string), typeof(TextModel2D),
                    new PropertyMetadata("Text", (d, e) =>
                    {
                        ((d as Element2DCore).SceneNode as TextNode2D).Text = e.NewValue == null ? string.Empty : (string)e.NewValue;
                    }));

            public string Text
            {
                set
                {
                    SetValue(TextProperty, value);
                }
                get
                {
                    return (string)GetValue(TextProperty);
                }
            }


            public static readonly DependencyProperty ForegroundProperty
                = DependencyProperty.Register("Foreground", typeof(Media.Brush), typeof(TextModel2D),
                    new PropertyMetadata(new Media.SolidColorBrush(Colors.Black), (d, e) =>
                    {
                        var model = (d as TextModel2D);
                        model.foregroundChanged = true;
                    }));

            public Media.Brush Foreground
            {
                set
                {
                    SetValue(ForegroundProperty, value);
                }
                get
                {
                    return (Media.Brush)GetValue(ForegroundProperty);
                }
            }

            public static readonly DependencyProperty BackgroundProperty
                = DependencyProperty.Register("Background", typeof(Media.Brush), typeof(TextModel2D),
                    new PropertyMetadata(null, (d, e) =>
                {
                    var model = (d as TextModel2D);
                    model.backgroundChanged = true;
                }));

            public Media.Brush Background
            {
                set
                {
                    SetValue(BackgroundProperty, value);
                }
                get
                {
                    return (Media.Brush)GetValue(BackgroundProperty);
                }
            }

            public static readonly DependencyProperty FontSizeProperty
                = DependencyProperty.Register("FontSize", typeof(int), typeof(TextModel2D),
                    new PropertyMetadata(12, (d, e) =>
                    {
                        ((d as Element2DCore).SceneNode as TextNode2D).FontSize = Math.Max(1, (int)e.NewValue);
                    }));

            public int FontSize
            {
                set
                {
                    SetValue(FontSizeProperty, value);
                }
                get
                {
                    return (int)GetValue(FontSizeProperty);
                }
            }

            public static readonly DependencyProperty FontWeightProperty
                = DependencyProperty.Register("FontWeight", typeof(WpfFontWeight), typeof(TextModel2D),
                    new PropertyMetadata(FontWeights.Normal, (d, e) =>
                    {
                        ((d as Element2DCore).SceneNode as TextNode2D).FontWeight = ((WpfFontWeight)e.NewValue).ToDXFontWeight();
                    }));

            public WpfFontWeight FontWeight
            {
                set
                {
                    SetValue(FontWeightProperty, value);
                }
                get
                {
                    return (WpfFontWeight)GetValue(FontWeightProperty);
                }
            }

            public static readonly DependencyProperty FontStyleProperty
                = DependencyProperty.Register("FontStyle", typeof(WpfFontStyle), typeof(TextModel2D),
                    new PropertyMetadata(FontStyles.Normal, (d, e) =>
                    {
                        ((d as Element2DCore).SceneNode as TextNode2D).FontStyle = ((WpfFontStyle)e.NewValue).ToDXFontStyle();
                    }));

            public WpfFontStyle FontStyle
            {
                set
                {
                    SetValue(FontStyleProperty, value);
                }
                get
                {
                    return (WpfFontStyle)GetValue(FontStyleProperty);
                }
            }


            /// <summary>
            /// Gets or sets the text alignment.
            /// </summary>
            /// <value>
            /// The text alignment.
            /// </value>
            public WpfTextAlignment TextAlignment
            {
                get
                {
                    return (WpfTextAlignment)GetValue(TextAlignmentProperty);
                }
                set
                {
                    SetValue(TextAlignmentProperty, value);
                }
            }

            /// <summary>
            /// The text alignment property
            /// </summary>
            public static readonly DependencyProperty TextAlignmentProperty =
                DependencyProperty.Register("TextAlignment", typeof(WpfTextAlignment), typeof(TextModel2D), new PropertyMetadata(WpfTextAlignment.Left, (d, e) =>
                {
                    ((d as Element2DCore).SceneNode as TextNode2D).TextAlignment = ((WpfTextAlignment)e.NewValue).ToD2DTextAlignment();
                }));

            /// <summary>
            /// Gets or sets the text alignment.
            /// </summary>
            /// <value>
            /// The text alignment.
            /// </value>
            public WpfFlowDirection FlowDirection
            {
                get
                {
                    return (WpfFlowDirection)GetValue(FlowDirectionProperty);
                }
                set
                {
                    SetValue(FlowDirectionProperty, value);
                }
            }

            /// <summary>
            /// The text alignment property
            /// </summary>
            public static readonly DependencyProperty FlowDirectionProperty =
                DependencyProperty.Register("FlowDirection", typeof(WpfFlowDirection), typeof(TextModel2D), new PropertyMetadata(WpfFlowDirection.LeftToRight, (d, e) =>
                {
                    ((d as Element2DCore).SceneNode as TextNode2D).FlowDirection = ((WpfFlowDirection)e.NewValue).ToD2DFlowDir();
                }));


            /// <summary>
            /// Gets or sets the font family.
            /// </summary>
            /// <value>
            /// The font family.
            /// </value>
            public string FontFamily
            {
                get
                {
                    return (string)GetValue(FontFamilyProperty);
                }
                set
                {
                    SetValue(FontFamilyProperty, value);
                }
            }
            /// <summary>
            /// The font family property
            /// </summary>
            public static readonly DependencyProperty FontFamilyProperty =
                DependencyProperty.Register("FontFamily", typeof(string), typeof(TextModel2D), new PropertyMetadata(DefaultFont, (d, e) =>
                {
                    ((d as Element2DCore).SceneNode as TextNode2D).FontFamily = e.NewValue == null ? "Arial" : (string)e.NewValue;
                }));

            private bool foregroundChanged = true;
            private bool backgroundChanged = true;

            protected override SceneNode2D OnCreateSceneNode()
            {
                return new TextNode2D();
            }

            protected override void OnAttached()
            {
                base.OnAttached();
                foregroundChanged = true;
                backgroundChanged = true;
            }

            protected override void OnUpdate(RenderContext2D context)
            {
                base.OnUpdate(context);
                if (foregroundChanged)
                {
                    (SceneNode as TextNode2D).Foreground = Foreground != null ? Foreground.ToD2DBrush(context.DeviceContext) : null;
                    foregroundChanged = false;
                }
                if (backgroundChanged)
                {
                    (SceneNode as TextNode2D).Background = Background != null ? Background.ToD2DBrush(context.DeviceContext) : null;
                    backgroundChanged = false;
                }
            }

            protected override void AssignDefaultValuesToSceneNode(SceneNode2D node)
            {
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

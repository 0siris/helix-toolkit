/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Native;

namespace HelixToolkit.SharpDX.Core {
    namespace Core2D {
        public class TextRenderCore2D : RenderCore2DBase {
            private Brush background;

            private FlowDirection flowDirection = FlowDirection.LeftToRight;

            private string fontFamily = "Arial";

            private int fontSize = 12;

            private FontStyle fontStyle = FontStyle.Normal;

            private FontWeight fontWeight = FontWeight.Normal;

            private Brush foreground;

            private float maxHeight;

            private float maxWidth;
            private string text = string.Empty;

            private TextAlignment textAlignment = TextAlignment.Leading;

            private DirectWriteFactory textFactory;
            private TextFormat textFormat;

            private TextLayout textLayout;

            protected bool textLayoutDirty = true;

            public string Text {
                get => text;
                set {
                    if (SetAffectsRender(ref text, value)) textLayoutDirty = true;
                }
            }

            public Brush Foreground {
                get => foreground;
                set {
                    var old = foreground;
                    if (SetAffectsRender(ref foreground, value)) RemoveAndDispose(ref old);
                }
            }

            public Brush Background {
                get => background;
                set {
                    var old = background;
                    if (SetAffectsRender(ref background, value)) RemoveAndDispose(ref old);
                }
            }

            public string FontFamily {
                get => fontFamily;
                set {
                    if (SetAffectsRender(ref fontFamily, value) && IsAttached) UpdateFontFormat();
                }
            }

            public int FontSize {
                get => fontSize;
                set {
                    if (SetAffectsRender(ref fontSize, value) && IsAttached) UpdateFontFormat();
                }
            }

            public FontWeight FontWeight {
                get => fontWeight;
                set {
                    if (SetAffectsRender(ref fontWeight, value) && IsAttached) UpdateFontFormat();
                }
            }

            public FontStyle FontStyle {
                get => fontStyle;
                set {
                    if (SetAffectsRender(ref fontStyle, value) && IsAttached) UpdateFontFormat();
                }
            }

            public DrawTextOptions DrawingOptions { get; set; } = DrawTextOptions.None;

            public TextAlignment TextAlignment {
                get => textAlignment;
                set => SetAffectsRender(ref textAlignment, value);
            }

            public FlowDirection FlowDirection {
                get => flowDirection;
                set => SetAffectsRender(ref flowDirection, value);
            }

            public TextMetrics Metrices {
                get {
                    UpdateTextLayout();
                    return textLayout.Metrics;
                }
            }

            public float MaxWidth {
                get => maxWidth;
                set {
                    if (Set(ref maxWidth, value)) textLayoutDirty = true;
                }
            }

            public float MaxHeight {
                get => maxHeight;
                set {
                    if (Set(ref maxHeight, value)) textLayoutDirty = true;
                }
            }

            protected override bool OnAttach(IRenderHost host) {
                if (base.OnAttach(host)) {
                    textLayoutDirty = true;
                    textFactory = new DirectWriteFactory();
                    textFormat = new TextFormat(textFactory,
                                                FontFamily,
                                                FontWeight,
                                                FontStyle,
                                                FontSize * host.DpiScale);
                    return true;
                }

                return false;
            }

            protected override void OnDetach() {
                RemoveAndDispose(ref textFormat);
                RemoveAndDispose(ref textLayout);
                RemoveAndDispose(ref foreground);
                RemoveAndDispose(ref background);
                RemoveAndDispose(ref textFactory);
                base.OnDetach();
            }

            private void UpdateFontFormat() {
                RemoveAndDispose(ref textFormat);
                textFormat = new TextFormat(textFactory,
                                            FontFamily,
                                            FontWeight,
                                            FontStyle,
                                            FontSize * RenderHost.DpiScale);
                textLayoutDirty = true;
            }

            private void UpdateTextLayout() {
                if (textLayoutDirty) {
                    RemoveAndDispose(ref textLayout);
                    textLayout = new TextLayout(textFactory, Text, textFormat, MaxWidth, MaxHeight);
                    textLayoutDirty = false;
                }

                textLayout.TextAlignment = TextAlignment;
            }

            protected override bool CanRender(RenderContext2D context) {
                return base.CanRender(context) && Foreground != null && Text != null;
            }

            protected override void OnRender(RenderContext2D context) {
                if (Background != null) context.DeviceContext.FillRectangle(LayoutBound, Background);
                UpdateTextLayout();
                context.DeviceContext.DrawTextLayout(new Vector2(LayoutBound.Left, LayoutBound.Top),
                                                     textLayout,
                                                     Foreground,
                                                     DrawingOptions);
            }
        }
    }
}

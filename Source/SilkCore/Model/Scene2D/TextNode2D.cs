/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Core2D;

namespace HelixToolkit.SharpDX.Core {
    namespace Model.Scene2D {
        public class TextNode2D : SceneNode2D {
            private string text = string.Empty;

            private TextRenderCore2D textRenderable;

            public string Text {
                get => text;
                set {
                    if (SetAffectsMeasure(ref text, value)) (RenderCore as TextRenderCore2D).Text = value;
                }
            }

            public Brush Foreground {
                get => (RenderCore as TextRenderCore2D).Foreground;
                set => (RenderCore as TextRenderCore2D).Foreground = value;
            }

            public Brush Background {
                get => (RenderCore as TextRenderCore2D).Background;
                set => (RenderCore as TextRenderCore2D).Background = value;
            }

            public int FontSize {
                get => (RenderCore as TextRenderCore2D).FontSize;
                set => (RenderCore as TextRenderCore2D).FontSize = value;
            }

            public FontWeight FontWeight {
                get => (RenderCore as TextRenderCore2D).FontWeight;
                set => (RenderCore as TextRenderCore2D).FontWeight = value;
            }

            public FontStyle FontStyle {
                get => (RenderCore as TextRenderCore2D).FontStyle;
                set => (RenderCore as TextRenderCore2D).FontStyle = value;
            }

            public TextAlignment TextAlignment {
                get => (RenderCore as TextRenderCore2D).TextAlignment;
                set => (RenderCore as TextRenderCore2D).TextAlignment = value;
            }

            public FlowDirection FlowDirection {
                get => (RenderCore as TextRenderCore2D).FlowDirection;
                set => (RenderCore as TextRenderCore2D).FlowDirection = value;
            }

            public string FontFamily {
                get => (RenderCore as TextRenderCore2D).FontFamily;
                set => (RenderCore as TextRenderCore2D).FontFamily = value;
            }

            protected override RenderCore2D CreateRenderCore() {
                textRenderable = new TextRenderCore2D();
                return textRenderable;
            }

        protected override bool OnHitTest(ref Vector2 mousePoint, out HitTest2DResult? hitResult) {
                hitResult = null;
                if (LayoutBoundWithTransform.Contains(mousePoint)) {
                    hitResult = new HitTest2DResult(WrapperSource);
                    return true;
                }

                return false;
            }

            protected override Size2F MeasureOverride(Size2F availableSize) {
                textRenderable.MaxWidth = availableSize.Width;
                textRenderable.MaxHeight = availableSize.Height;
                var metrices = textRenderable.Metrices;
                return new Size2F(metrices.WidthIncludingTrailingWhitespace, metrices.Height);
            }

            protected override RectangleF ArrangeOverride(RectangleF finalSize) {
                textRenderable.MaxWidth = finalSize.Width;
                textRenderable.MaxHeight = finalSize.Height;
                var metrices = textRenderable.Metrices;
                return finalSize;
            }
        }
    }
}

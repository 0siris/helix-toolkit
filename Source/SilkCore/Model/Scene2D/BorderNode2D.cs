/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core2D;

namespace HelixToolkit.SharpDX.Core {
    namespace Model.Scene2D {
        public class BorderNode2D : ContentNode2D {
            private Thickness borderThickness;

            private Thickness padding = new(0);

            private CapStyle strokeDashCap = CapStyle.Flat;

            private float strokeDashOffset;

            private DashStyle strokeDashStyle = DashStyle.Solid;

            private CapStyle strokeEndLineCap = CapStyle.Flat;

            private LineJoin strokeLineJoin = LineJoin.Miter;

            private float strokeMiterLimit = 1;

            private CapStyle strokeStartLineCap = CapStyle.Flat;

            private bool strokeStyleChanged = true;

            public float CornerRadius {
                get => (RenderCore as BorderRenderCore2D).CornerRadius;
                set => (RenderCore as BorderRenderCore2D).CornerRadius = value;
            }

            public Thickness Padding {
                get => padding;
                set => SetAffectsMeasure(ref padding, value);
            }

            public Brush BorderBrush {
                get => (RenderCore as BorderRenderCore2D).StrokeBrush;
                set => (RenderCore as BorderRenderCore2D).StrokeBrush = value;
            }

            public CapStyle StrokeDashCap {
                get => strokeDashCap;
                set {
                    if (SetAffectsRender(ref strokeDashCap, value)) strokeStyleChanged = true;
                }
            }

            public CapStyle StrokeStartLineCap {
                get => strokeStartLineCap;
                set {
                    if (SetAffectsRender(ref strokeStartLineCap, value)) strokeStyleChanged = true;
                }
            }

            public CapStyle StrokeEndLineCap {
                get => strokeEndLineCap;
                set {
                    if (SetAffectsRender(ref strokeEndLineCap, value)) strokeStyleChanged = true;
                }
            }

            public DashStyle StrokeDashStyle {
                get => strokeDashStyle;
                set {
                    if (SetAffectsRender(ref strokeDashStyle, value)) strokeStyleChanged = true;
                }
            }

            public float StrokeDashOffset {
                get => strokeDashOffset;
                set {
                    if (SetAffectsRender(ref strokeDashOffset, value)) strokeStyleChanged = true;
                }
            }

            public LineJoin StrokeLineJoin {
                get => strokeLineJoin;
                set {
                    if (SetAffectsRender(ref strokeLineJoin, value)) strokeStyleChanged = true;
                }
            }

            public float StrokeMiterLimit {
                get => strokeMiterLimit;
                set {
                    if (SetAffectsRender(ref strokeMiterLimit, value)) strokeStyleChanged = true;
                }
            }

            public Thickness BorderThickness {
                get => borderThickness;
                set {
                    if (SetAffectsMeasure(ref borderThickness, value))
                        (RenderCore as BorderRenderCore2D).BorderThickness = value;
                }
            }

            protected override bool OnAttach(IRenderHost host) {
                if (base.OnAttach(host)) {
                    strokeStyleChanged = true;
                    return true;
                }

                return false;
            }

            public override void Update(RenderContext2D context) {
                base.Update(context);
                if (strokeStyleChanged) {
                    (RenderCore as BorderRenderCore2D).StrokeStyle = new StrokeStyle(context.DeviceResources.Factory2D,
                        new StrokeStyleProperties {
                            DashCap = StrokeDashCap,
                            StartCap = StrokeStartLineCap,
                            EndCap = StrokeEndLineCap,
                            DashOffset = StrokeDashOffset,
                            LineJoin = StrokeLineJoin,
                            MiterLimit = Math.Max(1, StrokeMiterLimit),
                            DashStyle = StrokeDashStyle
                        });
                    strokeStyleChanged = false;
                }
            }

            protected override Size2F MeasureOverride(Size2F availableSize) {
                if (Content != null) {
                    var margin = new Size2F(
                        BorderThickness.Left / 2 + Padding.Left + BorderThickness.Right / 2 + Padding.Right,
                        BorderThickness.Top / 2 + Padding.Top + BorderThickness.Bottom / 2 + Padding.Bottom);
                    margin.Width *= DpiScale;
                    margin.Height *= DpiScale;
                    var childAvail = new Size2F(Math.Max(0, availableSize.Width - margin.Width),
                                                Math.Max(0, availableSize.Height - margin.Height));

                    var size = base.MeasureOverride(childAvail);
                    if (Width != float.PositiveInfinity && Height != float.PositiveInfinity) return availableSize;

                    if (Width != float.PositiveInfinity) size.Width = Width * DpiScale;
                    if (Height != float.PositiveInfinity) size.Height = Height * DpiScale;
                    return size;
                } else {
                    var size = new Size2F(BorderThickness.Left / 2 + Padding.Left + BorderThickness.Right / 2 +
                                          Padding.Right +
                                          MarginWidthHeight.X + Width == float.PositiveInfinity
                                              ? 0
                                              : Width,
                                          BorderThickness.Top / 2 + Padding.Top + BorderThickness.Bottom / 2 +
                                          Padding.Bottom +
                                          MarginWidthHeight.Y + Height == float.PositiveInfinity
                                              ? 0
                                              : Height);
                    size.Width *= DpiScale;
                    size.Height *= DpiScale;
                    return size;
                }
            }

            protected override RectangleF ArrangeOverride(RectangleF finalSize) {
                var contentRect = new RectangleF(finalSize.Left, finalSize.Top, finalSize.Width, finalSize.Height);
                contentRect.Left += (BorderThickness.Left / 2 + Padding.Left) * DpiScale;
                contentRect.Right -= (BorderThickness.Right / 2 + Padding.Right) * DpiScale;
                contentRect.Top += (BorderThickness.Top / 2 + Padding.Top) * DpiScale;
                contentRect.Bottom -= (BorderThickness.Bottom / 2 + Padding.Bottom) * DpiScale;
                base.ArrangeOverride(contentRect);
                return finalSize;
            }
        }
    }
}

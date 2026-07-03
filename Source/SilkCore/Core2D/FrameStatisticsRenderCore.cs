/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core {
    namespace Core2D {
        /// <summary>
        /// </summary>
        public class FrameStatisticsRenderCore : RenderCore2DBase {
            private Brush background;
            private DirectWriteFactory factory;

            private Brush foreground;
            private TextFormat format;
            private string previousStr = string.Empty;
            private RectangleF renderBound = new(0, 0, 100, 0);
            private IRenderStatistics statistics;

            private TextLayout textLayout;

            /// <summary>
            ///     Gets or sets the foreground.
            /// </summary>
            /// <value>
            ///     The foreground.
            /// </value>
            public Brush Foreground {
                get => foreground;
                set {
                    var old = foreground;
                    if (SetAffectsRender(ref foreground, value)) RemoveAndDispose(ref old);
                }
            }

            /// <summary>
            ///     Gets or sets the background.
            /// </summary>
            /// <value>
            ///     The background.
            /// </value>
            public Brush Background {
                get => background;
                set {
                    var old = background;
                    if (SetAffectsRender(ref background, value)) RemoveAndDispose(ref old);
                }
            }

            /// <summary>
            ///     Called when [attach].
            /// </summary>
            /// <param name="target">The target.</param>
            /// <returns></returns>
            protected override bool OnAttach(IRenderHost target) {
                factory = new DirectWriteFactory();
                format = new TextFormat(factory, "Arial", FontWeight.Normal, FontStyle.Normal, 12 * target.DpiScale);
                previousStr = string.Empty;
                statistics = target.RenderStatistics;
                return base.OnAttach(target);
            }

            protected override void OnDetach() {
                RemoveAndDispose(ref format);
                RemoveAndDispose(ref foreground);
                RemoveAndDispose(ref background);
                RemoveAndDispose(ref textLayout);
                RemoveAndDispose(ref factory);
                base.OnDetach();
            }

            /// <summary>
            ///     Determines whether this instance can render the specified context.
            /// </summary>
            /// <param name="context">The context.</param>
            /// <returns>
            ///     <c>true</c> if this instance can render the specified context; otherwise, <c>false</c>.
            /// </returns>
            protected override bool CanRender(RenderContext2D context) {
                return base.CanRender(context) && statistics != null && statistics.FrameDetail != RenderDetail.None;
            }

            /// <summary>
            ///     Called when [render].
            /// </summary>
            /// <param name="context">The context.</param>
            protected override void OnRender(RenderContext2D context) {
                if (background == null)
                    Background = new SolidColorBrush(context.DeviceContext, new Color4(0.8f, 0.8f, 0.8f, 0.6f));
                if (foreground == null) Foreground = new SolidColorBrush(context.DeviceContext, new Color4(0, 0, 1, 1));
                var str = statistics.GetDetailString();
                if (str != previousStr || textLayout == null) {
                    previousStr = str;
                    RemoveAndDispose(ref textLayout);
                    textLayout = new TextLayout(factory, str, format, float.MaxValue, float.MaxValue);
                }

                var metrices = textLayout.Metrics;
                renderBound.Width = Math.Max(metrices.Width, renderBound.Width);
                renderBound.Height = metrices.Height;
                context.DeviceContext.Transform =
                    Matrix3x2.Translation((float) context.ActualWidth - renderBound.Width, 0);
                context.DeviceContext.FillRectangle(renderBound, background);
                context.DeviceContext.DrawTextLayout(Vector2.Zero, textLayout, foreground);
            }
        }
    }
}

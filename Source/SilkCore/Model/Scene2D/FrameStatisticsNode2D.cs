/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Core2D;

namespace HelixToolkit.SharpDX.Core {
    namespace Model.Scene2D {
        public class FrameStatisticsNode2D : SceneNode2D {
            public FrameStatisticsNode2D() {
                HorizontalAlignment = HorizontalAlignment.Right;
                VerticalAlignment = VerticalAlignment.Top;
                EnableBitmapCache = false;
            }

            public Brush Foreground {
                get => (RenderCore as FrameStatisticsRenderCore).Foreground;
                set => (RenderCore as FrameStatisticsRenderCore).Foreground = value;
            }

            public Brush Background {
                get => (RenderCore as FrameStatisticsRenderCore).Background;
                set => (RenderCore as FrameStatisticsRenderCore).Background = value;
            }

            protected override RenderCore2D CreateRenderCore() {
                return new FrameStatisticsRenderCore();
            }

            protected override bool CanHitTest() {
                return false;
            }

            protected override bool OnHitTest(ref Vector2 mousePoint, out HitTest2DResult hitResult) {
                hitResult = null;
                return false;
            }
        }
    }
}

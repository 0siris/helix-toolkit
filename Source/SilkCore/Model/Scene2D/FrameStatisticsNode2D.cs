/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Core2D;

namespace HelixToolkit.SharpDX.Core.Model.Scene2D;
public class FrameStatisticsNode2D : SceneNode2D {
    public FrameStatisticsNode2D() {
        HorizontalAlignment = HorizontalAlignment.Right;
        VerticalAlignment = VerticalAlignment.Top;
        EnableBitmapCache = false;
    }

    public Brush? Foreground {
        get => ((FrameStatisticsRenderCore)RenderCore).Foreground;
        set => ((FrameStatisticsRenderCore)RenderCore).Foreground = value;
    }

    public Brush? Background {
        get => ((FrameStatisticsRenderCore)RenderCore).Background;
        set => ((FrameStatisticsRenderCore)RenderCore).Background = value;
    }

    protected override RenderCore2D CreateRenderCore() => new FrameStatisticsRenderCore();

    protected override bool CanHitTest() => false;

    protected override bool OnHitTest(ref Vector2 mousePoint, out HitTest2DResult? hitResult) {
        hitResult = null;
        return false;
    }
}

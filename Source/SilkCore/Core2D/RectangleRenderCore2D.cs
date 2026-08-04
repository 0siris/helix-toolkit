/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

namespace HelixToolkit.SharpDX.Core.Core2D;
public class RectangleRenderCore2D : ShapeRenderCore2DBase {
    protected override void OnRender(RenderContext2D context) {
        if (FillBrush != null) context.DeviceContext.FillRectangle(LayoutBound, FillBrush);
        if (StrokeBrush != null && StrokeStyle != null)
            context.DeviceContext.DrawRectangle(LayoutBound, StrokeBrush, StrokeWidth, StrokeStyle);
    }
}

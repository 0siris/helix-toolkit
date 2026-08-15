/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/
//#define DEBUGBOUNDS


using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render;

namespace HelixToolkit.SharpDX.Core.Core2D.Abstract;
/// <summary>
/// </summary>
public abstract class RenderCore2DBase : RenderCore2D {
#if DEBUGBOUNDS
    /// <summary>
    ///
    /// </summary>
    public bool ShowDrawingBorder { set; get; } = true;
#else
    /// <summary>
    /// </summary>
    public bool ShowDrawingBorder { get; set; } = false;
#endif
    /// <summary>
    ///     Renders the specified context.
    /// </summary>
    /// <param name="context">The context.</param>
    public override void Render(RenderContext2D context) {
        if (!CanRender(context)) 
            return;
        
        context.DeviceContext.Transform = Transform;
        if (ShowDrawingBorder) {
            using var borderBrush = new SolidColorBrush(context.DeviceContext, new Color4(0, 0, 1, 1));
            using var borderDotStyle = new StrokeStyle(context.DeviceContext.Factory,
                                                       new StrokeStyleProperties { DashStyle = DashStyle.DashDot });
            using var borderLineStyle = new StrokeStyle(context.DeviceContext.Factory,
                                                        new StrokeStyleProperties { DashStyle = DashStyle.Solid });
            context.DeviceContext.DrawRectangle(LayoutBound,
                                                borderBrush,
                                                1f,
                                                IsMouseOver ? borderLineStyle : borderDotStyle);
            context.DeviceContext.DrawRectangle(LayoutClippingBound,
                                                borderBrush,
                                                0.5f,
                                                borderDotStyle);
        }

        OnRender(context);
    }

    /// <summary>
    ///     Called when [render].
    /// </summary>
    /// <param name="context">The context.</param>
    protected abstract void OnRender(RenderContext2D context);

    /// <summary>
    ///     Determines whether this instance can render the specified context.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns>
    ///     <c>true</c> if this instance can render the specified context; otherwise, <c>false</c>.
    /// </returns>
    protected virtual bool CanRender(RenderContext2D context) => IsAttached && IsRendering;
}

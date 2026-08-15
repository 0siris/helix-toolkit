/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core2D.Abstract;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Core2D;
/// <summary>
/// </summary>
public class FrameStatisticsRenderCore : RenderCore2DBase {
    private DirectWriteFactory? Factory {
        get;
        set => SetDispose(ref field, value);
    }

    private TextFormat? Format {
        get;
        set => SetDispose(ref field, value);
    }
    
    private string previousStr = string.Empty;
    private RectangleF renderBound = new(0, 0, 100, 0);
    private IRenderStatistics? statistics;

    private TextLayout? TextLayout {
        get;
        set => SetDispose(ref field, value);
    }

    /// <summary>
    ///     Gets or sets the foreground.
    /// </summary>
    /// <value>
    ///     The foreground.
    /// </value>
    public Brush? Foreground {
        get;
        set => SetAffectsRender2(ref field, value)?.Dispose();
    }

    /// <summary>
    ///     Gets or sets the background.
    /// </summary>
    /// <value>
    ///     The background.
    /// </value>
    public Brush? Background {
        get;
        set => SetAffectsRender2(ref field, value)?.Dispose();
    }

    /// <summary>
    ///     Called when [attach].
    /// </summary>
    /// <param name="target">The target.</param>
    /// <returns></returns>
    protected override bool OnAttach(IRenderHost target) {
        Factory = new DirectWriteFactory();
        Format = new TextFormat(Factory, "Arial", FontWeight.Normal, FontStyle.Normal, 12 * target.DpiScale);
        previousStr = string.Empty;
        statistics = target.RenderStatistics;
        return base.OnAttach(target);
    }

    protected override void OnDetach() {
        Format = null;
        Foreground = null;
        Background = null;
        TextLayout = null;
        Factory = null;
        base.OnDetach();
    }

    /// <summary>
    ///     Determines whether this instance can render the specified context.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns>
    ///     <c>true</c> if this instance can render the specified context; otherwise, <c>false</c>.
    /// </returns>
    protected override bool CanRender(RenderContext2D context) 
        => base.CanRender(context) && statistics != null && statistics.FrameDetail != RenderDetail.None;

    /// <summary>
    ///     Called when [render].
    /// </summary>
    /// <param name="context">The context.</param>
    protected override void OnRender(RenderContext2D context) {
        Background ??= new SolidColorBrush(context.DeviceContext, new Color4(0.8f, 0.8f, 0.8f, 0.6f));
        Foreground ??= new SolidColorBrush(context.DeviceContext, new Color4(0, 0, 1, 1));

        var factory = Factory ?? throw new System.InvalidOperationException("Text factory is not initialized.");
        var format = Format ?? throw new System.InvalidOperationException("Text format is not initialized.");
        var str = statistics.AssertNotNull("Must be attached")
                            .GetDetailString();
        
        if (str != previousStr || TextLayout == null) {
            previousStr = str;
            TextLayout = new TextLayout(factory, str, format, float.MaxValue, float.MaxValue);
        }

        var textLayout = TextLayout ?? throw new System.InvalidOperationException("Text layout is not initialized.");
        var metrices = textLayout.Metrics;
        renderBound.Width = Math.Max(metrices.Width, renderBound.Width);
        renderBound.Height = metrices.Height;
        context.DeviceContext.Transform =
            Matrix3X2.Translation((float)context.ActualWidth - renderBound.Width, 0);
        context.DeviceContext.FillRectangle(renderBound, Background);
        context.DeviceContext.DrawTextLayout(Vector2.Zero, textLayout, Foreground);
    }
}

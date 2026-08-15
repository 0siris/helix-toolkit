/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Core2D.Abstract;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render;

namespace HelixToolkit.SharpDX.Core.Core2D;
public class TextRenderCore2D : RenderCore2DBase {
    private Brush? background;

    private Brush? foreground;

    private DirectWriteFactory? textFactory;
    private TextFormat? textFormat;

    private TextLayout? textLayout;

    private DirectWriteFactory TextFactory => textFactory
        ?? throw new InvalidOperationException("Text factory is not initialized.");

    private TextFormat TextFormat => textFormat
        ?? throw new InvalidOperationException("Text format is not initialized.");

    private TextLayout TextLayout => textLayout
        ?? throw new InvalidOperationException("Text layout is not initialized.");

    protected bool TextLayoutDirty = true;

    public string Text {
        get;
        set {
            if (SetAffectsRender(ref field, value)) 
                TextLayoutDirty = true;
        }
    } = string.Empty;

    public Brush? Foreground {
        get => foreground;
        set {
            var old = foreground;
            if (SetAffectsRender(ref foreground, value)) 
                RemoveAndDispose(ref old);
        }
    }

    public Brush? Background {
        get => background;
        set {
            var old = background;
            if (SetAffectsRender(ref background, value)) RemoveAndDispose(ref old);
        }
    }

    public string FontFamily {
        get;
        set {
            if (SetAffectsRender(ref field, value) && IsAttached) UpdateFontFormat();
        }
    } = "Arial";

    public int FontSize {
        get;
        set {
            if (SetAffectsRender(ref field, value) && IsAttached) UpdateFontFormat();
        }
    } = 12;

    public FontWeight FontWeight {
        get;
        set {
            if (SetAffectsRender(ref field, value) && IsAttached) UpdateFontFormat();
        }
    } = FontWeight.Normal;

    public FontStyle FontStyle {
        get;
        set {
            if (SetAffectsRender(ref field, value) && IsAttached) UpdateFontFormat();
        }
    } = FontStyle.Normal;

    public DrawTextOptions DrawingOptions { get; set; } = DrawTextOptions.None;

    public TextAlignment TextAlignment {
        get;
        set => SetAffectsRender(ref field, value);
    } = TextAlignment.Leading;

    public FlowDirection FlowDirection {
        get;
        set => SetAffectsRender(ref field, value);
    } = FlowDirection.LeftToRight;

    public TextMetrics Metrices {
        get {
            UpdateTextLayout();
            return TextLayout.Metrics;
        }
    }

    public float MaxWidth {
        get;
        set {
            if (Set(ref field, value)) TextLayoutDirty = true;
        }
    }

    public float MaxHeight {
        get;
        set {
            if (Set(ref field, value)) TextLayoutDirty = true;
        }
    }

    protected override bool OnAttach(IRenderHost host) {
        if (base.OnAttach(host)) {
            TextLayoutDirty = true;
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
        var renderHost = RenderHost ?? throw new InvalidOperationException("Render host is not initialized.");
        textFormat = new TextFormat(TextFactory,
                                    FontFamily,
                                    FontWeight,
                                    FontStyle,
                                    FontSize * renderHost.DpiScale);
        TextLayoutDirty = true;
    }

    private void UpdateTextLayout() {
        if (TextLayoutDirty) {
            RemoveAndDispose(ref textLayout);
            textLayout = new TextLayout(TextFactory, Text, TextFormat, MaxWidth, MaxHeight);
            TextLayoutDirty = false;
        }

        TextLayout.TextAlignment = TextAlignment;
    }

    protected override bool CanRender(RenderContext2D context) => base.CanRender(context) && Foreground is not null;

    protected override void OnRender(RenderContext2D context) {
        if (Background is { } background) context.DeviceContext.FillRectangle(LayoutBound, background);
        if (Foreground is not { } foreground) return;
        UpdateTextLayout();
        context.DeviceContext.DrawTextLayout(new Vector2(LayoutBound.Left, LayoutBound.Top),
                                             TextLayout,
                                             foreground,
                                             DrawingOptions);
    }
}

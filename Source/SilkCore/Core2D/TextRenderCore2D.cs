/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Native;

namespace HelixToolkit.SharpDX.Core.Core2D;
public class TextRenderCore2D : RenderCore2DBase {
    private Brush background;

    private Brush foreground;

    private DirectWriteFactory textFactory;
    private TextFormat textFormat;

    private TextLayout textLayout;

    protected bool textLayoutDirty = true;

    public string Text {
        get;
        set {
            if (SetAffectsRender(ref field, value)) 
                textLayoutDirty = true;
        }
    } = string.Empty;

    public Brush Foreground {
        get => foreground;
        set {
            var old = foreground;
            if (SetAffectsRender(ref foreground, value)) 
                RemoveAndDispose(ref old);
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
            return textLayout.Metrics;
        }
    }

    public float MaxWidth {
        get;
        set {
            if (Set(ref field, value)) textLayoutDirty = true;
        }
    }

    public float MaxHeight {
        get;
        set {
            if (Set(ref field, value)) textLayoutDirty = true;
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

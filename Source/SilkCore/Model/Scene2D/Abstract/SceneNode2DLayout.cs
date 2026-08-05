/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.CompilerServices;

namespace HelixToolkit.SharpDX.Core.Model.Scene2D;
/// <summary>
/// </summary>
public partial class SceneNode2D {
    #region layout management

    #region Properties

    /// <summary>
    ///     Gets or sets a value indicating whether this instance is measure dirty.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance is measure dirty; otherwise, <c>false</c>.
    /// </value>
    public bool IsMeasureDirty { get; protected set; } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether this instance is arrange dirty.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance is arrange dirty; otherwise, <c>false</c>.
    /// </value>
    public bool IsArrangeDirty { get; protected set; } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether this instance is transform dirty.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance is transform dirty; otherwise, <c>false</c>.
    /// </value>
    public bool IsTransformDirty { get; private set; } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether this instance is visual dirty.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance is visual dirty; otherwise, <c>false</c>.
    /// </value>
    public bool IsVisualDirty { get; set; } = true;

    public Thickness Margin {
        get;
        set {
            if (Set(ref field, value)) {
                MarginWidthHeight = new Vector2(value.Left + value.Right, value.Top + value.Bottom);
                InvalidateMeasure();
            }
        }
    }

    protected Vector2 MarginWidthHeight { get; private set; }

    private float width = float.PositiveInfinity;

    public float Width {
        get => width;
        set {
            if (Set(ref width, value)) InvalidateMeasure();
        }
    }

    private float height = float.PositiveInfinity;

    public float Height {
        get => height;
        set {
            if (Set(ref height, value)) InvalidateMeasure();
        }
    }

    public float MinimumWidth {
        get;
        set {
            if (Set(ref field, value) && value > width) InvalidateMeasure();
        }
    }

    public float MinimumHeight {
        get;
        set {
            if (Set(ref field, value) && value > height) InvalidateMeasure();
        }
    }

    public float MaximumWidth {
        get;
        set {
            if (Set(ref field, value) && value < width) InvalidateMeasure();
        }
    } = float.PositiveInfinity;

    public float MaximumHeight {
        get;
        set {
            if (Set(ref field, value) && value < height) InvalidateMeasure();
        }
    } = float.PositiveInfinity;

    public HorizontalAlignment HorizontalAlignment {
        get;
        set {
            if (Set(ref field, value)) InvalidateArrange();
        }
    } = HorizontalAlignment.Stretch;

    public VerticalAlignment VerticalAlignment {
        get;
        set {
            if (Set(ref field, value)) InvalidateArrange();
        }
    } = VerticalAlignment.Stretch;

    public Vector2 LayoutOffsets {
        get;
        private set {
            if (Set(ref field, value)) InvalidateTransform();
        }
    } = Vector2.Zero;

    /// <summary>
    ///     Gets the render size. Same as the <see cref="LayoutBound" /> size
    /// </summary>
    /// <value>
    ///     The size of the render.
    /// </value>
    public Vector2 RenderSize {
        get;
        private set {
            if (Set(ref field, value)) InvalidateTransform();
        }
    } = Vector2.Zero;

    public Vector2 RenderTransformOrigin {
        get;
        set {
            if (Set(ref field, value)) InvalidateRender();
        }
    } = new(0.5f, 0.5f);

    /// <summary>
    ///     Gets the size of the desired size after measure.
    /// </summary>
    /// <value>
    ///     The size of the desired.
    /// </value>
    public Vector2 DesiredSize { get; private set; }

    /// <summary>
    ///     Gets the size of the unclipped desired size after measure.
    /// </summary>
    /// <value>
    ///     The size of the unclipped desired.
    /// </value>
    public Vector2 UnclippedDesiredSize { get; private set; } = new(-1, -1);

    private Vector2 Size => new(width, height);

    public bool ClipEnabled { get; private set; }

    public bool ClipToBound { get; set; } = false;

    /// <summary>
    ///     Gets or sets the layout clip bound. This bound includes the margin.
    /// </summary>
    /// <value>
    ///     The layout clip bound.
    /// </value>
    public RectangleF LayoutClipBound {
        get => RenderCore.LayoutClippingBound;
        private set => RenderCore.LayoutClippingBound = value;
    }

    /// <summary>
    ///     Gets the size of the actual layout bound without margin.
    /// </summary>
    public RectangleF LayoutBound {
        get => RenderCore.LayoutBound;
        private set => RenderCore.LayoutBound = value;
    }

    private Size2F? previousMeasureSize;
    private RectangleF? previousArrange;

    #endregion Properties

    public void InvalidateMeasure() {
        IsArrangeDirty = true;
        IsMeasureDirty = true;
        TraverseUp(this,
                   p => {
                       if (p.IsArrangeDirty && p.IsMeasureDirty) return false;
                       p.IsArrangeDirty = true;
                       p.IsMeasureDirty = true;
                       return true;
                   });
        if (IsAttached) InvalidateRender();
    }

    public void InvalidateArrange() {
        IsArrangeDirty = true;
        TraverseUp(this,
                   p => {
                       if (p.IsArrangeDirty) return false;
                       p.IsArrangeDirty = true;
                       return true;
                   });
        if (IsAttached) InvalidateRender();
    }

    public void InvalidateVisual() {
        IsVisualDirty = true;
        TraverseUp(this,
                   p => {
                       if (p.IsVisualDirty) return false;
                       p.IsVisualDirty = true;
                       return true;
                   });
        if (IsAttached) InvalidateRender();
    }

    public void InvalidateTransform() {
        IsTransformDirty = true;
        TraverseUp(this,
                   e => {
                       if (e.IsTransformDirty) return false;
                       e.IsTransformDirty = true;
                       return true;
                   });
        if (IsAttached) InvalidateRender();
    }

    public void InvalidateAll() {
        IsTransformDirty = true;
        IsMeasureDirty = true;
        IsArrangeDirty = true;
        IsVisualDirty = true;
        TraverseUp(this,
                   p => {
                       if (p.IsTransformDirty && p.IsMeasureDirty && p.IsArrangeDirty && p.IsVisualDirty)
                           return false;
                       p.IsTransformDirty = true;
                       p.IsMeasureDirty = true;
                       p.IsArrangeDirty = true;
                       p.IsVisualDirty = true;
                       return true;
                   });
        if (IsAttached) InvalidateRender();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static void TraverseUp(SceneNode2D core, Func<SceneNode2D, bool> action) {
        var ancestor = core.Parent;
        while (ancestor != null) {
            if (!action(ancestor)) break;
            ancestor = ancestor.Parent;
        }
    }

    public void Measure(Size2F size) {
        if (!IsAttached || Visibility == Visibility.Collapsed ||
            (!IsMeasureDirty && previousMeasureSize == size)) return;
        previousMeasureSize = size;
        var availableSize = size.ToVector2();
        var availableSizeWithoutMargin = availableSize - MarginWidthHeight * DpiScale;
        Vector2 maxSize = Vector2.Zero, minSize = Vector2.Zero;
        CalculateMinMax(ref minSize, ref maxSize);

        availableSizeWithoutMargin.X = Math.Max(minSize.X, Math.Min(availableSizeWithoutMargin.X, maxSize.X));
        availableSizeWithoutMargin.Y = Math.Max(minSize.Y, Math.Min(availableSizeWithoutMargin.Y, maxSize.Y));

        var desiredSize = MeasureOverride(availableSizeWithoutMargin.ToSize2F()).ToVector2();

        var unclippedDesiredSize = desiredSize;

        var clipped = false;
        if (desiredSize.X > maxSize.X) {
            desiredSize.X = maxSize.X;
            clipped = true;
        }

        if (desiredSize.Y > maxSize.Y) {
            desiredSize.Y = maxSize.Y;
            clipped = true;
        }

        var clippedDesiredSize = desiredSize + MarginWidthHeight * DpiScale;

        if (clippedDesiredSize.X > availableSize.X) {
            clippedDesiredSize.X = availableSize.X;
            clipped = true;
        }

        if (clippedDesiredSize.Y > availableSize.Y) {
            clippedDesiredSize.Y = availableSize.Y;
            clipped = true;
        }

        if (clipped || clippedDesiredSize.X < 0 || clippedDesiredSize.Y < 0)
            UnclippedDesiredSize = unclippedDesiredSize;
        else
            UnclippedDesiredSize = new Vector2(-1, -1);
        if (DesiredSize != clippedDesiredSize) {
            DesiredSize = clippedDesiredSize;
            for (var i = 0; i < ItemsInternal.Count; ++i) ItemsInternal[i].InvalidateMeasure();
        } else {
            IsMeasureDirty = false;
        }
    }

    public void Arrange(RectangleF rect) {
        if (!IsAttached || Visibility == Visibility.Collapsed) return;
        if (IsMeasureDirty) Measure(previousMeasureSize ?? rect.Size);
        var ancestorDirty = false;
        TraverseUp(this,
                   parent => {
                       if (parent.IsArrangeDirty) {
                           ancestorDirty = true;
                           return false;
                       }

                       return true;
                   });

        var rectWidthHeight = new Vector2(rect.Width, rect.Height);

        if ((!IsArrangeDirty && !ancestorDirty && previousArrange == rect) ||
            rectWidthHeight.LengthSquared() == 0)
            return;
        previousArrange = rect;
        var arrangeSize = rectWidthHeight;

        ClipEnabled = false;
        var desiredSize = DesiredSize;

        if (float.IsNaN(DesiredSize.X) || float.IsNaN(DesiredSize.Y)) {
            if (UnclippedDesiredSize.X == -1 || UnclippedDesiredSize.Y == -1)
                desiredSize = arrangeSize - MarginWidthHeight * DpiScale;
            else
                desiredSize = UnclippedDesiredSize - MarginWidthHeight * DpiScale;
        }

        if (arrangeSize.X < desiredSize.X) {
            ClipEnabled = true;
            arrangeSize.X = desiredSize.X;
        }

        if (arrangeSize.Y < desiredSize.Y) {
            ClipEnabled = true;
            arrangeSize.Y = desiredSize.Y;
        }

        if (HorizontalAlignment != HorizontalAlignment.Stretch) arrangeSize.X = desiredSize.X;

        if (VerticalAlignment != VerticalAlignment.Stretch) arrangeSize.Y = desiredSize.Y;

        Vector2 minSize = Vector2.Zero, maxSize = Vector2.Zero;

        CalculateMinMax(ref minSize, ref maxSize);

        var calcedMaxWidth = Math.Max(desiredSize.X, maxSize.X);
        if (calcedMaxWidth < arrangeSize.X) {
            ClipEnabled = true;
            arrangeSize.X = calcedMaxWidth;
        }

        var calcedMaxHeight = Math.Max(desiredSize.Y, maxSize.Y);
        if (calcedMaxHeight < arrangeSize.Y) {
            ClipEnabled = true;
            arrangeSize.Y = calcedMaxHeight;
        }

        var oldRenderSize = RenderSize;
        var arrangeResultSize = ArrangeOverride(new RectangleF(Margin.Left * DpiScale,
                                                               Margin.Top * DpiScale,
                                                               arrangeSize.X - MarginWidthHeight.X * DpiScale,
                                                               arrangeSize.Y - MarginWidthHeight.Y * DpiScale))
            .ToVector2();

        var arrangeSizeChanged = arrangeResultSize != oldRenderSize;
        if (arrangeSizeChanged) InvalidateAll();

        RenderSize = arrangeResultSize;

        var clippedArrangeResultSize = new Vector2(Math.Min(arrangeResultSize.X, maxSize.X),
                                                   Math.Min(arrangeResultSize.Y, maxSize.Y));
        if (!ClipEnabled)
            ClipEnabled = clippedArrangeResultSize.X < arrangeResultSize.X ||
                          clippedArrangeResultSize.Y < arrangeResultSize.Y;

        var clientSize = new Vector2(Math.Max(0, rectWidthHeight.X - MarginWidthHeight.X * DpiScale),
                                     Math.Max(0, rectWidthHeight.Y - MarginWidthHeight.Y * DpiScale));

        if (!ClipEnabled)
            ClipEnabled = clientSize.X < clippedArrangeResultSize.X ||
                          clientSize.Y < clippedArrangeResultSize.Y;

        var layoutOffset = Vector2.Zero;

        var tempHorizontalAlign = HorizontalAlignment;
        var tempVerticalAlign = VerticalAlignment;

        if (tempHorizontalAlign == HorizontalAlignment.Stretch && clippedArrangeResultSize.X >= clientSize.X)
            tempHorizontalAlign = HorizontalAlignment.Left;

        if (tempVerticalAlign == VerticalAlignment.Stretch && clippedArrangeResultSize.Y >= clientSize.Y)
            tempVerticalAlign = VerticalAlignment.Top;

        if ((tempHorizontalAlign == HorizontalAlignment.Center ||
             tempHorizontalAlign == HorizontalAlignment.Stretch) && clientSize.X >= clippedArrangeResultSize.X)
            layoutOffset.X = (clientSize.X - clippedArrangeResultSize.X) / 2.0f;
        else if (tempHorizontalAlign == HorizontalAlignment.Right && clientSize.X >= clippedArrangeResultSize.X)
            layoutOffset.X = clientSize.X - clippedArrangeResultSize.X;
        else
            layoutOffset.X = 0;

        if ((tempVerticalAlign == VerticalAlignment.Center || tempVerticalAlign == VerticalAlignment.Stretch) &&
            clientSize.Y >= clippedArrangeResultSize.Y)
            layoutOffset.Y = (clientSize.Y - clippedArrangeResultSize.Y) / 2.0f;
        else if (tempVerticalAlign == VerticalAlignment.Bottom && clientSize.Y >= clippedArrangeResultSize.Y)
            layoutOffset.Y = clientSize.Y - clippedArrangeResultSize.Y;
        else
            layoutOffset.Y = 0;

        layoutOffset += new Vector2(rect.Left, rect.Top);

        if (ClipEnabled || ClipToBound) LayoutClipBound = new RectangleF(0, 0, clientSize.X, clientSize.Y);

        LayoutOffsets = layoutOffset;
        UpdateLayoutInternal();
        IsArrangeDirty = false;
    }

    private void CalculateMinMax(ref Vector2 minSize, ref Vector2 maxSize) {
        maxSize.Y = MaximumHeight;
        minSize.Y = MinimumHeight;

        var dimensionLength = Height;

        var height = dimensionLength;

        maxSize.Y = Math.Max(Math.Min(height, maxSize.Y), minSize.Y);

        height = float.IsInfinity(dimensionLength) ? 0 : dimensionLength;

        minSize.Y = Math.Max(Math.Min(maxSize.Y, height), minSize.Y);

        maxSize.X = MaximumWidth;
        minSize.X = MinimumWidth;

        dimensionLength = Width;

        var width = dimensionLength;

        maxSize.X = Math.Max(Math.Min(width, maxSize.X), minSize.X);

        width = float.IsInfinity(dimensionLength) ? 0 : dimensionLength;

        minSize.X = Math.Max(Math.Min(maxSize.X, width), minSize.X);
        minSize *= DpiScale;
        maxSize *= DpiScale;
    }

    private void UpdateLayoutInternal() {
        LayoutBound = new RectangleF(Margin.Left * DpiScale, Margin.Top * DpiScale, RenderSize.X, RenderSize.Y);
        LayoutClipBound = new RectangleF(0,
                                         0,
                                         RenderSize.X + MarginWidthHeight.X * DpiScale,
                                         RenderSize.Y + MarginWidthHeight.Y * DpiScale);
        LayoutTranslate = Matrix3x2.Translation((float)Math.Round(LayoutOffsets.X),
                                                (float)Math.Round(LayoutOffsets.Y));
    }

    protected virtual RectangleF ArrangeOverride(RectangleF finalSize) {
        for (var i = 0; i < ItemsInternal.Count; ++i) ItemsInternal[i].Arrange(finalSize);
        return finalSize;
    }

    protected virtual Size2F MeasureOverride(Size2F availableSize) {
        for (var i = 0; i < ItemsInternal.Count; ++i) ItemsInternal[i].Measure(availableSize);
        return availableSize;
    }

    #endregion layout management
}

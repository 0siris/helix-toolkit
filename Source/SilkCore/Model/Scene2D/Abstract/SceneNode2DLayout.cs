/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.CompilerServices;
using HelixToolkit.SharpDX.Core.Native;

namespace HelixToolkit.SharpDX.Core.Model.Scene2D;
/// <summary>Provides renderer-independent 2D scene state, layout and invalidation.</summary>
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

    /// <summary>Gets or sets the device-independent space surrounding the node.</summary>
    public Thickness Margin {
        get;
        set {
            if (Set(ref field, value)) {
                MarginWidthHeight = new Vector2(value.Left + value.Right, value.Top + value.Bottom);
                InvalidateMeasure();
            }
        }
    }

    /// <summary>Gets the summed horizontal and vertical margins.</summary>
    protected Vector2 MarginWidthHeight { get; private set; }

    /// <summary>Retains the requested device-independent width, or NaN for automatic measurement.</summary>
    private float width = float.PositiveInfinity;

    /// <summary>Gets or sets the requested width; NaN selects automatic measurement.</summary>
    public float Width {
        get => width;
        set {
            if (Set(ref width, value)) InvalidateMeasure();
        }
    }

    /// <summary>Retains the requested device-independent height, or NaN for automatic measurement.</summary>
    private float height = float.PositiveInfinity;

    /// <summary>Gets or sets the requested height; NaN selects automatic measurement.</summary>
    public float Height {
        get => height;
        set {
            if (Set(ref height, value)) InvalidateMeasure();
        }
    }

    /// <summary>Gets or sets the minimum measured width.</summary>
    public float MinimumWidth {
        get;
        set {
            if (Set(ref field, value) && value > width) InvalidateMeasure();
        }
    }

    /// <summary>Gets or sets the minimum measured height.</summary>
    public float MinimumHeight {
        get;
        set {
            if (Set(ref field, value) && value > height) InvalidateMeasure();
        }
    }

    /// <summary>Gets or sets the maximum measured width.</summary>
    public float MaximumWidth {
        get;
        set {
            if (Set(ref field, value) && value < width) InvalidateMeasure();
        }
    } = float.PositiveInfinity;

    /// <summary>Gets or sets the maximum measured height.</summary>
    public float MaximumHeight {
        get;
        set {
            if (Set(ref field, value) && value < height) InvalidateMeasure();
        }
    } = float.PositiveInfinity;

    /// <summary>Gets or sets horizontal placement within the arranged rectangle.</summary>
    public HorizontalAlignment HorizontalAlignment {
        get;
        set {
            if (Set(ref field, value)) InvalidateArrange();
        }
    } = HorizontalAlignment.Stretch;

    /// <summary>Gets or sets vertical placement within the arranged rectangle.</summary>
    public VerticalAlignment VerticalAlignment {
        get;
        set {
            if (Set(ref field, value)) InvalidateArrange();
        }
    } = VerticalAlignment.Stretch;

    /// <summary>Gets or sets the physical translation assigned by layout.</summary>
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

    /// <summary>Gets or sets the normalized origin used by the render transform.</summary>
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

    /// <summary>Gets the measured physical render size.</summary>
    private Vector2 Size => new(width, height);

    /// <summary>Gets whether layout currently requires clipping.</summary>
    public bool ClipEnabled { get; private set; }

    /// <summary>Gets or sets whether drawing is clipped to the layout bounds.</summary>
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

    /// <summary>Retains the previous measurement constraint for unchanged-layout reuse.</summary>
    private Size2F? previousMeasureSize;
    /// <summary>Retains the previous arranged rectangle for unchanged-layout reuse.</summary>
    private RectangleF? previousArrange;

    #endregion Properties

    /// <summary>Marks this node and its ancestors for measurement and notifies attached render consumers.</summary>
    public void InvalidateMeasure() {
        IsArrangeDirty = true;
        IsMeasureDirty = true;
        TraverseUp(this,
                   p => {
                       if (p is {IsArrangeDirty: true, IsMeasureDirty: true}) return false;
                       p.IsArrangeDirty = true;
                       p.IsMeasureDirty = true;
                       return true;
                   });
        if (IsAttached) InvalidateRender();
    }

    /// <summary>Marks this node and its ancestors for arrangement and notifies attached render consumers.</summary>
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

    /// <summary>
    ///     Marks this node and its ancestors for repaint without re-entering render invalidation.
    /// </summary>
    public void InvalidateVisual() => InvalidateRender();

    /// <summary>Marks this node and its ancestors for transform updates and notifies attached consumers.</summary>
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

    /// <summary>Marks transform, measurement, arrangement and paint state dirty and notifies attached consumers.</summary>
    public void InvalidateAll() {
        IsTransformDirty = true;
        IsMeasureDirty = true;
        IsArrangeDirty = true;
        IsVisualDirty = true;
        TraverseUp(this,
                   p => {
                       if (p is {IsTransformDirty: true, IsMeasureDirty: true, IsArrangeDirty: true, IsVisualDirty: true})
                           return false;
                       p.IsTransformDirty = true;
                       p.IsMeasureDirty = true;
                       p.IsArrangeDirty = true;
                       p.IsVisualDirty = true;
                       return true;
                   });
        if (IsAttached) InvalidateRender();
    }

    /// <summary>Visits ancestors until the supplied action stops traversal.</summary>
    /// <param name="core">The source render core or starting scene node.</param>
    /// <param name="action">The visitor; returning false stops ancestor traversal.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static void TraverseUp(SceneNode2D core, Func<SceneNode2D, bool> action) {
        var ancestor = core.Parent;
        while (ancestor != null) {
            if (!action(ancestor)) break;
            ancestor = ancestor.Parent;
        }
    }

    /// <summary>Measures the node against a physical-size constraint, reusing clean layout state.</summary>
    /// <param name="size">The physical measurement constraint.</param>
    public void Measure(Size2F size) => Measure(size, ScenePreparationPermission);

    /// <summary>Measures with a borrowed permission inherited by nested layout operations.</summary>
    /// <param name="size">The physical measurement constraint.</param>
    /// <param name="canContinue">The retained frame permission, or null for independent native callers.</param>
    internal void Measure(Size2F size, Func<bool>? canContinue) {
        var previous = scenePreparationPermission;
        scenePreparationPermission = canContinue;
        try {
            MeasureCore(size);
        } finally {
            scenePreparationPermission = previous;
        }
    }

    /// <summary>Runs measurement while permission protects virtual callbacks and their following work.</summary>
    /// <param name="size">The physical measurement constraint.</param>
    private void MeasureCore(Size2F size) {
        if (!CanContinueScenePreparation || !IsAttached || Visibility == Visibility.Collapsed ||
            (!IsMeasureDirty && previousMeasureSize == size)) return;
        previousMeasureSize = size;
        var availableSize = size.ToVector2();
        var availableSizeWithoutMargin = availableSize - MarginWidthHeight * DpiScale;
        Vector2 maxSize = Vector2.Zero, minSize = Vector2.Zero;
        CalculateMinMax(ref minSize, ref maxSize);

        availableSizeWithoutMargin.X = Math.Max(minSize.X, Math.Min(availableSizeWithoutMargin.X, maxSize.X));
        availableSizeWithoutMargin.Y = Math.Max(minSize.Y, Math.Min(availableSizeWithoutMargin.Y, maxSize.Y));

        var desiredSize = MeasureOverride(availableSizeWithoutMargin.ToSize2F()).ToVector2();
        if (!CanContinueScenePreparation) return;

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
            for (var i = 0; i < ItemsInternal.Count && CanContinueScenePreparation; ++i)
                ItemsInternal[i].InvalidateMeasure();
        } else {
            IsMeasureDirty = false;
        }
    }

    /// <summary>Arranges the node within a physical rectangle without allocating an ancestor-search callback.</summary>
    /// <param name="rect">The physical arrangement rectangle.</param>
    public void Arrange(RectangleF rect) => Arrange(rect, ScenePreparationPermission);

    /// <summary>Arranges with a borrowed permission inherited by nested layout operations.</summary>
    /// <param name="rect">The physical arrangement rectangle.</param>
    /// <param name="canContinue">The retained frame permission, or null for independent native callers.</param>
    internal void Arrange(RectangleF rect, Func<bool>? canContinue) {
        var previous = scenePreparationPermission;
        scenePreparationPermission = canContinue;
        try {
            ArrangeCore(rect);
        } finally {
            scenePreparationPermission = previous;
        }
    }

    /// <summary>Runs arrangement while permission protects virtual callbacks and their following work.</summary>
    /// <param name="rect">The physical arrangement rectangle.</param>
    private void ArrangeCore(RectangleF rect) {
        if (!CanContinueScenePreparation || !IsAttached || Visibility == Visibility.Collapsed) return;
        if (IsMeasureDirty) Measure(previousMeasureSize ?? rect.Size);
        if (!CanContinueScenePreparation) return;
        var ancestorDirty = false;
        for (var ancestor = Parent; ancestor is not null; ancestor = ancestor.Parent)
            if (ancestor.IsArrangeDirty) {
                ancestorDirty = true;
                break;
            }

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

        if (!CanContinueScenePreparation) return;

        var arrangeSizeChanged = arrangeResultSize != oldRenderSize;
        if (arrangeSizeChanged) InvalidateAll();
        if (!CanContinueScenePreparation) return;

        RenderSize = arrangeResultSize;
        if (!CanContinueScenePreparation) return;

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
        if (!CanContinueScenePreparation) return;
        UpdateLayoutInternal();
        if (!CanContinueScenePreparation) return;
        IsArrangeDirty = false;
    }

    /// <summary>Computes the physical minimum and maximum sizes including the configured DPI scale.</summary>
    /// <param name="minSize">Receives the physical minimum size.</param>
    /// <param name="maxSize">Receives the physical maximum size.</param>
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

    /// <summary>Updates cached layout bounds from the current render size and margins.</summary>
    private void UpdateLayoutInternal() {
        LayoutBound = new RectangleF(Margin.Left * DpiScale, Margin.Top * DpiScale, RenderSize.X, RenderSize.Y);
        if (!CanContinueScenePreparation) return;
        LayoutClipBound = new RectangleF(0,
                                         0,
                                         RenderSize.X + MarginWidthHeight.X * DpiScale,
                                         RenderSize.Y + MarginWidthHeight.Y * DpiScale);
        if (!CanContinueScenePreparation) return;
        LayoutTranslate = Matrix3X2.Translation((float)Math.Round(LayoutOffsets.X),
                                                (float)Math.Round(LayoutOffsets.Y));
    }

    /// <summary>Provides the node-specific arrangement calculation.</summary>
    /// <param name="finalSize">The available physical arrangement rectangle.</param>
    /// <returns>The resulting physical content size.</returns>
    protected virtual RectangleF ArrangeOverride(RectangleF finalSize) {
        for (var i = 0; i < ItemsInternal.Count && CanContinueScenePreparation; ++i)
            ItemsInternal[i].Arrange(finalSize);
        return finalSize;
    }

    /// <summary>Provides the node-specific measurement calculation.</summary>
    /// <param name="availableSize">The available physical measurement size.</param>
    /// <returns>The requested physical content size.</returns>
    protected virtual Size2F MeasureOverride(Size2F availableSize) {
        for (var i = 0; i < ItemsInternal.Count && CanContinueScenePreparation; ++i)
            ItemsInternal[i].Measure(availableSize);
        return availableSize;
    }

    #endregion layout management
}

/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Native;

namespace HelixToolkit.SharpDX.Core.Model.Scene2D;
public class StackPanelNode2D : PanelNode2D {
    public StackPanelNode2D() {
        EnableBitmapCache = true;
    }

    public Orientation Orientation {
        get;
        set => SetAffectsMeasure(ref field, value);
    } = Orientation.Horizontal;

    protected override Size2F MeasureOverride(Size2F availableSize) {
        var size = new Size2F();
        switch (Orientation) {
            case Orientation.Horizontal:
                availableSize.Width = float.PositiveInfinity;
                break;

            case Orientation.Vertical:
                availableSize.Height = float.PositiveInfinity;
                break;
        }

        foreach (var child in Items) {
                child.Measure(availableSize);
                switch (Orientation) {
                    case Orientation.Horizontal:
                        size.Width += child.DesiredSize.X;
                        size.Height = Math.Max(size.Height, child.DesiredSize.Y);
                        break;

                    case Orientation.Vertical:
                        size.Width = Math.Max(child.DesiredSize.X, size.Width);
                        size.Height += child.DesiredSize.Y;
                        break;
                }
        }

        return size;
    }

    protected override RectangleF ArrangeOverride(RectangleF finalSize) {
        float lastSize = 0;
        var totalSize = finalSize;
        foreach (var child in Items) {
                switch (Orientation) {
                    case Orientation.Horizontal:
                        totalSize.Left += lastSize;
                        lastSize = child.DesiredSize.X;
                        totalSize.Right = totalSize.Left + lastSize;
                        //totalSize.Bottom = totalSize.Top + Math.Min(finalSize.Height, c.DesiredSize.Y);
                        break;

                    case Orientation.Vertical:
                        totalSize.Top += lastSize;
                        lastSize = child.DesiredSize.Y;
                        //totalSize.Right = totalSize.Left + Math.Min(finalSize.Width, c.DesiredSize.X);
                        totalSize.Bottom = totalSize.Top + lastSize;
                        break;
                }

                child.Arrange(totalSize);
        }

        return finalSize;
    }
}

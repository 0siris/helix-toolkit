using System;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;

namespace HelixToolkit.Wpf.SharpDX.Controls;

/// <summary>
///     The VisualWrapper simply integrates a raw Visual child into a tree
///     of FrameworkElements.
///     https://blogs.msdn.microsoft.com/dwayneneed/2007/04/26/multithreaded-ui-hostvisual/
/// </summary>
[ContentProperty("Child")]
public class VisualWrapper<T> : FrameworkElement where T : Visual {
    private T? child;

    public T? Child {
        get => child;

        set {
            if (child is { } oldChild) RemoveVisualChild(oldChild);

            child = value;

            if (child is { } newChild) AddVisualChild(newChild);
        }
    }

    protected override int VisualChildrenCount => child is null ? 0 : 1;

    protected override Visual GetVisualChild(int index) {
        if (child is { } visualChild && index == 0) return visualChild;

        throw new ArgumentOutOfRangeException(nameof(index));
    }
}

/// <summary>
///     The VisualWrapper simply integrates a raw Visual child into a tree
///     of FrameworkElements.
/// </summary>
public class VisualWrapper : VisualWrapper<Visual> { }
